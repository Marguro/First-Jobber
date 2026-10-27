namespace FirstJobber
{
    public enum GamePhase
    {
        MainMenu,
        CreateApplicant,
        ResumeEditor,
        CompanySelection,
        InterviewIntroduction,
        InterviewResumeReview,
        InterviewQuestion,
        InterviewReaction,
        Rejection,
        Sleep,
        DayTransition,
        DailyEvent,
        FinalDecision,
        Ending
    }

    public enum EventTrigger
    {
        OnDayStart,
        BeforeResume,
        AfterResumeConfirmed,
        AfterCompanySelected,
        BeforeInterview,
        AfterInterview,
        AfterRejection,
        BeforeSleep,
        OnEnterResume,
        OnEnterCompanySelection
    }
}
