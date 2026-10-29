using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FirstJobber.EditorTools
{
    // Editor-only packaging; runtime never searches the AssetDatabase or filesystem.
    public sealed class EndingVideoPackaging : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        const string Folder = "Assets/StreamingAssets/EndingVideos";
        public int callbackOrder => 100;

        static EndingData[] Videos()
        {
            var matches = AssetDatabase.FindAssets("GameDatabase_Production t:GameDatabase");
            if (matches.Length != 1) throw new BuildFailedException("Expected one GameDatabase_Production.");
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(AssetDatabase.GUIDToAssetPath(matches[0]));
            return db.endings.Where(e => e != null && e.presentationMode == EndingPresentationMode.Video).ToArray();
        }

        static bool EqualFiles(string a, string b)
        {
            using (var sha = SHA256.Create())
            using (var first = File.OpenRead(a))
            using (var second = File.OpenRead(b))
                return sha.ComputeHash(first).SequenceEqual(sha.ComputeHash(second));
        }

        [MenuItem("Tools/First Jobber/Ending Videos/Package Production Videos")]
        public static void Package()
        {
            Directory.CreateDirectory(Folder);
            var used = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var ending in Videos())
            {
                string source = AssetDatabase.GetAssetPath(ending.videoClip);
                if (string.IsNullOrEmpty(source) || !File.Exists(source) || !source.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                    throw new BuildFailedException("Ending " + ending.id + ": assign an existing MP4 VideoClip before packaging.");
                string filename = string.IsNullOrWhiteSpace(ending.webVideoFileName) ? Path.GetFileName(source) : ending.webVideoFileName;
                if (!EndingData.IsValidWebVideoFileName(filename)) throw new BuildFailedException("Invalid Web Video Filename for " + ending.id);
                if (used.TryGetValue(filename, out var previous) && !EqualFiles(previous, source))
                    throw new BuildFailedException("Conflicting ending video filename: " + filename);
                used[filename] = source;
                string destination = Folder + "/" + filename;
                if (!File.Exists(destination) || !EqualFiles(source, destination)) File.Copy(source, destination, true);
                if (ending.webVideoFileName != filename)
                {
                    Undo.RecordObject(ending, "Configure Web ending video");
                    ending.webVideoFileName = filename;
                    EditorUtility.SetDirty(ending);
                }
                Debug.Log("[EndingVideoPackaging] " + ending.id + " -> " + destination);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate(Folder, true);
        }

        [MenuItem("Tools/First Jobber/Ending Videos/Validate Production Videos")]
        public static void ValidateProduction() => Validate(Folder, true);

        static void Validate(string folder, bool compareSource)
        {
            foreach (var ending in Videos())
            {
                if (!EndingData.IsValidWebVideoFileName(ending.webVideoFileName))
                    throw new BuildFailedException("Missing/invalid Web Video Filename: " + ending.id);
                var path = Path.Combine(folder, ending.webVideoFileName);
                // Exact case matters on itch.io even when the build is created on Windows.
                if (!Directory.Exists(folder) || !Directory.GetFiles(folder).Any(p => Path.GetFileName(p) == ending.webVideoFileName))
                    throw new BuildFailedException("Missing ending MP4 (case-sensitive): " + path);
                string expected = compareSource ? AssetDatabase.GetAssetPath(ending.videoClip) : Path.Combine(Folder, ending.webVideoFileName);
                if (!File.Exists(expected) || !EqualFiles(path, expected))
                    throw new BuildFailedException("Stale/mismatched packaged MP4 for " + ending.id + ". Run Package Production Videos.");
                Debug.Log("[EndingVideoPackaging] Verified " + ending.id + ": " + path + " (" + new FileInfo(path).Length + " bytes)");
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.WebGL) Validate(Folder, true);
        }
        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.WebGL)
                Validate(Path.Combine(report.summary.outputPath, "StreamingAssets/EndingVideos"), false);
        }
    }
}
