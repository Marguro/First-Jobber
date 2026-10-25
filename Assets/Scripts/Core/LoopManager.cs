namespace FirstJobber
{
    /// <summary>Day / loop rules. The number of loops is never hardcoded: it follows the choice pools.</summary>
    public class LoopManager
    {
        readonly GameSession session;
        readonly ChoiceManager choices;

        public LoopManager(GameSession session, ChoiceManager choices)
        {
            this.session = session;
            this.choices = choices;
        }

        public void BeginApplication() { session.applicationOpen = true; }

        /// <summary>Consumes today's application exactly once. Returns false if it was already applied.</summary>
        public bool ApplyRejection()
        {
            if (!session.applicationOpen) return false;
            session.applicationOpen = false;
            choices.ConsumeApplication("Rejected");
            return true;
        }

        public void AdvanceDay() { session.day++; }

        public bool IsFinalDay() { return !choices.HasValidApplication(); }
    }
}
