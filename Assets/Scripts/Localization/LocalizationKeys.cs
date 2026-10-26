using System;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>
    /// The localization key scheme. Keys are derived from existing data, so they stay stable as long as ids do:
    ///   &lt;type&gt;/&lt;id&gt;/&lt;field&gt;                         category/cat_skills/displayName, company/acme/companyName
    ///   &lt;type&gt;/&lt;id&gt;/&lt;arrayField&gt;/&lt;index&gt;/text         choice/c1/playerReaction/0/text   (DialogueLine entries)
    ///   question/&lt;id&gt;/answers/&lt;answerId&gt;/&lt;field&gt;      question/q1/answers/a1/text, .../a1/hrResponse/0/text
    ///   database/&lt;arrayField&gt;/&lt;index&gt;/text               database/rejectionHR/0/text
    ///   speaker/&lt;speaker name&gt;                         speaker/HR, speaker/Father  (not "Player": that shows the applicant's name)
    ///   ui/...                                        strings written in code, see UiText
    ///   ui/label/&lt;hierarchy path&gt;                      static scene labels, see LocalizedLabel
    /// &lt;type&gt; is category | choice | company | question | event | ending; &lt;id&gt; is the asset's id field (asset name if empty).
    /// </summary>
    public static class LocalizationKeys
    {
        public static string Of(ScriptableObject asset, string field)
        {
            return Prefix(asset) + "/" + field;
        }

        public static string Prefix(ScriptableObject asset)
        {
            switch (asset)
            {
                case ResumeCategoryData a: return "category/" + IdOr(a.id, a);
                case ResumeChoiceData a: return "choice/" + IdOr(a.id, a);
                case CompanyData a: return "company/" + IdOr(a.id, a);
                case InterviewQuestionData a: return "question/" + IdOr(a.id, a);
                case DailyEventData a: return "event/" + IdOr(a.id, a);
                case EndingData a: return "ending/" + IdOr(a.id, a);
                case GameDatabase _: return "database";
                default: return "asset/" + asset.name;
            }
        }

        static string IdOr(string id, ScriptableObject asset) { return string.IsNullOrEmpty(id) ? asset.name : id; }

        /// <summary>Key of a field of an interview answer, e.g. Answer(q, a, 0, "text").</summary>
        public static string Answer(InterviewQuestionData q, InterviewAnswer a, int index, string field)
        {
            string id = a != null && !string.IsNullOrEmpty(a.id) ? a.id : index.ToString();
            return Prefix(q) + "/answers/" + id + "/" + field;
        }

        /// <summary>
        /// Calls add(key, english) for every translatable string of the database and all assets it lists, and gives
        /// every DialogueLine its runtime key (DialogueLine.locKey). add may be null (= only stamp the keys).
        /// </summary>
        public static void Visit(GameDatabase db, Action<string, string> add)
        {
            if (db == null) return;
            VisitAsset(db, add);
            foreach (var a in db.categories) VisitAsset(a, add);
            foreach (var a in db.choices) VisitAsset(a, add);
            foreach (var a in db.companies) VisitAsset(a, add);
            foreach (var a in db.questions) VisitAsset(a, add);
            foreach (var a in db.events) VisitAsset(a, add);
            foreach (var a in db.endings) VisitAsset(a, add);
        }

        /// <summary>Same as Visit for one asset (also used by the exporter for assets that are not in the database).</summary>
        public static void VisitAsset(ScriptableObject asset, Action<string, string> add)
        {
            switch (asset)
            {
                case GameDatabase d:
                    Emit(GameDatabase.GenderPromptKey, d.genderPrompt, add);
                    if (d.genders != null)
                        foreach (var g in d.genders)
                            if (g != null && !string.IsNullOrWhiteSpace(g.id)) Emit(GameDatabase.GenderKey(g.id), g.displayName, add);
                    Lines(d, "resumeIntroLines", d.resumeIntroLines, add);
                    Lines(d, "defaultHrIntro", d.defaultHrIntro, add);
                    Lines(d, "hrReviewLines", d.hrReviewLines, add);
                    Lines(d, "rejectionHR", d.rejectionHR, add);
                    Lines(d, "rejectionPlayer", d.rejectionPlayer, add);
                    Lines(d, "fatherOfferLines", d.fatherOfferLines, add);
                    break;
                case ResumeCategoryData c:
                    Text(c, "displayName", c.displayName, add);
                    Text(c, "emptyMessage", c.emptyMessage, add);
                    break;
                case ResumeChoiceData c:
                    Text(c, "displayText", c.displayText, add);
                    Text(c, "description", c.description, add);
                    Lines(c, "playerReaction", c.playerReaction, add);
                    Lines(c, "hrReactions", c.hrReactions, add);
                    break;
                case CompanyData c:
                    Text(c, "companyName", c.companyName, add);
                    Text(c, "jobTitle", c.jobTitle, add);
                    Text(c, "salary", c.salary, add);
                    Text(c, "industry", c.industry, add);
                    Text(c, "description", c.description, add);
                    Lines(c, "playerReaction", c.playerReaction, add);
                    Lines(c, "hrIntro", c.hrIntro, add);
                    Lines(c, "rejection", c.rejection, add);
                    break;
                case InterviewQuestionData q:
                    Text(q, "questionText", q.questionText, add);
                    if (q.answers == null) break;
                    for (int i = 0; i < q.answers.Count; i++)
                    {
                        var a = q.answers[i];
                        if (a == null) continue;
                        Emit(Answer(q, a, i, "text"), a.text, add);
                        LinesWithPrefix(Answer(q, a, i, "playerReaction"), a.playerReaction, add);
                        LinesWithPrefix(Answer(q, a, i, "hrResponse"), a.hrResponse, add);
                    }
                    break;
                case DailyEventData e:
                    Text(e, "caption", e.caption, add);
                    Lines(e, "lines", e.lines, add);
                    break;
                case EndingData e:
                    Text(e, "title", e.title, add);
                    Text(e, "optionLabel", e.optionLabel, add);
                    Text(e, "optionDescription", e.optionDescription, add);
                    Lines(e, "lines", e.lines, add);
                    Text(e, "finalText", e.finalText, add);
                    break;
            }
        }

        static void Text(ScriptableObject owner, string field, string english, Action<string, string> add)
        {
            Emit(Of(owner, field), english, add);
        }

        static void Lines(ScriptableObject owner, string field, DialogueLine[] lines, Action<string, string> add)
        {
            LinesWithPrefix(Of(owner, field), lines, add);
        }

        static void LinesWithPrefix(string prefix, DialogueLine[] lines, Action<string, string> add)
        {
            if (lines == null) return;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line == null) continue;
                line.locKey = prefix + "/" + i + "/text";
                for (int p = 0; p < line.PageCount; p++)
                    Emit(line.GetPageKey(p), line.GetPage(p), add);
                if (!string.IsNullOrEmpty(line.speaker) && line.speaker != Speakers.Player)
                    Emit(SpeakerKey(line.speaker), line.speaker, add);
            }
        }

        public static string SpeakerKey(string speaker) { return "speaker/" + speaker; }

        static void Emit(string key, string english, Action<string, string> add)
        {
            if (add != null && !string.IsNullOrWhiteSpace(english)) add(key, english);
        }
    }
}
