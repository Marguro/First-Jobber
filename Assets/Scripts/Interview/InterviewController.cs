using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>
    /// Runs one interview from the resume the player submitted:
    /// intro -> HR reviews resume -> N questions (answer, player reaction, HR response) -> Finished.
    /// The rejection itself is handled by GameFlowManager.
    /// </summary>
    public class InterviewController : MonoBehaviour
    {
        [SerializeField] TMP_Text questionText;
        [SerializeField] ChoiceListUI answerList;
        [SerializeField] HRReactionController hr;
        [SerializeField] float neutralDelay = 0.4f;

        public event Action Finished;
        /// <summary>Raised for the interview sub-phases (introduction, resume review, question, reaction).</summary>
        public event Action<GamePhase> PhaseRequested;

        GameContext ctx;
        string chosenAnswerId;
        bool awaitingAnswer;
        Coroutine routine;

        readonly List<ResumeChoiceData> selectedBuffer = new List<ResumeChoiceData>();
        readonly List<InterviewQuestionData> questionBuffer = new List<InterviewQuestionData>();
        readonly List<ChoiceItem> itemBuffer = new List<ChoiceItem>();

        void Awake()
        {
            if (answerList != null) answerList.ItemChosen += OnAnswerChosen;
        }

        void OnDestroy()
        {
            if (answerList != null) answerList.ItemChosen -= OnAnswerChosen;
        }

        public void Begin(GameContext context)
        {
            Cancel();
            ctx = context;
            hr.StrangeMode = ctx.Session.HasFlag("hr_strange");
            hr.SetCharacter(ctx.SelectedCompany != null ? ctx.SelectedCompany.hr : null);
            routine = StartCoroutine(Run());
        }

        public void Cancel()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            awaitingAnswer = false;
            if (answerList != null) { answerList.SetInteractable(false); answerList.Clear(); }
            if (questionText != null) questionText.text = "";
        }

        IEnumerator Run()
        {
            var company = ctx.SelectedCompany;
            // Everything below comes from the company's HR. No HR = the old shared interview (and an error in the log).
            var hrData = ctx.SelectedHr;
            if (hrData == null)
                Debug.LogError("[Interview] Company '" + (company != null ? company.name : "?") + "' has no HR assigned. Running the old shared interview. Assign CompanyData.hr.", company);

            if (questionText != null) questionText.text = "";
            answerList.Clear();
            answerList.SetInteractable(false);

            // 1. HR introduction: HR's own -> company's legacy -> shared default
            PhaseRequested?.Invoke(GamePhase.InterviewIntroduction);
            if (hrData != null && HasLines(hrData.introduction)) yield return PlayAndWait(hrData.introduction, hrData.introMode);
            else if (company != null && HasLines(company.hrIntro)) yield return PlayAndWait(company.hrIntro);
            else yield return PlayAndWait(ctx.Database.defaultHrIntro, ctx.Database.defaultHrIntroMode);

            // 2. HR reviews the resume and reacts to each selected choice
            PhaseRequested?.Invoke(GamePhase.InterviewResumeReview);
            if (hrData != null && HasLines(hrData.resumeReview)) yield return PlayAndWait(hrData.resumeReview, hrData.resumeReviewMode);
            else yield return PlayAndWait(ctx.Database.hrReviewLines, ctx.Database.hrReviewMode);
            ctx.Choices.GetSelectedChoices(selectedBuffer);
            for (int i = 0; i < selectedBuffer.Count; i++)
            {
                var own = hrData != null ? hrData.FindChoiceReaction(selectedBuffer[i]) : null;
                if (own != null) yield return PlayAndWait(own.lines, own.mode);
                else yield return PlayAndWait(selectedBuffer[i].hrReactions);
            }
            yield return new WaitForSecondsRealtime(neutralDelay);
            hr.ResetToNeutral();

            // 3. Questions: the HR's sequence (fixed + resume-linked), or the shared bank without an HR
            if (hrData != null)
                HrInterviewPlanner.Plan(hrData, ctx.Session, selectedBuffer, company, questionBuffer);
            else
                InterviewQuestionSelector.Pick(ctx.Database, ctx.Session, selectedBuffer, company,
                    Mathf.Max(1, ctx.Database.questionsPerInterview), questionBuffer);
            ctx.Session.interviewQuestionIds.Clear();
            foreach (var q in questionBuffer) ctx.Session.interviewQuestionIds.Add(q.id);

            for (int i = 0; i < questionBuffer.Count; i++)
            {
                var question = questionBuffer[i];
                ctx.Session.interviewIndex = i;

                PhaseRequested?.Invoke(GamePhase.InterviewQuestion);
                string formatted = ctx.Dialogue.Format(LocalizationManager.Get(question, "questionText", question.questionText));
                if (HasLines(question.questionDialogue))
                {
                    string shown = ctx.Dialogue.FirstText(question.questionDialogue);
                    if (!string.IsNullOrEmpty(shown)) formatted = shown;
                }
                if (questionText != null) questionText.text = formatted;
                if (HasLines(question.questionDialogue)) yield return PlayAndWait(question.questionDialogue);
                else yield return PlayAndWait(new[] { new DialogueLine(Speakers.HR, formatted, question.hrEmotion) });

                // Show answers and wait for exactly one pick
                BuildAnswerItems(question);
                chosenAnswerId = null;
                awaitingAnswer = true;
                answerList.SetItems(itemBuffer, "", true);
                answerList.SetInteractable(true);
                while (chosenAnswerId == null) yield return null;
                awaitingAnswer = false;
                answerList.SetInteractable(false);

                // Reactions
                PhaseRequested?.Invoke(GamePhase.InterviewReaction);
                var answer = FindAnswer(question, chosenAnswerId);
                if (answer != null)
                {
                    yield return PlayAndWait(answer.playerReaction, answer.playerReactionMode);
                    yield return PlayAndWait(answer.hrResponse, answer.hrResponseMode);
                }
                if (!ctx.Session.askedQuestionIds.Contains(question.id)) ctx.Session.askedQuestionIds.Add(question.id);

                yield return new WaitForSecondsRealtime(neutralDelay);
                hr.ResetToNeutral();
            }

            answerList.Clear();
            if (questionText != null) questionText.text = "";
            routine = null;
            Finished?.Invoke();
        }

        static bool HasLines(DialogueLine[] lines) => lines != null && lines.Length > 0;

        void BuildAnswerItems(InterviewQuestionData question)
        {
            itemBuffer.Clear();
            for (int i = 0; i < question.answers.Count; i++)
            {
                var a = question.answers[i];
                if (a == null) continue;
                itemBuffer.Add(new ChoiceItem
                {
                    id = a.id,
                    title = LocalizationManager.Get(LocalizationKeys.Answer(question, a, i, "text"), a.text),
                    state = ChoiceState.Available
                });
            }
        }

        static InterviewAnswer FindAnswer(InterviewQuestionData question, string id)
        {
            for (int i = 0; i < question.answers.Count; i++)
                if (question.answers[i] != null && question.answers[i].id == id) return question.answers[i];
            return null;
        }

        void OnAnswerChosen(string id)
        {
            if (!awaitingAnswer || chosenAnswerId != null) return;
            chosenAnswerId = id;
            answerList.SetInteractable(false);
        }

        IEnumerator PlayAndWait(IList<DialogueLine> lines, DialoguePlaybackMode mode = DialoguePlaybackMode.Sequential)
        {
            if (lines == null || lines.Count == 0) yield break;
            bool done = false;
            ctx.Dialogue.Play(lines, () => done = true, 0f, mode);
            while (!done) yield return null;
        }
    }
}
