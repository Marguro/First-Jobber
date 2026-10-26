using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FirstJobber.EditorTools
{
    /// <summary>
    /// Imports ContentImport/FirstJobber_Production_Content/production_content_bilingual.json into the EXISTING
    /// ScriptableObject types and the EXISTING Thai localization table. English goes into the assets, Thai only into
    /// LocalizationTable_TH. Everything is created under Assets/Data/Production; demo content is never touched.
    /// Safe to run again: assets are found by path, answers by id, table rows by key.
    ///   15. Import Production Content
    ///   16. Use Production Database In Scene / 17. Use Demo Database In Scene
    ///   18. Assign Production Questions To Selected HR
    ///   19. Add Language Selector To Main Menu
    /// </summary>
    public static class ProductionContentImporter
    {
        const string JsonPath = "ContentImport/FirstJobber_Production_Content/production_content_bilingual.json"; // relative to the project root
        const string Root = SampleDataGenerator.ProductionRoot;
        const string CategoryFolder = Root + "/Resume/Categories";
        const string ChoiceFolder = Root + "/Resume/Choices";
        const string QuestionFolder = Root + "/Interview/ResumeLinkedQuestions";
        const string QuestionCopyFolder = Root + "/Interview/HR_Copies";
        const string ProductionDbPath = Root + "/GameDatabase_Production.asset";
        const string DemoDbPath = "Assets/Data/GameDatabase.asset";
        const string TablePath = "Assets/Resources/Localization/LocalizationTable_TH.asset";
        const string TodoConfidenceTag = "todo_confidence";

        // ------------------------------------------------------------ JSON shape (JsonUtility)

        [Serializable] class Loc { public string en; public string th; }
        [Serializable] class JLine { public string en; public string th; }
        [Serializable] class JBlock { public string speaker; public List<JLine> lines; }
        [Serializable] class JReaction { public string playbackMode; public List<JBlock> blocks; }
        [Serializable] class JAnswer { public string id; public Loc display; }
        [Serializable] class JQuestion { public string id; public Loc question; public List<JAnswer> answers; }
        [Serializable] class JChoice { public string id; public Loc display; public JReaction playerReaction; public JQuestion linkedQuestion; }
        [Serializable] class JCategory { public string id; public Loc display; public List<JChoice> choices; }
        [Serializable] class JRoot { public int schemaVersion; public List<JCategory> categories; }

        // ------------------------------------------------------------ 15. import

        [MenuItem("Tools/First Jobber/15. Import Production Content")]
        public static void Import()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Production] Leave Play mode first."); return; }
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", JsonPath));
            if (!File.Exists(full)) { Debug.LogError("[Production] JSON not found: " + JsonPath); return; }
            var root = JsonUtility.FromJson<JRoot>(File.ReadAllText(full, System.Text.Encoding.UTF8));
            if (root == null || root.categories == null || root.categories.Count == 0) { Debug.LogError("[Production] JSON has no categories."); return; }

            var demoDb = AssetDatabase.LoadAssetAtPath<GameDatabase>(DemoDbPath);
            if (demoDb == null) { Debug.LogError("[Production] Demo database not found at " + DemoDbPath + "."); return; }

            EnsureFolder(CategoryFolder); EnsureFolder(ChoiceFolder); EnsureFolder(QuestionFolder);
            var table = LoadOrCreateTable();
            var report = new ImportReport();

            var categories = new List<ResumeCategoryData>();
            var choices = new List<ResumeChoiceData>();
            var questions = new List<InterviewQuestionData>();

            foreach (var jc in root.categories)
            {
                var cat = GetOrCreate<ResumeCategoryData>(CategoryFolder + "/Cat_P_" + jc.id + ".asset", out bool catNew);
                cat.id = jc.id;
                cat.displayName = jc.display.en;
                EditorUtility.SetDirty(cat);
                categories.Add(cat);
                report.Translate(table, LocalizationKeys.Of(cat, "displayName"), jc.display);

                foreach (var jch in jc.choices)
                {
                    var choice = GetOrCreate<ResumeChoiceData>(ChoiceFolder + "/Choice_P_" + jch.id + ".asset", out bool choiceNew);
                    choice.id = jch.id;
                    choice.category = cat;
                    choice.displayText = jch.display.en;
                    if (choiceNew)
                    {
                        // Confidence was NOT authored: keep the neutral default and make the TODO visible.
                        choice.confidencePercentage = 50f;
                        choice.tags = new[] { TodoConfidenceTag };
                    }
                    ApplyReaction(choice, jch.playerReaction);
                    EditorUtility.SetDirty(choice);
                    choices.Add(choice);
                    report.Translate(table, LocalizationKeys.Of(choice, "displayText"), jch.display);

                    // stamp the runtime keys, then register each page under the key the game will really look up
                    LocalizationKeys.VisitAsset(choice, null);
                    var block = choice.playerReaction[0];
                    var pages = jch.playerReaction.blocks[0].lines;
                    for (int p = 0; p < pages.Count; p++)
                    {
                        report.Translate(table, block.GetPageKey(p), pages[p].en, pages[p].th);
                        report.ReactionPages++;
                    }

                    var jq = jch.linkedQuestion;
                    var q = GetOrCreate<InterviewQuestionData>(QuestionFolder + "/Q_P_" + jq.id + ".asset", out _);
                    q.id = jq.id;
                    q.questionText = jq.question.en;
                    q.linkedChoice = choice;     // exact choice match (strongest tier)
                    q.linkedCategory = null;
                    ApplyAnswers(q, jq.answers);
                    EditorUtility.SetDirty(q);
                    questions.Add(q);
                    report.Translate(table, LocalizationKeys.Of(q, "questionText"), jq.question);
                    for (int a = 0; a < jq.answers.Count; a++)
                    {
                        var answer = q.answers.Find(x => x.id == jq.answers[a].id);
                        report.Translate(table, LocalizationKeys.Answer(q, answer, q.answers.IndexOf(answer), "text"), jq.answers[a].display);
                        report.Answers++;
                    }
                }
            }

            BuildProductionDatabase(demoDb, categories, choices);
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Verify(root, categories, choices, questions, table, report);

            // Existing exporter: refreshes the CSV and reports coverage. It refuses (and says so) if the CSV has un-imported edits.
            LocalizationTools.Export();
        }

        static void ApplyReaction(ResumeChoiceData choice, JReaction reaction)
        {
            // ONE block, every page in order. Never RandomOne: the whole authored reaction must play.
            var pages = new List<string>();
            foreach (var b in reaction.blocks) foreach (var l in b.lines) pages.Add(l.en);

            DialogueLine block = null;
            if (choice.playerReaction != null && choice.playerReaction.Length > 0 && choice.playerReaction[0] != null
                && choice.playerReaction[0].speaker == Speakers.Player)
                block = choice.playerReaction[0];
            if (block == null)
            {
                block = new DialogueLine { speaker = Speakers.Player };
                var list = new List<DialogueLine> { block };
                if (choice.playerReaction != null) list.AddRange(choice.playerReaction);
                choice.playerReaction = list.ToArray();
            }
            block.text = "";
            block.dialogueLines = pages;
        }

        static void ApplyAnswers(InterviewQuestionData q, List<JAnswer> jsonAnswers)
        {
            var result = new List<InterviewAnswer>();
            foreach (var ja in jsonAnswers)
            {
                var a = q.answers.Find(x => x != null && x.id == ja.id) ?? new InterviewAnswer
                {
                    id = ja.id,
                    playerReaction = new DialogueLine[0], // not authored: empty is valid and cannot softlock
                    hrResponse = new DialogueLine[0]
                };
                a.text = ja.display.en;
                result.Add(a);
            }
            foreach (var old in q.answers) if (old != null && !result.Contains(old)) result.Add(old); // keep anything added by hand
            q.answers = result;
        }

        static void BuildProductionDatabase(GameDatabase demo, List<ResumeCategoryData> categories, List<ResumeChoiceData> choices)
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(ProductionDbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<GameDatabase>();
                EditorUtility.CopySerialized(demo, db); // same companies / HR / events / endings / shared dialogue (shared references, demo untouched)
                db.questions = new List<InterviewQuestionData>(); // the question pool is NOT attached to the shared bank
                AssetDatabase.CreateAsset(db, ProductionDbPath);
            }
            db.categories = new List<ResumeCategoryData>(categories);
            db.choices = new List<ResumeChoiceData>(choices);
            db.InvalidateLookups();
            EditorUtility.SetDirty(db);
        }

        // ------------------------------------------------------------ verification

        static void Verify(JRoot root, List<ResumeCategoryData> cats, List<ResumeChoiceData> choices,
            List<InterviewQuestionData> questions, LocalizationTable table, ImportReport r)
        {
            var problems = new List<string>();
            int pages = 0, answers = 0;
            var ids = new HashSet<string>();
            foreach (var c in choices) if (!ids.Add(c.id)) problems.Add("Duplicate choice id " + c.id);
            foreach (var q in questions) if (!ids.Add(q.id)) problems.Add("Duplicate question id " + q.id);
            foreach (var c in choices)
            {
                pages += c.playerReaction[0].PageCount;
                var q = questions.Find(x => x.linkedChoice == c);
                if (q == null) problems.Add("Choice " + c.id + " has no linked question.");
            }
            foreach (var q in questions)
            {
                answers += q.answers.Count;
                if (q.linkedChoice == null) problems.Add("Question " + q.id + " has no linked choice.");
            }
            var keys = new HashSet<string>();
            foreach (var e in table.entries) if (!keys.Add(e.key)) problems.Add("Duplicate localization key " + e.key);

            string summary = "[Production] categories " + cats.Count + "/3, choices " + choices.Count + "/15, linked questions " + questions.Count
                             + "/15, reaction pages " + pages + "/42, answers " + answers + "/42. Thai: " + r.Added + " added, " + r.Skipped
                             + " already translated (kept), " + r.Conflicts + " differing existing translations (kept).";
            if (cats.Count != 3 || choices.Count != 15 || questions.Count != 15 || pages != 42 || answers != 42)
                problems.Add("Counts differ from the expected 3 / 15 / 15 / 42 / 42.");
            if (problems.Count == 0) Debug.Log(summary + " No problems.");
            else Debug.LogWarning(summary + "\n - " + string.Join("\n - ", problems));
        }

        // ------------------------------------------------------------ localization table

        class ImportReport
        {
            public int Added, Skipped, Conflicts, ReactionPages, Answers;

            public void Translate(LocalizationTable table, string key, Loc loc) { Translate(table, key, loc.en, loc.th); }

            // English stays in the asset; only Thai is stored. Existing translations are never overwritten.
            public void Translate(LocalizationTable table, string key, string english, string thai)
            {
                if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(thai)) return;
                var entry = table.entries.Find(e => e != null && e.key == key);
                if (entry == null)
                {
                    table.entries.Add(new LocalizationTable.Entry { key = key, english = english, thai = thai });
                    Added++;
                    return;
                }
                if (string.IsNullOrWhiteSpace(entry.thai)) { entry.thai = thai; entry.english = english; Added++; }
                else if (entry.thai == thai) Skipped++;
                else { Conflicts++; Debug.LogWarning("[Production] Kept existing Thai for '" + key + "' (differs from the JSON)."); }
            }
        }

        static LocalizationTable LoadOrCreateTable()
        {
            var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(TablePath);
            if (table != null) return table;
            EnsureFolder("Assets/Resources/Localization");
            table = ScriptableObject.CreateInstance<LocalizationTable>();
            AssetDatabase.CreateAsset(table, TablePath);
            return table;
        }

        // ------------------------------------------------------------ 16 / 17 switch the scene's database

        [MenuItem("Tools/First Jobber/16. Use Production Database In Scene")]
        public static void UseProduction() { SwitchDatabase(ProductionDbPath); }

        [MenuItem("Tools/First Jobber/17. Use Demo Database In Scene")]
        public static void UseDemo() { SwitchDatabase(DemoDbPath); }

        static void SwitchDatabase(string path)
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(path);
            if (db == null) { Debug.LogError("[Production] No database at " + path + (path == ProductionDbPath ? ". Run '15. Import Production Content' first." : ".")); return; }
            int changed = 0;
            foreach (var flow in UnityEngine.Object.FindObjectsByType<GameFlowManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)) changed += SetDb(flow, db);
            foreach (var ev in UnityEngine.Object.FindObjectsByType<DailyEventManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)) changed += SetDb(ev, db);
            if (changed == 0) { Debug.LogWarning("[Production] No GameFlowManager / DailyEventManager with a 'database' field in the open scene."); return; }
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Production] Scene now uses '" + db.name + "' (" + changed + " reference(s)). Save the scene (Ctrl+S).");
        }

        static int SetDb(MonoBehaviour target, GameDatabase db)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty("database");
            if (prop == null) return 0;
            prop.objectReferenceValue = db;
            so.ApplyModifiedProperties();
            return 1;
        }

        // ------------------------------------------------------------ 18 assign to ONE explicitly selected HR

        [MenuItem("Tools/First Jobber/18. Assign Production Questions To Selected HR")]
        public static void AssignToSelectedHr()
        {
            var hr = Selection.activeObject as HRCharacterData;
            if (hr == null) { EditorUtility.DisplayDialog("Assign Production Questions", "Select exactly one HR Character asset in the Project window first.", "OK"); return; }

            var pool = new List<InterviewQuestionData>();
            foreach (var guid in AssetDatabase.FindAssets("t:InterviewQuestionData", new[] { QuestionFolder }))
                pool.Add(AssetDatabase.LoadAssetAtPath<InterviewQuestionData>(AssetDatabase.GUIDToAssetPath(guid)));
            pool.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            if (pool.Count == 0) { Debug.LogError("[Production] No questions in " + QuestionFolder + ". Run '15. Import Production Content' first."); return; }

            if (!EditorUtility.DisplayDialog("Assign Production Questions",
                    "Add the " + pool.Count + " production Resume-Linked questions to '" + hr.DisplayName + "' only?\n\nQuestions already used by another HR are copied (own id) instead of shared.",
                    "Assign", "Cancel")) return;
            AssignTo(hr, pool);
        }

        /// <summary>Adds the pool to ONE HR. Questions another HR already uses are copied (own id, Thai carried over).</summary>
        public static void AssignTo(HRCharacterData hr, List<InterviewQuestionData> pool)
        {
            var users = FindQuestionUsers();
            var table = LoadOrCreateTable();
            Undo.RecordObject(hr, "Assign production questions");
            int assigned = 0, copied = 0, already = 0;
            foreach (var template in pool)
            {
                if (hr.resumeLinkedQuestions.Exists(x => x != null && (x == template || x.id.StartsWith(template.id + "__", StringComparison.Ordinal)))) { already++; continue; }

                InterviewQuestionData toAdd = template;
                if (users.TryGetValue(template, out var owner) && owner != hr)
                {
                    toAdd = CopyForHr(template, hr, table);
                    copied++;
                }
                else assigned++;
                hr.resumeLinkedQuestions.Add(toAdd);
            }
            EditorUtility.SetDirty(hr);
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log("[Production] '" + hr.DisplayName + "': " + assigned + " assigned, " + copied + " copied (already used by another HR), " + already + " already present.", hr);
        }

        static InterviewQuestionData CopyForHr(InterviewQuestionData template, HRCharacterData hr, LocalizationTable table)
        {
            string tag = string.IsNullOrEmpty(hr.id) ? hr.name : hr.id;
            string path = QuestionCopyFolder + "/" + hr.name + "/Q_P_" + template.id + "__" + tag + ".asset";
            EnsureFolder(QuestionCopyFolder + "/" + hr.name);
            var copy = AssetDatabase.LoadAssetAtPath<InterviewQuestionData>(path);
            if (copy == null)
            {
                copy = UnityEngine.Object.Instantiate(template);
                copy.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(copy, path);
            }
            copy.id = template.id + "__" + tag;
            copy.linkedChoice = template.linkedChoice;
            EditorUtility.SetDirty(copy);

            // The copy has its own keys: carry the Thai over (both key lists are produced in the same order).
            var from = KeysOf(template);
            var to = KeysOf(copy);
            for (int i = 0; i < from.Count && i < to.Count; i++)
            {
                var src = table.entries.Find(e => e != null && e.key == from[i]);
                if (src == null || string.IsNullOrWhiteSpace(src.thai)) continue;
                if (table.entries.Exists(e => e != null && e.key == to[i])) continue;
                table.entries.Add(new LocalizationTable.Entry { key = to[i], english = src.english, thai = src.thai });
            }
            return copy;
        }

        static List<string> KeysOf(ScriptableObject asset)
        {
            var keys = new List<string>();
            LocalizationKeys.VisitAsset(asset, (k, _) => keys.Add(k));
            return keys;
        }

        static Dictionary<InterviewQuestionData, HRCharacterData> FindQuestionUsers()
        {
            var map = new Dictionary<InterviewQuestionData, HRCharacterData>();
            foreach (var guid in AssetDatabase.FindAssets("t:HRCharacterData"))
            {
                var other = AssetDatabase.LoadAssetAtPath<HRCharacterData>(AssetDatabase.GUIDToAssetPath(guid));
                if (other == null) continue;
                foreach (var q in other.fixedQuestions) if (q != null && !map.ContainsKey(q)) map[q] = other;
                foreach (var q in other.resumeLinkedQuestions) if (q != null && !map.ContainsKey(q)) map[q] = other;
                if (other.fallbackQuestion != null && !map.ContainsKey(other.fallbackQuestion)) map[other.fallbackQuestion] = other;
            }
            return map;
        }

        // ------------------------------------------------------------ 19 language selector

        [MenuItem("Tools/First Jobber/19. Add Language Selector To Main Menu")]
        public static void AddLanguageSelector()
        {
            var menu = UnityEngine.Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
            if (menu == null) { Debug.LogError("[Menu] No MainMenuController in the open scene."); return; }
            if (menu.GetComponentInChildren<LanguageDropdown>(true) != null) { Debug.Log("[Menu] The main menu already has a language selector."); return; }
            var panel = (RectTransform)menu.transform;
            GameSceneBuilder.AddLanguageSelector(panel);
            var created = panel.GetComponentInChildren<LanguageDropdown>(true);
            if (created != null) Undo.RegisterCreatedObjectUndo(created.gameObject, "Add language selector");
            EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
            Debug.Log("[Menu] Language selector added to '" + panel.name + "'. Save the scene (Ctrl+S).");
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
