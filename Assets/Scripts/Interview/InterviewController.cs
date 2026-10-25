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
            hr.StrangeMode = ctx.Session.HasFlag("hr_strange");
            hr.ResetToNeutral();
            if (questionText != null) questionText.text = "";
            answerList.Clear();
            answerList.SetInteractable(false);

            // 1. HR introduction
            PhaseRequested?.Invoke(GamePhase.InterviewIntroduction);
            yield return PlayAndWait(company != null && company.hrIntro != null && company.hrIntro.Length > 0
                ? company.hrIntro : ctx.Database.defaultHrIntro);

            // 2. HR reviews the resume and reacts to each selected choice
            PhaseRequested?.Invoke(GamePhase.InterviewResumeReview);
            yield return PlayAndWait(ctx.Database.hrReviewLines);
            ctx.Choices.GetSelectedChoices(selectedBuffer);
            for (int i = 0; i < selectedBuffer.Count; i++)
                yield return PlayAndWait(selectedBuffer[i].hrReactions);
            yield return new WaitForSecondsRealtime(neutralDelay);
            hr.ResetToNeutral();

            // 3. Questions linked to the resume
            InterviewQuestionSelector.Pick(ctx.Database, ctx.Session, selectedBuffer, company,
                Mathf.Max(1, ctx.Database.questionsPerInterview), questionBuffer);
            ctx.Session.interviewQuestionIds.Clear();
            foreach (var q in questionBuffer) ctx.Session.interviewQuestionIds.Add(q.id);

            for (int i = 0; i < questionBuffer.Count; i++)
            {
                var question = questionBuffer[i];
                ctx.Session.interviewIndex = i;

                PhaseRequested?.Invoke(GamePhase.InterviewQuestion);
                string formatted = ctx.Dialogue.Format(question.questionText);
                if (questionText != null) questionText.text = formatted;
                yield return PlayAndWait(new[] { new DialogueLine(Speakers.HR, formatted, question.hrEmotion) });

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
                    yield return PlayAndWait(answer.playerReaction);
                    yield return PlayAndWait(answer.hrResponse);
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

        void BuildAnswerItems(InterviewQuestionData question)
        {
            itemBuffer.Clear();
            for (int i = 0; i < question.answers.Count; i++)
            {
                var a = question.answers[i];
                if (a == null) continue;
                itemBuffer.Add(new ChoiceItem { id = a.id, title = a.text, state = ChoiceState.Available });
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

        IEnumerator PlayAndWait(IList<DialogueLine> lines)
        {
            if (lines == null || lines.Count == 0) yield break;
            bool done = false;
            ctx.Dialogue.Play(lines, () => done = true);
            while (!done) yield return null;
        }
    }
}
