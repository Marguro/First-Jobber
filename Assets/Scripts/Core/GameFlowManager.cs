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
        bool debugEndingPreview;

        // Allowed transitions. Add a phase here to extend the flow.
        static readonly Dictionary<GamePhase, GamePhase[]> Transitions = new Dictionary<GamePhase, GamePhase[]>
        {
            { GamePhase.MainMenu, new[] { GamePhase.CreateApplicant, GamePhase.ResumeEditor, GamePhase.FinalDecision } },
            { GamePhase.CreateApplicant, new[] { GamePhase.ResumeEditor, GamePhase.MainMenu } },
            { GamePhase.ResumeEditor, new[] { GamePhase.CompanySelection, GamePhase.DailyEvent } },
            { GamePhase.CompanySelection, new[] { GamePhase.InterviewIntroduction, GamePhase.ResumeEditor } },
            { GamePhase.InterviewIntroduction, new[] { GamePhase.InterviewResumeReview } },
            { GamePhase.InterviewResumeReview, new[] { GamePhase.InterviewQuestion } },
            { GamePhase.InterviewQuestion, new[] { GamePhase.InterviewReaction } },
            { GamePhase.InterviewReaction, new[] { GamePhase.InterviewQuestion, GamePhase.Rejection } },
            { GamePhase.Rejection, new[] { GamePhase.Sleep } },
            { GamePhase.Sleep, new[] { GamePhase.DayTransition } },
            { GamePhase.DayTransition, new[] { GamePhase.DailyEvent, GamePhase.ResumeEditor, GamePhase.FinalDecision } },
            { GamePhase.DailyEvent, new[] { GamePhase.ResumeEditor, GamePhase.FinalDecision } },
            { GamePhase.FinalDecision, new[] { GamePhase.Ending, GamePhase.ResumeEditor } },
            { GamePhase.Ending, new[] { GamePhase.MainMenu, GamePhase.CreateApplicant, GamePhase.ResumeEditor } },
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
            if (ending != null) { ending.RestartRequested += OnEndingMenu; ending.MenuRequested += OnEndingMenu; ending.PresentationCompleted += OnEndingPresentationCompleted; }
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
            if (ending != null) { ending.RestartRequested -= OnEndingMenu; ending.MenuRequested -= OnEndingMenu; ending.PresentationCompleted -= OnEndingPresentationCompleted; }
        }

        void Start()
        {
            referencesOk = CheckReferences();
            if (!referencesOk) return;

            foreach (var problem in database.Validate()) Debug.LogWarning("[Content] " + problem, database);
            LocalizationKeys.Visit(database, null); // gives every DialogueLine its localization key

            dialogue.TokenResolver = ResolveToken;
            dialogue.PlayerNameProvider = () => session != null ? session.applicantName : null;
            dialogue.GenderIdProvider = () => session != null ? session.applicantGender : null;
            dialogue.HrNameProvider = () => { var h = ctx != null ? ctx.SelectedHr : null; return h != null ? LocalizationManager.Get(h, "hrName", h.hrName) : null; };
            // Portrait identity is strict: a missing company HR must never borrow another HR's art.
            dialogue.HrCharacterProvider = () => { var company = ctx != null ? ctx.SelectedCompany : null; return company != null ? company.hr : null; };

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
            switch (key.ToLowerInvariant())
            {
                case "name":
                case "playername": return session.applicantName;
                case "gender": return database.GenderDisplay(session.applicantGender);
                case "hr": { var h = ctx != null ? ctx.SelectedHr : null; return h != null ? LocalizationManager.Get(h, "hrName", h.hrName) : "HR"; }
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
            else if (Phase == GamePhase.InterviewIntroduction || Phase == GamePhase.InterviewResumeReview
                || Phase == GamePhase.InterviewQuestion || Phase == GamePhase.InterviewReaction || Phase == GamePhase.Rejection)
                interview.RefreshLanguage();
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
        void NewGame(string playerName, string genderId)
        {
            if (!referencesOk || Phase != GamePhase.MainMenu) return;
            Debug.Log("[GameFlow] Starting Day 1 with validated applicant; gender ID=" + genderId, this);
            session = new GameSession { applicantName = playerName, applicantGender = genderId };
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

            if (IsFinalStoryDay())
            {
                if (ChangePhase(GamePhase.FinalDecision)) OpenFinalStory();
                return;
            }
            if (ChangePhase(GamePhase.ResumeEditor)) BeginDay(false);
        }

        void OnApplicantSubmitted(string playerName, string genderId)
        {
            if (Phase != GamePhase.CreateApplicant) return;
            session.applicantName = playerName;
            session.applicantGender = genderId;
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
            if (session.day == 1) Debug.Log("[Applicant] Day 1 profile save requested before opening story. Save failures are reported by GameSession.", this);
            resumeEditor.Refresh(ctx);
            OpenResumeEditor(playIntro, true);
        }

        void OpenResumeEditor(bool playIntro, bool runBeforeResume)
        {
            Action open = () => {
                Debug.Log("[GameFlow] Opening Resume Editor after BeforeResume events; day=" + session.day, this);
                if (Phase == GamePhase.DailyEvent) ChangePhase(GamePhase.ResumeEditor);
                resumeEditor.Open(ctx, playIntro && database.finalStoryDay == 0, () =>
                    dailyEvents.Run(EventTrigger.OnEnterResume, session, resumeEditor.Unlock));
            };
            if (runBeforeResume && database.finalStoryDay > 0)
            {
                dailyEvents.ClearPanel();
                ChangePhase(GamePhase.DailyEvent);
                dailyEvents.Run(EventTrigger.BeforeResume, session, open);
            }
            else if (runBeforeResume) dailyEvents.Run(EventTrigger.BeforeResume, session, open);
            else open();
        }

        void OnResumeConfirmed()
        {
            if (Phase != GamePhase.ResumeEditor) return;
            // Confidence was captured when Confirm was pressed (ResumeEditorController); it is not recalculated here.
            // The confidence reaction is NOT played now: it plays when the company application is confirmed (OnCompanyConfirmed).
            dailyEvents.Run(EventTrigger.AfterResumeConfirmed, session, () =>
            {
                if (ChangePhase(GamePhase.CompanySelection))
                {
                    companySelection.Open(ctx);
                    companySelection.SetStoryLocked(true);
                    dailyEvents.Run(EventTrigger.OnEnterCompanySelection, session, () => companySelection.SetStoryLocked(false));
                }
            });
        }

        void OnCompanyBack()
        {
            if (Phase != GamePhase.CompanySelection) return;
            if (ChangePhase(GamePhase.ResumeEditor)) OpenResumeEditor(false, false);
        }

        void OnCompanyConfirmed(string companyId)
        {
            if (Phase != GamePhase.CompanySelection || session.applicationOpen) return; // one application = one reaction
            var company = database.FindCompany(companyId);
            if (company == null) return;
            session.selectedCompanyId = companyId;
            ctx.Loop.BeginApplication();

            // Confidence reaction (finalized value from Resume Confirmation) -> company line -> events -> interview.
            // All of it plays while the Company Selection screen is still shown; each step starts when the previous one completed.
            // Day and confidence are captured here, once; later refreshes cannot change the pool.
            var confidenceLines = database.ResolveApplyReaction(session.day, session.confirmedConfidence, out var confidenceMode);
            dialogue.Play(confidenceLines, mode: confidenceMode, onComplete: () =>
                dialogue.Play(database.companyConfirmedLines, mode: database.companyConfirmedMode, onComplete: () =>
                    dailyEvents.Run(EventTrigger.AfterCompanySelected, session, () =>
                        dailyEvents.Run(EventTrigger.BeforeInterview, session, StartInterview))));
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
            // The company's HR owns the rejection. Missing -> the company's own lines -> the shared fallback.
            var hrData = ctx.SelectedHr;
            DialogueLine[] hrLines;
            DialoguePlaybackMode hrMode;
            if (hrData != null && hrData.rejection != null && hrData.rejection.Length > 0) { hrLines = hrData.rejection; hrMode = hrData.rejectionMode; }
            else if (company != null && company.rejection != null && company.rejection.Length > 0) { hrLines = company.rejection; hrMode = DialoguePlaybackMode.Sequential; }
            else { hrLines = database.rejectionHR; hrMode = database.rejectionHRMode; }
            yield return dialogue.PlayAndWait(hrLines, 0f, hrMode);
            yield return dialogue.PlayAndWait(database.rejectionPlayer, 0f, database.rejectionPlayerMode);

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

                if (IsFinalStoryDay()) next = GamePhase.FinalDecision;
                else if (dailyEvents.HasEligible(EventTrigger.OnDayStart, session)) next = GamePhase.DailyEvent;
                else if (database.finalStoryDay > 0 && dailyEvents.HasEligible(EventTrigger.BeforeResume, session)) next = GamePhase.DailyEvent;
                else next = GamePhase.ResumeEditor;

                dailyEvents.ClearPanel();
                ChangePhase(next, true);
                if (next == GamePhase.ResumeEditor) resumeEditor.Refresh(ctx); // no stale content while fading in
            });

            switch (next)
            {
                case GamePhase.FinalDecision:
                    OpenFinalStory();
                    break;
                case GamePhase.DailyEvent:
                    dailyEvents.Run(EventTrigger.OnDayStart, session, () =>
                    {
                        BeginDay(false);
                    });
                    break;
                default:
                    BeginDay(false);
                    break;
            }
        }

        bool IsFinalStoryDay() => database.finalStoryDay > 0 ? session.day >= database.finalStoryDay : ctx.Loop.IsFinalDay();

        void OpenFinalStory()
        {
            finalDecision.PrepareStory();
            dailyEvents.Run(EventTrigger.OnDayStart, session, () => finalDecision.Open(ctx));
        }

        void ApplyAtmosphere(bool instant)
        {
            if (atmosphere != null) atmosphere.Apply(session.day, instant);
        }

        // ------------------------------------------------------------ ending

        void OnEndingChosen(string endingId)
        {
            if (Phase != GamePhase.FinalDecision) return;
            debugEndingPreview = false;
            var data = database.FindEnding(endingId);
            if (data == null)
            {
                Debug.LogError("[GameFlow] Unknown ending id '" + endingId + "'.");
                return;
            }
            if (!ChangePhase(GamePhase.Ending)) return;
            session.endingId = endingId;
            if (!data.RestartsCycle) GameSession.DeleteSave();
            ending.Show(ctx, data);
        }

        void OnEndingPresentationCompleted(EndingData data)
        {
            if (Phase != GamePhase.Ending || data == null || session.endingId != data.id) return;
            if (debugEndingPreview) { debugEndingPreview = false; return; } // Existing debug command never writes/reset saves.
#if UNITY_EDITOR
            if (editorEndingPreview)
            {
                editorEndingPreview = false;
                session = editorSavedSession; ctx = editorSavedContext;
                Phase = GamePhase.MainMenu;
                ui.ShowPanelFor(Phase, true); mainMenu.Open(database);
                Debug.Log("[Ending] Presentation preview complete. Original session/save untouched; ending action was not executed.", this);
                return;
            }
#endif
            if (data.RestartsCycle)
            {
                // Preserve identity, history and once-per-playthrough choice reactions/tutorials.
                session.day = 1;
                session.consumedChoiceIds.Clear();
                session.consumedCompanyIds.Clear();
                session.askedQuestionIds.Clear();
                session.flags.RemoveAll(f => f.StartsWith("event_choice/") || f == "resume_distorted" || f == "hr_strange");
                session.triggeredEvents.RemoveAll(id => !id.StartsWith("event_day01_"));
                session.interviewQuestionIds.Clear();
                session.interviewIndex = 0;
                session.confirmedConfidence = 0;
                session.endingId = null;
                ApplyAtmosphere(true);
                if (ChangePhase(GamePhase.ResumeEditor)) BeginDay(false);
                return;
            }
        }

        public GameDatabase Database => database;

