using UnityEngine;

namespace FirstJobber
{
    /// <summary>Observes successful existing controller events. Never validates or advances gameplay.</summary>
    public class UIPresentationAudio : MonoBehaviour
    {
        [SerializeField] UIManager ui;
        [SerializeField] ResumeEditorController resume;
        [SerializeField] CompanySelectionController companies;
        [SerializeField] InterviewController interview;
        [SerializeField] UISfxPlayer sfx;
        [SerializeField] UIVisualMotion completionFeedback;
        void OnEnable()
        {
            if (ui != null) { ui.SectionChanged += Transition; ui.PhasePresented += Phase; }
            if (resume != null) resume.Confirmed += ResumeComplete;
            if (companies != null) companies.CompanyConfirmed += Application;
            if (interview != null) interview.Finished += Complete;
        }
        void OnDisable()
        {
            if (ui != null) { ui.SectionChanged -= Transition; ui.PhasePresented -= Phase; }
            if (resume != null) resume.Confirmed -= ResumeComplete;
            if (companies != null) companies.CompanyConfirmed -= Application;
            if (interview != null) interview.Finished -= Complete;
        }
        void Transition() { if (sfx != null) sfx.Play(UISfxKind.Transition); }
        void Phase(GamePhase phase) { if (phase == GamePhase.Sleep) Complete(); }
        void Application(string id) { Complete(); }
        void ResumeComplete()
        {
            Complete();
            if (completionFeedback != null) completionFeedback.Pulse();
        }
        void Complete()
        {
            if (sfx != null) sfx.Play(UISfxKind.FinishTask);
        }
    }
}
