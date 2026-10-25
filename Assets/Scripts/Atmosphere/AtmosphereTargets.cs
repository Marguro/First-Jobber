namespace FirstJobber
{
    /// <summary>
    /// Target Ids of the UI sprites that can change per atmosphere phase.
    /// A phase asset lists a sprite per id (Sprite Overrides); an AtmosphereSpriteBinding with the same id on a UI Image receives it.
    /// You can invent your own ids too - this list only drives the "Add Missing UI Slots" button and the sample phases.
    /// </summary>
    public static class AtmosphereTargets
    {
        public const string MainBackground = "MainBackground";
        public const string MenuPaper = "MenuPaper";
        public const string SubmitButton = "SubmitButton";
        public const string ResumeBackground = "ResumeBackground";
        public const string ChoiceListBackground = "ChoiceListBackground";
        public const string ConfirmButton = "ConfirmButton";
        public const string ChoiceButton = "ChoiceButton";
        public const string CompanyPanelBackground = "CompanyPanelBackground";
        public const string InterviewBackground = "InterviewBackground";
        public const string UIDecoration = "UIDecoration"; // the dialogue box

        public static readonly string[] Known =
        {
            MainBackground, MenuPaper, SubmitButton, ResumeBackground, ChoiceListBackground, ConfirmButton, ChoiceButton,
            CompanyPanelBackground, InterviewBackground, UIDecoration,
            ResumeConfidenceUI.AtmosphereBackgroundId, ResumeConfidenceUI.AtmosphereFillId,
            ResumeConfidenceUI.AtmosphereFrameId, ResumeConfidenceUI.AtmosphereDecorationId
        };
    }
}
