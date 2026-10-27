using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FirstJobber.EditorTools
{
    /// <summary>
    /// Imports ContentImport/HRProduction/hr_production_bilingual.json (5 companies, 5 HR, 15 Fixed Questions) into the
    /// EXISTING CompanyData / HRCharacterData / InterviewQuestionData types and the EXISTING Thai table.
    /// English goes into the assets, Thai only into LocalizationTable_TH. Demo content is never touched.
    /// Safe to run again: assets are found by path (GUIDs stay), existing authored fields are kept, answers are matched by id.
    ///   20. Import HR Production Content
    ///   21. Validate HR Production Content   (read-only report)
    /// Resume-Linked questions are NOT assigned here: use '18. Assign Production Questions To Selected HR' per HR.
    /// </summary>
    public static class HrProductionImporter
    {
        const string JsonPath = "ContentImport/HRProduction/hr_production_bilingual.json"; // relative to the project root
        const string Root = SampleDataGenerator.ProductionRoot;
        const string CompanyFolder = Root + "/Companies";
        const string HrFolder = Root + "/HR";
        const string FixedFolder = Root + "/Interview/FixedQuestions";
        const string ProductionDbPath = Root + "/GameDatabase_Production.asset";
        const string TablePath = "Assets/Resources/Localization/LocalizationTable_TH.asset";
        const int FixedPerInterview = 2, LinkedPerInterview = 2;

        // ------------------------------------------------------------ JSON shape (JsonUtility)

        [Serializable] class Loc { public string english; public string thai; }
        [Serializable] class JBlock { public string speaker; public List<Loc> lines; }
        [Serializable] class JVariant { public List<string> genderIds; public List<JBlock> dialogueBlocks; }
        [Serializable] class JFixed { public string id; public Loc question; public List<Loc> answers; public bool productionReady; }
        [Serializable]
        class JHr
        {
            public string hrId; public Loc name;
            public List<JVariant> introductionVariants, rejectionVariants;
            public List<JFixed> fixedQuestions;
        }
        [Serializable] class JCompany { public string companyId; public Loc name; public List<Loc> jobDescription, workingConditions; public JHr hr; }
        [Serializable] class JRoot { public List<JCompany> companies; }

        // ------------------------------------------------------------ 20. import

        [MenuItem("Tools/First Jobber/20. Import HR Production Content")]
        public static void Import()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[HR] Leave Play mode first."); return; }
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", JsonPath));
            if (!File.Exists(full)) { Debug.LogError("[HR] JSON not found: " + JsonPath); return; }
            var root = JsonUtility.FromJson<JRoot>(File.ReadAllText(full, Encoding.UTF8));
            if (root == null || root.companies == null || root.companies.Count == 0) { Debug.LogError("[HR] JSON has no companies."); return; }

            var productionDb = AssetDatabase.LoadAssetAtPath<GameDatabase>(ProductionDbPath);
            if (productionDb == null) { Debug.LogError("[HR] Run '15. Import Production Content' first (no " + ProductionDbPath + ")."); return; }

            EnsureFolder(CompanyFolder); EnsureFolder(HrFolder); EnsureFolder(FixedFolder);
            var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(TablePath);
            if (table == null) { Debug.LogError("[HR] Thai table not found at " + TablePath); return; }

            int added = 0, kept = 0, createdAssets = 0;
            var companies = new List<CompanyData>();
            var hrs = new List<HRCharacterData>();

            void Translate(string key, string english, string thai)
            {
                if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(thai)) return;
                var e = table.entries.Find(x => x != null && x.key == key);
                if (e == null) { table.entries.Add(new LocalizationTable.Entry { key = key, english = english, thai = thai }); added++; }
                else if (string.IsNullOrWhiteSpace(e.thai)) { e.thai = thai; e.english = english; added++; }
                else kept++; // existing translations are never overwritten
            }

            foreach (var jc in root.companies)
            {
                var hr = GetOrCreate<HRCharacterData>(HrFolder + "/HR_P_" + jc.hr.hrId + ".asset", out bool hrNew);
                var company = GetOrCreate<CompanyData>(CompanyFolder + "/Company_P_" + jc.companyId + ".asset", out bool coNew);
                if (hrNew) createdAssets++;
                if (coNew) createdAssets++;

                // ---- company (only the imported fields; logo / jobTitle / salary / tags stay as authored)
                company.id = jc.companyId;
                company.companyName = jc.name.english;
                company.description = Bullets(jc.jobDescription, l => l.english) + "\n\n" + Bullets(jc.workingConditions, l => l.english);
                company.hr = hr;
                EditorUtility.SetDirty(company);
                Translate(LocalizationKeys.Of(company, "companyName"), jc.name.english, jc.name.thai);
                Translate(LocalizationKeys.Of(company, "description"), company.description,
                    Bullets(jc.jobDescription, l => l.thai) + "\n\n" + Bullets(jc.workingConditions, l => l.thai));

                // ---- HR identity
                hr.id = jc.hr.hrId;
                hr.hrName = jc.hr.name.english;
                hr.company = company;
                if (string.IsNullOrEmpty(hr.description)) hr.description = "Production HR of " + jc.name.english + ". Portrait / emotions not authored yet.";
                Translate(LocalizationKeys.Of(hr, "hrName"), jc.hr.name.english, jc.hr.name.thai);

                // ---- HR dialogue (only written when empty: hand-authored dialogue is never replaced)
                if (hr.introduction == null || hr.introduction.Length == 0)
                {
                    hr.introduction = BuildBlocks(jc.hr.introductionVariants);
                    hr.introMode = jc.hr.introductionVariants.Count > 1 ? DialoguePlaybackMode.RandomOne : DialoguePlaybackMode.Sequential;
                }
                if (hr.rejection == null || hr.rejection.Length == 0)
                {
                    hr.rejection = BuildBlocks(jc.hr.rejectionVariants);
                    hr.rejectionMode = jc.hr.rejectionVariants.Count > 1 ? DialoguePlaybackMode.RandomOne : DialoguePlaybackMode.Sequential;
                }
                LocalizationKeys.VisitAsset(hr, null); // stamps the runtime keys
                TranslateBlocks(hr.introduction, jc.hr.introductionVariants, Translate);
                TranslateBlocks(hr.rejection, jc.hr.rejectionVariants, Translate);

                // ---- Fixed Questions (separate pool, owned by this HR)
                foreach (var jq in jc.hr.fixedQuestions)
                {
                    if (!jq.productionReady || jq.answers == null || jq.answers.Count == 0)
                    {
                        Debug.LogWarning("[HR] Skipped '" + jq.id + "': not production ready / no answers.");
                        continue;
                    }
                    var q = GetOrCreate<InterviewQuestionData>(FixedFolder + "/Q_P_" + jq.id + ".asset", out bool qNew);
                    if (qNew) createdAssets++;
                    q.id = jq.id;
                    q.questionText = jq.question.english;
                    q.linkedChoice = null; q.linkedCategory = null;       // Fixed = not linked to the resume
                    ApplyAnswers(q, jq.answers);
                    EditorUtility.SetDirty(q);
                    Translate(LocalizationKeys.Of(q, "questionText"), jq.question.english, jq.question.thai);
                    for (int a = 0; a < jq.answers.Count; a++)
                    {
                        var answer = q.answers.Find(x => x.id == AnswerId(a));
                        Translate(LocalizationKeys.Answer(q, answer, q.answers.IndexOf(answer), "text"), jq.answers[a].english, jq.answers[a].thai);
                    }
                    if (!hr.fixedQuestions.Contains(q)) hr.fixedQuestions.Add(q);
                }

                // ---- interview plan: Fixed, Fixed, Linked, Linked (Inspector-editable). Only when no sequence exists yet.
                if (hr.sequence == null || hr.sequence.Count == 0)
                {
                    hr.sequence = new List<InterviewSlot>();
                    for (int i = 0; i < FixedPerInterview; i++) hr.sequence.Add(new InterviewSlot { type = InterviewSlotType.Fixed });
                    for (int i = 0; i < LinkedPerInterview; i++) hr.sequence.Add(new InterviewSlot { type = InterviewSlotType.ResumeLinked });
                    hr.fixedPickMode = QuestionPickMode.Random;
                    hr.resumeLinkedPickMode = QuestionPickMode.Random;
                }
                EditorUtility.SetDirty(hr);
                companies.Add(company);
                hrs.Add(hr);
            }

            // Production database: the 5 production companies + their HR (the demo database keeps its own 4).
            productionDb.companies = new List<CompanyData>(companies);
            productionDb.hrCharacters = new List<HRCharacterData>(hrs);
            productionDb.InvalidateLookups();
            EditorUtility.SetDirty(productionDb);
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[HR] Imported " + companies.Count + " companies / " + hrs.Count + " HR. New assets: " + createdAssets + ". Thai: " + added + " added, " + kept + " already translated (kept).");
            Validate();
            LocalizationTools.Export(); // refreshes the CSV (refuses, and says so, if it has un-imported edits)
        }

        static string AnswerId(int index) { return "a" + (index + 1); }

        static string Bullets(List<Loc> lines, Func<Loc, string> pick)
        {
            var sb = new StringBuilder();
            if (lines != null)
                foreach (var l in lines) { if (sb.Length > 0) sb.Append('\n'); sb.Append("• ").Append(pick(l)); }
            return sb.ToString();
        }

        // Every authored variant becomes ONE block (pages in order), tagged with its gender ids. The gender filter runs
        // before RandomOne in DialogueManager.Play, so a male applicant can only ever hear the male block.
        static DialogueLine[] BuildBlocks(List<JVariant> variants)
        {
            var result = new List<DialogueLine>();
            foreach (var v in variants)
                foreach (var b in v.dialogueBlocks)
                {
                    var line = new DialogueLine { speaker = string.IsNullOrEmpty(b.speaker) ? Speakers.HR : b.speaker, text = "" };
                    line.dialogueLines = new List<string>();
                    foreach (var l in b.lines) line.dialogueLines.Add(l.english);
                    line.genderIds = v.genderIds != null ? v.genderIds.ToArray() : new string[0];
                    result.Add(line);
                }
            return result.ToArray();
        }

        // Thai of block i / page p, in the same order BuildBlocks produced them.
        static void TranslateBlocks(DialogueLine[] blocks, List<JVariant> variants, Action<string, string, string> translate)
        {
            int i = 0;
            foreach (var v in variants)
                foreach (var b in v.dialogueBlocks)
                {
                    if (i >= blocks.Length) return;
                    for (int p = 0; p < b.lines.Count && p < blocks[i].PageCount; p++)
                        translate(blocks[i].GetPageKey(p), blocks[i].GetPage(p), b.lines[p].thai);
                    i++;
                }
        }

        // Answers keep their authored order and a1..aN ids; no HR follow-up or player reaction is invented.
        static void ApplyAnswers(InterviewQuestionData q, List<Loc> jsonAnswers)
        {
            var result = new List<InterviewAnswer>();
            for (int i = 0; i < jsonAnswers.Count; i++)
            {
                string id = AnswerId(i);
                var a = q.answers.Find(x => x != null && x.id == id) ?? new InterviewAnswer
                {
                    id = id, playerReaction = new DialogueLine[0], hrResponse = new DialogueLine[0]
                };
                a.text = jsonAnswers[i].english;
                result.Add(a);
            }
            foreach (var old in q.answers) if (old != null && !result.Contains(old)) result.Add(old); // keep anything added by hand
            q.answers = result;
        }

        // ------------------------------------------------------------ 21. validate (read-only)

        [MenuItem("Tools/First Jobber/21. Validate HR Production Content")]
        public static void Validate()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(ProductionDbPath);
            if (db == null) { Debug.LogError("[HR] No production database. Run menu 15 and 20 first."); return; }

            var problems = new List<string>();
            var report = new StringBuilder("[HR] Validation of " + db.name + ":");
            var ids = new HashSet<string>();
            var owner = new Dictionary<InterviewQuestionData, HRCharacterData>();
            var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(TablePath);
            int fixedTotal = 0, missingThai = 0;

            foreach (var co in db.companies)
            {
                if (co == null) continue;
                if (!ids.Add(co.id)) problems.Add("Duplicate company id " + co.id);
                if (co.hr == null) { problems.Add(co.name + " has no HR."); continue; }
                if (co.hr.company != co) problems.Add("Company/HR cross-link mismatch: " + co.name + " -> " + co.hr.name + " -> " + (co.hr.company != null ? co.hr.company.name : "none"));
                if (!db.hrCharacters.Contains(co.hr)) problems.Add(co.hr.name + " is not in the database HR list.");
            }

            foreach (var hr in db.hrCharacters)
            {
                if (hr == null) continue;
                if (!ids.Add(hr.id)) problems.Add("Duplicate HR id " + hr.id);
                report.Append("\n - ").Append(hr.name).Append(" (").Append(hr.company != null ? hr.company.name : "?").Append("): fixed ")
                      .Append(hr.fixedQuestions.Count).Append(", resume-linked ").Append(hr.resumeLinkedQuestions.Count);

                foreach (var q in hr.fixedQuestions)
                {
                    if (q == null) { problems.Add(hr.name + " has an empty Fixed Question slot."); continue; }
                    fixedTotal++;
                    if (!ids.Add(q.id)) problems.Add("Duplicate question id " + q.id);
                    if (owner.TryGetValue(q, out var other) && other != hr) problems.Add(q.name + " is shared by " + other.name + " and " + hr.name + ".");
                    owner[q] = hr;
                    if (q.answers == null || q.answers.Count == 0) problems.Add(q.id + " has no answers.");
                    if (q.linkedChoice != null || q.linkedCategory != null) problems.Add(q.id + " is a Fixed Question but has a resume link.");
                    if (table != null && !table.entries.Exists(e => e != null && e.key == LocalizationKeys.Of(q, "questionText") && !string.IsNullOrWhiteSpace(e.thai))) missingThai++;
                }
                if (hr.fixedQuestions.Count < FixedPerInterview) problems.Add(hr.name + " has fewer than " + FixedPerInterview + " Fixed Questions.");
                CheckPlan(hr, problems);
                CheckGenders(hr, db, problems);
                CheckLinkedCoverage(hr, db, problems, report);
            }
            if (missingThai > 0) problems.Add(missingThai + " Fixed Question(s) have no Thai text (English will be shown).");
            report.Append("\n - Fixed Questions total: ").Append(fixedTotal).Append(" (expected 15)");
            if (fixedTotal != 15) problems.Add("Expected 15 Fixed Questions, found " + fixedTotal + ".");

            if (problems.Count == 0) Debug.Log(report + "\nNo problems.");
            else Debug.LogWarning(report + "\nProblems / TODO:\n - " + string.Join("\n - ", problems));
        }

        static void CheckPlan(HRCharacterData hr, List<string> problems)
        {
            int f = 0, l = 0;
            foreach (var s in hr.EffectiveSequence()) { if (s == null) continue; if (s.type == InterviewSlotType.Fixed) f++; else l++; }
            if (f != FixedPerInterview || l != LinkedPerInterview) problems.Add(hr.name + " sequence is " + f + " Fixed + " + l + " Linked (production default is 2 + 2).");
            if (hr.fixedPickMode != QuestionPickMode.Random) problems.Add(hr.name + " Fixed Pick Mode is not Random.");
            if (hr.resumeLinkedPickMode != QuestionPickMode.Random) problems.Add(hr.name + " Resume Linked Pick Mode is not Random.");
        }

        // Every gender the applicant can pick must still hear something in each optional HR dialogue.
        static void CheckGenders(HRCharacterData hr, GameDatabase db, List<string> problems)
        {
            foreach (var g in db.genders)
            {
                if (g == null) continue;
                Check(hr, "introduction", hr.introduction, g.id, problems);
                Check(hr, "rejection", hr.rejection, g.id, problems);
            }
        }

        static void Check(HRCharacterData hr, string label, DialogueLine[] lines, string gender, List<string> problems)
        {
            if (lines == null || lines.Length == 0) return;
            foreach (var l in lines) if (l != null && l.HasContent && l.MatchesGender(gender)) return;
            problems.Add(hr.name + " has no " + label + " for gender '" + gender + "' (that part will be skipped; no neutral line authored).");
        }

        // Can this HR give 2 distinct matching questions for every possible 1-choice-per-category resume?
        static void CheckLinkedCoverage(HRCharacterData hr, GameDatabase db, List<string> problems, StringBuilder report)
        {
            if (hr.resumeLinkedQuestions.Count == 0)
            {
                problems.Add(hr.name + ": NO Resume-Linked Questions assigned -> only " + FixedPerInterview + " of " + (FixedPerInterview + LinkedPerInterview)
                             + " questions per interview. Select the HR asset and run '18. Assign Production Questions To Selected HR'.");
                return;
            }
            int worst = int.MaxValue;
            var cats = db.categories;
            var picks = new int[cats.Count];
            var byCat = new List<List<ResumeChoiceData>>();
            foreach (var c in cats) { var l = new List<ResumeChoiceData>(); foreach (var ch in db.choices) if (ch != null && ch.category == c) l.Add(ch); byCat.Add(l); }
            bool any = byCat.Count > 0 && byCat.TrueForAll(l => l.Count > 0);
            if (!any) return;
            while (true)
            {
                var selected = new List<ResumeChoiceData>();
                for (int i = 0; i < picks.Length; i++) selected.Add(byCat[i][picks[i]]);
                int matches = 0;
                foreach (var q in hr.resumeLinkedQuestions)
                {
                    if (q == null) continue;
                    if (q.linkedChoice != null ? selected.Contains(q.linkedChoice) : (q.linkedCategory != null && selected.Exists(c => c.category == q.linkedCategory))) matches++;
                }
                worst = Mathf.Min(worst, matches);
                int k = 0;
                while (k < picks.Length && ++picks[k] >= byCat[k].Count) picks[k++] = 0;
                if (k == picks.Length) break;
            }
            report.Append(", worst-case matches ").Append(worst);
            if (worst < LinkedPerInterview)
                problems.Add(hr.name + ": some resumes match only " + worst + " assigned Resume-Linked question(s) (< " + LinkedPerInterview + "); those interviews ask fewer questions.");
        }

        // ------------------------------------------------------------ helpers

        static T GetOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            created = existing == null;
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
