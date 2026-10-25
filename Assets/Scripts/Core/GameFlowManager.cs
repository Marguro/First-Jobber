using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>
    /// Central state machine. Owns the current GamePhase, validates transitions, opens panels and wires the
    /// controllers together. Controllers never talk to each other directly - they raise events handled here.
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] GameDatabase database;

        [Header("Systems")]
        [SerializeField] DialogueManager dialogue;
        [SerializeField] UIManager ui;
        [SerializeField] DailyEventManager dailyEvents;
        [Tooltip("Optional. Applies the atmosphere phase that matches the current day.")]
        [SerializeField] AtmosphereManager atmosphere;

        [Header("Controllers")]
        [SerializeField] MainMenuController mainMenu;
        [SerializeField] CreateApplicantController createApplicant;
        [SerializeField] ResumeEditorController resumeEditor;
        [SerializeField] CompanySelectionController companySelection;
        [SerializeField] InterviewController interview;
        [SerializeField] FinalDecisionController finalDecision;
        [SerializeField] EndingController ending;

        [Header("Timing")]
        [SerializeField] float sleepHold = 1.5f;

        public GamePhase Phase { get; private set; } = GamePhase.MainMenu;
        public GameSession Session => session;

        GameSession session;
        GameContext ctx;
        bool referencesOk;

        // Allowed transitions. Add a phase here to extend the flow.
        static readonly Dictionary<GamePhase, GamePhase[]> Transitions = new Dictionary<GamePhase, GamePhase[]>
        {
            { GamePhase.MainMenu, new[] { GamePhase.CreateApplicant, GamePhase.ResumeEditor, GamePhase.FinalDecision } },
            { GamePhase.CreateApplicant, new[] { GamePhase.ResumeEditor, GamePhase.MainMenu } },
            { GamePhase.ResumeEditor, new[] { GamePhase.CompanySelection } },
            { GamePhase.CompanySelection, new[] { GamePhase.InterviewIntroduction, GamePhase.ResumeEditor } },
            { GamePhase.InterviewIntroduction, new[] { GamePhase.InterviewResumeReview } },
            { GamePhase.InterviewResumeReview, new[] { GamePhase.InterviewQuestion } },
            { GamePhase.InterviewQuestion, new[] { GamePhase.InterviewReaction } },
            { GamePhase.InterviewReaction, new[] { GamePhase.InterviewQuestion, GamePhase.Rejection } },
            { GamePhase.Rejection, new[] { GamePhase.Sleep } },
            { GamePhase.Sleep, new[] { GamePhase.DayTransition } },
            { GamePhase.DayTransition, new[] { GamePhase.DailyEvent, GamePhase.ResumeEditor, GamePhase.FinalDecision } },
            { GamePhase.DailyEvent, new[] { GamePhase.ResumeEditor } },
            { GamePhase.FinalDecision, new[] { GamePhase.Ending } },
            { GamePhase.Ending, new[] { GamePhase.MainMenu, GamePhase.CreateApplicant } },
        };

        // ------------------------------------------------------------ lifecycle

        void OnEnable()
        {
            if (mainMenu != null) { mainMenu.SubmitRequested += NewGame; mainMenu.ContinueRequested += ContinueGame; }
            if (createApplicant != null) createApplicant.Submitted += OnApplicantSubmitted;
            if (resumeEditor != null) resumeEditor.Confirmed += OnResumeConfirmed;
            if (companySelection != null) { companySelection.CompanyConfirmed += OnCompanyConfirmed; companySelection.BackRequested += OnCompanyBack; }
            if (interview != null) { interview.PhaseRequested += OnInterviewPhase; interview.Finished += OnInterviewFinished; }
            if (finalDecision != null) finalDecision.EndingChosen += OnEndingChosen;
            if (ending != null) { ending.RestartRequested += OnEndingMenu; ending.MenuRequested += OnEndingMenu; }
            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        void OnDisable()
        {
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
            if (mainMenu != null) { mainMenu.SubmitRequested -= NewGame; mainMenu.ContinueRequested -= ContinueGame; }
            if (createApplicant != null) createApplicant.Submitted -= OnApplicantSubmitted;
            if (resumeEditor != null) resumeEditor.Confirmed -= OnResumeConfirmed;
            if (companySelection != null) { companySelection.CompanyConfirmed -= OnCompanyConfirmed; companySelection.BackRequested -= OnCompanyBack; }
            if (interview != null) { interview.PhaseRequested -= OnInterviewPhase; interview.Finished -= OnInterviewFinished; }
            if (finalDecision != null) finalDecision.EndingChosen -= OnEndingChosen;
            if (ending != null) { ending.RestartRequested -= OnEndingMenu; ending.MenuRequested -= OnEndingMenu; }
        }

        void Start()
        {
            referencesOk = CheckReferences();
            if (!referencesOk) return;

            foreach (var problem in database.Validate()) Debug.LogWarning("[Content] " + problem, database);
            LocalizationKeys.Visit(database, null); // gives every DialogueLine its localization key

            dialogue.TokenResolver = ResolveToken;
            dialogue.PlayerNameProvider = () => session != null ? session.applicantName : null;

            session = new GameSession();
            ctx = new GameContext(database, session, dialogue);
            ApplyAtmosphere(true);
            Phase = GamePhase.MainMenu;
            ui.ShowPanelFor(GamePhase.MainMenu, true);
            mainMenu.Open(database);
        }

        bool CheckReferences()
        {
            var missing = new StringBuilder();
            void Need(UnityEngine.Object o, string label) { if (o == null) missing.Append("\n  - ").Append(label); }
            Need(database, "database"); Need(dialogue, "dialogue"); Need(ui, "ui"); Need(dailyEvents, "dailyEvents");
            Need(mainMenu, "mainMenu"); Need(createApplicant, "createApplicant"); Need(resumeEditor, "resumeEditor");
            Need(companySelection, "companySelection"); Need(interview, "interview");
            Need(finalDecision, "finalDecision"); Need(ending, "ending");
            if (missing.Length == 0) return true;
            Debug.LogError("[GameFlowManager] Unassigned references:" + missing + "\nRun Tools > First Jobber > Build Game Scene, or assign them in the Inspector.", this);
            return false;
        }

        string ResolveToken(string key)
        {
            if (session == null) return null;
            switch (key)
            {
                case "name": return session.applicantName;
                case "age": return session.applicantAge.ToString();
                case "day": return session.day.ToString();
                case "company": { var c = ctx != null ? ctx.SelectedCompany : null; return c != null ? LocalizationManager.Get(c, "companyName", c.companyName) : UiText.TokenCompany.Text; }
                case "job": { var c = ctx != null ? ctx.SelectedCompany : null; return c != null ? LocalizationManager.Get(c, "jobTitle", c.jobTitle) : UiText.TokenJob.Text; }
                default: return null;
            }
        }

        /// <summary>Re-renders the panel that is on screen. Dialogue / interview text already shown keeps its language until the next line.</summary>
        void OnLanguageChanged()
        {
            if (!referencesOk || ctx == null) return;
            if (Phase == GamePhase.ResumeEditor) resumeEditor.RefreshLanguage();
            else if (Phase == GamePhase.CompanySelection) companySelection.RefreshLanguage();
        }

        // ------------------------------------------------------------ state machine

        public bool ChangePhase(GamePhase next, bool instantPanel = false)
        {
            if (next == Phase) return true;
            if (!Transitions.TryGetValue(Phase, out var allowed) || Array.IndexOf(allowed, next) < 0)
            {
                Debug.LogWarning("[GameFlow] Invalid transition " + Phase + " -> " + next);
                return false;
            }
            dialogue.Stop(); // drops any pending dialogue callbacks from the old phase
            Phase = next;
            session.phase = next;
            ui.ShowPanelFor(next, instantPanel);
            return true;
        }

        // ------------------------------------------------------------ start of game

        /// <summary>Submit on the main-menu resume: the applicant is created right there, then the paper zooms out into the editor.</summary>
        void NewGame(string playerName, int age)
        {
            if (!referencesOk || Phase != GamePhase.MainMenu) return;
            session = new GameSession { applicantName = playerName, applicantAge = age };
            ctx = new GameContext(database, session, dialogue);
            ApplyAtmosphere(true);
            if (ChangePhase(GamePhase.ResumeEditor)) BeginDay(true);
        }

        void ContinueGame()
        {
            if (!referencesOk || Phase != GamePhase.MainMenu) return;
            if (!GameSession.TryLoad(out var loaded)) { mainMenu.Open(database); return; }
            session = loaded;
            ctx = new GameContext(database, session, dialogue);
            ApplyAtmosphere(true);

            if (ctx.Loop.IsFinalDay())
            {
                if (ChangePhase(GamePhase.FinalDecision)) finalDecision.Open(ctx);
                return;
            }
            if (ChangePhase(GamePhase.ResumeEditor)) BeginDay(false);
        }

        void OnApplicantSubmitted(string playerName, int age)
        {
            if (Phase != GamePhase.CreateApplicant) return;
            session.applicantName = playerName;
            session.applicantAge = age;
            if (ChangePhase(GamePhase.ResumeEditor)) BeginDay(true);
        }

        // ------------------------------------------------------------ a day

        /// <summary>Phase is already ResumeEditor. Clears temporary state, autosaves, opens the editor.</summary>
        void BeginDay(bool playIntro)
        {
            session.selections.Clear();
            session.selectedCompanyId = null;
            session.applicationOpen = false;
            session.Save();
            OpenResumeEditor(playIntro, true);
        }

        void OpenResumeEditor(bool playIntro, bool runBeforeResume)
        {
            resumeEditor.Open(ctx, playIntro, () =>
            {
                if (runBeforeResume) dailyEvents.Run(EventTrigger.BeforeResume, session, resumeEditor.Unlock);
                else resumeEditor.Unlock();
            });
        }

        void OnResumeConfirmed()
        {
            if (Phase != GamePhase.ResumeEditor) return;
            dailyEvents.Run(EventTrigger.AfterResumeConfirmed, session, () =>
            {
                if (ChangePhase(GamePhase.CompanySelection)) companySelection.Open(ctx);
            });
        }

        void OnCompanyBack()
        {
            if (Phase != GamePhase.CompanySelection) return;
            if (ChangePhase(GamePhase.ResumeEditor)) OpenResumeEditor(false, false);
        }

        void OnCompanyConfirmed(string companyId)
        {
            if (Phase != GamePhase.CompanySelection) return;
            session.selectedCompanyId = companyId;
            ctx.Loop.BeginApplication();
            dailyEvents.Run(EventTrigger.AfterCompanySelected, session, () =>
                dailyEvents.Run(EventTrigger.BeforeInterview, session, StartInterview));
        }

        // ------------------------------------------------------------ interview & rejection

        void StartInterview()
        {
            if (!ChangePhase(GamePhase.InterviewIntroduction)) return;
            interview.Begin(ctx);
        }

        void OnInterviewPhase(GamePhase phase) { ChangePhase(phase); }

        void OnInterviewFinished()
        {
            dailyEvents.Run(EventTrigger.AfterInterview, session, () =>
            {
                if (ChangePhase(GamePhase.Rejection)) StartCoroutine(RejectionRoutine());
            });
        }

        IEnumerator RejectionRoutine()
        {
            var company = ctx.SelectedCompany;
            var hrLines = company != null && company.rejection != null && company.rejection.Length > 0
                ? company.rejection : database.rejectionHR;
            yield return dialogue.PlayAndWait(hrLines);
            yield return dialogue.PlayAndWait(database.rejectionPlayer);

            // Consequences: runs exactly once per application (LoopManager guards against repeats).
            ctx.Loop.ApplyRejection();

            yield return RunEvents(EventTrigger.AfterRejection);
            yield return RunEvents(EventTrigger.BeforeSleep);
            yield return SleepRoutine();
        }

        IEnumerator RunEvents(EventTrigger trigger)
        {
            bool done = false;
            dailyEvents.Run(trigger, session, () => done = true);
            while (!done) yield return null;
        }

        IEnumerator SleepRoutine()
        {
            if (!ChangePhase(GamePhase.Sleep)) yield break;

            var next = GamePhase.ResumeEditor;
            yield return ui.Blackout(UiText.DayOver.Format(session.day), sleepHold, () =>
            {
                ctx.Loop.AdvanceDay();
                ApplyAtmosphere(false);
                ChangePhase(GamePhase.DayTransition);

                if (ctx.Loop.IsFinalDay()) next = GamePhase.FinalDecision;
                else if (dailyEvents.HasEligible(EventTrigger.OnDayStart, session)) next = GamePhase.DailyEvent;
                else next = GamePhase.ResumeEditor;

                dailyEvents.ClearPanel();
                ChangePhase(next, true);
                if (next == GamePhase.ResumeEditor) resumeEditor.Refresh(ctx); // no stale content while fading in
            });

            switch (next)
            {
                case GamePhase.FinalDecision:
                    finalDecision.Open(ctx);
                    break;
                case GamePhase.DailyEvent:
                    dailyEvents.Run(EventTrigger.OnDayStart, session, () =>
                    {
                        if (ChangePhase(GamePhase.ResumeEditor)) BeginDay(false);
                    });
                    break;
                default:
                    BeginDay(false);
                    break;
            }
        }

        void ApplyAtmosphere(bool instant)
        {
            if (atmosphere != null) atmosphere.Apply(session.day, instant);
        }

        // ------------------------------------------------------------ ending

        void OnEndingChosen(string endingId)
        {
            if (Phase != GamePhase.FinalDecision) return;
            var data = database.FindEnding(endingId);
            if (data == null)
            {
                Debug.LogError("[GameFlow] Unknown ending id '" + endingId + "'.");
                return;
            }
            if (!ChangePhase(GamePhase.Ending)) return;
            session.endingId = endingId;
            GameSession.DeleteSave();
            ending.Show(ctx, data);
        }

        public GameDatabase Database => database;

        /// <summary>
        /// Debug only: jumps straight to an ending from any phase, bypassing the state machine.
        /// Leaves the save file alone (a real ending deletes it).
        /// </summary>
        public bool DebugTriggerEnding(string endingId)
        {
            if (!referencesOk || database == null) return false;
            var data = database.FindEnding(endingId);
            if (data == null) { Debug.LogError("[Debug] Unknown ending id '" + endingId + "'."); return false; }

            StopAllCoroutines(); // drop a running rejection / sleep sequence so it cannot change phase afterwards
            dialogue.Stop();
            if (session == null || ctx == null)
            {
                session = new GameSession();
                ctx = new GameContext(database, session, dialogue);
            }
            Phase = GamePhase.Ending;
            session.phase = GamePhase.Ending;
            session.endingId = endingId;
            ui.ShowPanelFor(GamePhase.Ending, true);
            ending.Show(ctx, data);
            Debug.Log("[Debug] Triggered ending '" + endingId + "'.");
            return true;
        }

        void OnEndingMenu()
        {
            if (!ChangePhase(GamePhase.MainMenu)) return;
            mainMenu.Open(database);
        }
    }
}