#if UNITY_EDITOR
        [Header("Editor-only ending presentation preview")]
        [SerializeField] EndingData editorPreviewEnding;
        bool editorEndingPreview;
        GameSession editorSavedSession;
        GameContext editorSavedContext;
        [ContextMenu("Debug/Preview assigned ending presentation (Play Mode, Main Menu only)")]
        void PreviewEndingPresentation()
        {
            if (!Application.isPlaying || !referencesOk || Phase != GamePhase.MainMenu || dialogue.IsPlaying || editorPreviewEnding == null)
            { Debug.LogWarning("[Ending] Assign Editor Preview Ending, then use preview in Play Mode at Main Menu.", this); return; }
            editorSavedSession = session; editorSavedContext = ctx; editorEndingPreview = true;
            session = new GameSession { applicantName = "Preview", applicantGender = Genders.Unspecified, day = 6, endingId = editorPreviewEnding.id, phase = GamePhase.Ending };
            ctx = new GameContext(database, session, dialogue);
            Phase = GamePhase.Ending; ui.ShowPanelFor(Phase, true);
            ending.Show(ctx, editorPreviewEnding);
        }

        [ContextMenu("Debug/Preview Day 2 phone event (Play Mode, Main Menu only)")]
        void PreviewDay2Phone() { PreviewPhoneEvent(2); }

        [ContextMenu("Debug/Preview Day 4 phone event (Play Mode, Main Menu only)")]
        void PreviewDay4Phone() { PreviewPhoneEvent(4); }

        void PreviewPhoneEvent(int day)
        {
            if (!Application.isPlaying || !referencesOk || Phase != GamePhase.MainMenu || dialogue.IsPlaying || dailyEvents.IsWaitingForChoice)
            {
                Debug.LogWarning("[DailyEvent] Preview is available only in Play Mode at the Main Menu with no active dialogue.", this);
                return;
            }
            var ev = database.events.Find(e => e != null && e.exactDay == day && e.trigger == EventTrigger.BeforeResume);
            if (ev == null || ev.segments == null) { Debug.LogWarning("[DailyEvent] No segmented phone event configured for day " + day, this); return; }
            var previousSession = session;
            var previousContext = ctx;
            session = new GameSession { day = day, applicantName = "Preview", applicantGender = Genders.Unspecified };
            ctx = new GameContext(database, session, dialogue);
            Phase = GamePhase.DailyEvent;
            dailyEvents.ClearPanel();
            ui.ShowPanelFor(Phase, true);
            dailyEvents.EditorPreviewEvent(ev, session, () => {
                session = previousSession;
                ctx = previousContext;
                Phase = GamePhase.MainMenu;
                ui.ShowPanelFor(Phase, true);
                mainMenu.Open(database);
                Debug.Log("[DailyEvent] Phone preview complete; returned to Main Menu. Save data was not changed.", this);
            });
        }
#endif

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
            debugEndingPreview = true;
            ending.Show(ctx, data, true);
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
