using System;

namespace FirstJobber
{
    /// <summary>A UI string written in code: stable key + English source. Text is the localized version.</summary>
    public readonly struct UiString
    {
        public readonly string key;
        public readonly string english;

        public UiString(string key, string english)
        {
            this.key = key;
            this.english = english;
        }

        public string Text => LocalizationManager.Get(key, english);

        /// <summary>string.Format on the localized text; falls back to the English format if a translation has bad placeholders.</summary>
        public string Format(params object[] args) { return UiText.Format(Text, english, args); }
    }

    /// <summary>Registry of every UI string that lives in code (not in a ScriptableObject). The exporter lists All.</summary>
    public static class UiText
    {
        public static readonly UiString Day = new UiString("ui/day", "Day {0}");
        public static readonly UiString Gender = new UiString("ui/gender", "Gender: {0}");
        public static readonly UiString NothingLeft = new UiString("ui/resume/nothing_left", "(nothing left)");
        public static readonly UiString NothingToChoose = new UiString("ui/resume/nothing_to_choose", "Nothing to choose.");
        public static readonly UiString Rejected = new UiString("ui/company/rejected", "REJECTED");
        public static readonly UiString NoCompaniesLeft = new UiString("ui/company/none_left", "No companies left.");
        public static readonly UiString SelectCompany = new UiString("ui/company/select_prompt", "Select a company to see its details.");
        public static readonly UiString ChooseCompany = new UiString("ui/company/choose", "Choose a company");
        public static readonly UiString SalaryNotSpecified = new UiString("ui/company/salary_not_specified", "Salary not specified");
        public static readonly UiString WhatWillYouDo = new UiString("ui/final/title", "What will you do?");
        public static readonly UiString NoEndings = new UiString("ui/final/no_endings", "No endings configured.");
        public static readonly UiString ConfirmDecision = new UiString("ui/daily/confirm", "Confirm decision");
        public static readonly UiString BackToDecisions = new UiString("ui/daily/back", "Back");
        public static readonly UiString DayOver = new UiString("ui/sleep/day_over", "Day {0} is over...");
        public static readonly UiString TokenCompany = new UiString("ui/token/company", "the company");
        public static readonly UiString TokenJob = new UiString("ui/token/job", "the position");
        public static readonly UiString NameRequired = new UiString("ui/error/name_required", "Please enter a name.");
        public static readonly UiString NameTooLong = new UiString("ui/error/name_too_long", "Name is too long (max {0}).");
        public static readonly UiString GenderRequired = new UiString("ui/error/gender_required", "Please select a gender.");
        // ResumeConfidenceUI keeps its own serialized English text (emptyMessage / categoriesFormat); these are the defaults.
        public static readonly UiString ConfidenceEmpty = new UiString("ui/confidence/empty", "No choices selected");
        public static readonly UiString ConfidenceCategories = new UiString("ui/confidence/categories", "{0} / {1} categories");

        public static readonly UiString[] All =
        {
            Day, Gender, NothingLeft, NothingToChoose, Rejected, NoCompaniesLeft, SelectCompany, WhatWillYouDo, NoEndings,
            DayOver, TokenCompany, TokenJob, NameRequired, NameTooLong, GenderRequired, ConfidenceEmpty, ConfidenceCategories,
            ConfirmDecision, BackToDecisions, ChooseCompany, SalaryNotSpecified
        };

        public static string Format(string format, string englishFormat, params object[] args)
        {
            try { return string.Format(format, args); }
            catch (FormatException) { return string.Format(englishFormat, args); }
        }
    }
}
