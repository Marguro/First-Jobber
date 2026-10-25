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
    }
}
