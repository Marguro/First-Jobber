namespace FirstJobber
{
    /// <summary>Everything a controller needs for the current playthrough. Rebuilt on New Game / Continue.</summary>
    public class GameContext
    {
        public GameDatabase Database;
        public GameSession Session;
        public ChoiceManager Choices;
        public LoopManager Loop;
        public DialogueManager Dialogue;

        public GameContext(GameDatabase database, GameSession session, DialogueManager dialogue)
        {
            Database = database;
            Session = session;
            Dialogue = dialogue;
            Choices = new ChoiceManager(database, session);
            Loop = new LoopManager(session, Choices);
        }

        public CompanyData SelectedCompany => Database.FindCompany(Session.selectedCompanyId);

        /// <summary>The HR of the selected company; the database fallback HR only when the company has none. Null = legacy shared interview.</summary>
        public HRCharacterData SelectedHr
        {
            get
            {
                var company = SelectedCompany;
                if (company == null) return null;
                return company.hr != null ? company.hr : Database.fallbackHr;
            }
        }
    }
}
