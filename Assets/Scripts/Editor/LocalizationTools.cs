using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace FirstJobber.EditorTools
{
    /// <summary>
    /// Tools > First Jobber > 12. Export Translation Template / 13. Import Translation CSV / 14. Setup Thai Font.
    /// The English text in the ScriptableObjects stays the source; the table only holds the Thai text.
    /// </summary>
    public static class LocalizationTools
    {
        const string TableFolder = "Assets/Resources/Localization";
        const string TablePath = TableFolder + "/LocalizationTable_TH.asset";
        const string CsvFolder = "Assets/Localization";
        const string CsvPath = CsvFolder + "/translation_template.csv";
        const string ScenePath = "Assets/Scenes/Game.unity";
        const string FontName = "MN Cha Nom Khaimuk";
        const string DoctorExamplePath = "Packages/com.phengine.thaitextcare/Examples/ThaiFontDoctor.asset";

        // ------------------------------------------------------------ 12. export

        [MenuItem("Tools/First Jobber/12. Export Translation Template")]
        public static void Export()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Localization] Leave Play mode first."); return; }
            var db = FindDatabase();
            if (db == null) { Debug.LogError("[Localization] No GameDatabase found."); return; }

            // 1. Collect every (key, english) in a stable order.
            var keys = new List<string>();
            var english = new Dictionary<string, string>();
            void Add(string k, string text)
            {
                if (english.TryGetValue(k, out var existing))
                {
                    if (existing != text) Debug.LogWarning("[Localization] Key '" + k + "' has two different English texts; keeping the first.");
                    return;
                }
                english[k] = text;
                keys.Add(k);
            }

            LocalizationKeys.Visit(db, Add);
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Data" }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && !(asset is GameDatabase)) LocalizationKeys.VisitAsset(asset, Add); // assets not (yet) in the database
            }
            LocalizationKeys.VisitAsset(InterviewQuestionSelector.GetEmergencyQuestion(), Add);
            foreach (var u in UiText.All) Add(u.key, u.english);
            int labelCount = ScanSceneLabels(Add);

            // 2. Merge with the table (existing Thai is kept).
            var table = LoadOrCreateTable();
            var oldByKey = new Dictionary<string, LocalizationTable.Entry>();
            foreach (var e in table.entries) if (e != null && !string.IsNullOrEmpty(e.key)) oldByKey[e.key] = e;

            // Refuse to overwrite CSV edits that were never imported.
            if (File.Exists(CsvPath))
            {
                int pending = 0;
                foreach (var row in ParseCsv(File.ReadAllText(CsvPath, Encoding.UTF8)))
                {
                    if (row.Count < 3 || row[0] == "key" || string.IsNullOrWhiteSpace(row[2])) continue;
                    oldByKey.TryGetValue(row[0], out var entry);
                    if (entry == null || entry.thai != row[2]) pending++;
                }
                if (pending > 0)
                {
                    Debug.LogError("[Localization] Export cancelled: " + CsvPath + " has " + pending + " Thai text(s) that are not in the table yet. "
                                   + "Run '13. Import Translation CSV' first (the CSV wins), or delete the CSV (the table wins).");
                    return;
                }
            }

            var merged = new List<LocalizationTable.Entry>(keys.Count);
            int changedEnglish = 0, translated = 0;
            foreach (var key in keys)
            {
                oldByKey.TryGetValue(key, out var old);
                string thai = old != null ? old.thai : "";
                if (old != null && old.english != english[key] && !string.IsNullOrEmpty(thai))
                {
                    changedEnglish++;
                    Debug.LogWarning("[Localization] English changed, check the Thai text of '" + key + "'.");
                }
                if (!string.IsNullOrWhiteSpace(thai)) translated++;
                merged.Add(new LocalizationTable.Entry { key = key, english = english[key], thai = thai });
            }

            // 3. Keys that no longer exist: reported; ones that already have Thai text are kept at the end so nothing is lost.
            int obsolete = 0;
            foreach (var pair in oldByKey)
            {
                if (english.ContainsKey(pair.Key)) continue;
                obsolete++;
                bool hasThai = !string.IsNullOrWhiteSpace(pair.Value.thai);
                Debug.LogWarning("[Localization] Key no longer exists: '" + pair.Key + "'" + (hasThai ? " (has Thai text, kept at the end of the table)" : " (removed)"));
                if (hasThai) merged.Add(pair.Value);
            }

            table.entries = merged;
            EditorUtility.SetDirty(table);

            // 4. CSV for spreadsheets (UTF-8 with BOM so Excel shows Thai correctly).
            Directory.CreateDirectory(CsvFolder);
            var sb = new StringBuilder("key,english,thai\r\n");
            foreach (var key in keys)
            {
                string thai = "";
                foreach (var e in merged) if (e.key == key) { thai = e.thai; break; }
                sb.Append(CsvField(key)).Append(',').Append(CsvField(english[key])).Append(',').Append(CsvField(thai)).Append("\r\n");
            }
            File.WriteAllText(CsvPath, sb.ToString(), new UTF8Encoding(true));
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(CsvPath);

            Debug.Log("[Localization] Exported " + keys.Count + " keys (" + translated + " translated, " + labelCount + " scene labels, "
                      + changedEnglish + " with changed English, " + obsolete + " obsolete) to " + CsvPath + " and " + TablePath + ".", table);
        }

        /// <summary>Adds the static TMP labels of the Game scene (open or saved). Returns how many were found.</summary>
        static int ScanSceneLabels(Action<string, string> add)
        {
            if (!File.Exists(ScenePath)) { Debug.LogWarning("[Localization] " + ScenePath + " not found; scene labels were not exported."); return 0; }
            var scene = EditorSceneManager.GetSceneByPath(ScenePath);
            bool openedHere = false;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                openedHere = true;
            }

            int count = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (!LocalizedLabel.ShouldLocalize(t)) continue;
                    add(LocalizedLabel.KeyFor(t), t.text);
                    count++;
                }
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
            return count;
        }

        // ------------------------------------------------------------ 13. import

        [MenuItem("Tools/First Jobber/13. Import Translation CSV")]
        public static void Import()
        {
            if (!File.Exists(CsvPath)) { Debug.LogError("[Localization] " + CsvPath + " not found. Run '12. Export Translation Template' first."); return; }
            var table = LoadOrCreateTable();
            var byKey = new Dictionary<string, LocalizationTable.Entry>();
            foreach (var e in table.entries) if (e != null && !string.IsNullOrEmpty(e.key)) byKey[e.key] = e;

            int updated = 0, added = 0, empty = 0;
            foreach (var row in ParseCsv(File.ReadAllText(CsvPath, Encoding.UTF8)))
            {
                if (row.Count < 2 || row[0] == "key") continue;
                string thai = row.Count > 2 ? row[2] : "";
                if (!byKey.TryGetValue(row[0], out var entry))
                {
                    Debug.LogWarning("[Localization] Unknown key in CSV (added to the table): '" + row[0] + "'.");
                    entry = new LocalizationTable.Entry { key = row[0], english = row[1] };
                    table.entries.Add(entry);
                    byKey[entry.key] = entry;
                    added++;
                }
                if (entry.thai != thai) updated++;
                entry.thai = thai;
                if (string.IsNullOrWhiteSpace(thai)) empty++;
            }
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log("[Localization] Imported " + CsvPath + ": " + updated + " changed, " + added + " added, " + empty + " still untranslated (they show English).", table);
        }

        // ------------------------------------------------------------ 14. thai font

        [MenuItem("Tools/First Jobber/14. Setup Thai Font")]
        public static void SetupThaiFont()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Localization] Leave Play mode first."); return; }

            Font font = null;
            foreach (var guid in AssetDatabase.FindAssets("t:Font " + FontName))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(p) == FontName) { font = AssetDatabase.LoadAssetAtPath<Font>(p); break; }
            }
            if (font == null) { Debug.LogError("[Localization] Font '" + FontName + "' (.ttf) not found."); return; }

            var thaiChars = new StringBuilder();
            for (int c = 0x0E01; c <= 0x0E5B; c++) if (c <= 0x0E3A || c >= 0x0E3F) thaiChars.Append((char)c);
            int missingThai = 0;
            foreach (char c in thaiChars.ToString()) if (!font.HasCharacter(c)) missingThai++;
            if (missingThai == thaiChars.Length) { Debug.LogError("[Localization] '" + FontName + "' contains no Thai glyphs. Pick another font."); return; }

            var c90 = new StringBuilder();
            for (int c = 0xF700; c <= 0xF71A; c++) if (font.HasCharacter((char)c)) c90.Append((char)c);
            bool hasC90 = c90.Length == 0x1B;

            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(font)).Replace('\\', '/');
            string assetName = FontName + " Thai SDF";
            string fontPath = folder + "/" + assetName + ".asset";

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (fontAsset == null)
            {
                fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                fontAsset.name = assetName;
                AssetDatabase.CreateAsset(fontAsset, fontPath);
                if (fontAsset.material != null)
                {
                    fontAsset.material.name = assetName + " Material";
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                }
                if (fontAsset.atlasTextures != null)
                    foreach (var tex in fontAsset.atlasTextures)
                    {
                        tex.name = assetName + " Atlas";
                        AssetDatabase.AddObjectToAsset(tex, fontAsset);
                    }
            }

            // Bake the glyphs we know we need so the atlas is already filled in builds (more are added on demand).
            var wanted = new StringBuilder();
            for (int c = 32; c <= 126; c++) wanted.Append((char)c);
            wanted.Append(thaiChars).Append('​').Append(c90);
            fontAsset.TryAddCharacters(wanted.ToString(), out string notAdded);
            EditorUtility.SetDirty(fontAsset);
            if (fontAsset.atlasTextures != null) foreach (var tex in fontAsset.atlasTextures) EditorUtility.SetDirty(tex);

            // The table references the font, so it ships with the build (the table lives in Resources).
            var table = LoadOrCreateTable();
            table.font = fontAsset;
            table.correction = hasC90 ? PhEngine.ThaiTextCare.ThaiGlyphCorrection.FullC90 : PhEngine.ThaiTextCare.ThaiGlyphCorrection.None;
            EditorUtility.SetDirty(table);

            // ThaiFontDoctor (overlapping vowels / tone marks): a copy of the package example, pointed at our font. Not applied automatically.
            string doctorPath = folder + "/ThaiFontDoctor_" + FontName.Replace(" ", "") + ".asset";
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(doctorPath) == null)
            {
                if (AssetDatabase.CopyAsset(DoctorExamplePath, doctorPath))
                {
                    var doctor = AssetDatabase.LoadAssetAtPath<PhEngine.ThaiTextCare.Editor.ThaiFontDoctor>(doctorPath);
                    if (doctor != null) { doctor.SetFontAsset(fontAsset); EditorUtility.SetDirty(doctor); }
                }
                else Debug.LogWarning("[Localization] Could not copy the ThaiFontDoctor example; create one via Assets > Create > ThaiTextCare > ThaiFontDoctor.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Localization] Thai font ready: " + fontPath + ". TTF missing " + missingThai + "/" + thaiChars.Length + " Thai glyphs"
                      + (string.IsNullOrEmpty(notAdded) ? "" : ", could not bake " + notAdded.Length + " characters")
                      + ". C90 glyphs U+F700-F71A in the TTF: " + c90.Length + "/27 -> correction " + table.correction + ".", fontAsset);
        }

        // ------------------------------------------------------------ helpers

        static GameDatabase FindDatabase()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Data/GameDatabase.asset");
            if (db != null) return db;
            var guids = AssetDatabase.FindAssets("t:GameDatabase");
            return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<GameDatabase>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
        }

        static LocalizationTable LoadOrCreateTable()
        {
            var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(TablePath);
            if (table != null) return table;

            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(TableFolder)) AssetDatabase.CreateFolder("Assets/Resources", "Localization");
            table = ScriptableObject.CreateInstance<LocalizationTable>();
            AssetDatabase.CreateAsset(table, TablePath);
            return table;
        }

        static string CsvField(string value)
        {
            value = (value ?? "").Replace("\r\n", "\n");
            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>RFC 4180 parser: quoted fields may contain commas, doubled quotes and line breaks; \r\n inside fields becomes \n.</summary>
        static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;
            text = text.TrimStart('﻿');

            void EndField() { row.Add(field.ToString().Replace("\r\n", "\n")); field.Clear(); }
            void EndRow()
            {
                EndField();
                if (!(row.Count == 1 && row[0].Length == 0)) rows.Add(row); // skip blank lines
                row = new List<string>();
            }

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c != '"') field.Append(c);
                    else if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else quoted = false;
                }
                else if (c == '"') quoted = true;
                else if (c == ',') EndField();
                else if (c == '\n') EndRow();
                else if (c == '\r') { if (i + 1 >= text.Length || text[i + 1] != '\n') EndRow(); }
                else field.Append(c);
            }
            if (field.Length > 0 || row.Count > 0) EndRow();
            return rows;
        }
    }
}
