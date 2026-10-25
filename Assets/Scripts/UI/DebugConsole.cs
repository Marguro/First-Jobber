using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstJobber
{
    /// <summary>
    /// In-game debug console (Editor and Development Builds only). Press ` (backquote) or F1 to toggle.
    /// Lists every ending as a button, plus a command line: "ending 2", "ending ending_my_own_terms", "endings".
    /// Creates itself at startup; no scene setup needed.
    /// </summary>
    public class DebugConsole : MonoBehaviour
    {
        GameFlowManager flow;
        bool open;
        string command = "";
        string message = "";
        Vector2 scroll;
        Rect window = new Rect(20, 20, 420, 460);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (!Debug.isDebugBuild) return;
            if (FindFirstObjectByType<DebugConsole>() != null) return;
            var go = new GameObject("DebugConsole");
            go.AddComponent<DebugConsole>();
            DontDestroyOnLoad(go);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.backquoteKey.wasPressedThisFrame || kb.f1Key.wasPressedThisFrame)) open = !open;
        }

        void OnGUI()
        {
            if (!open) return;
            if (flow == null) flow = FindFirstObjectByType<GameFlowManager>();
            window = GUILayout.Window(94021, window, Draw, "Debug Console  (` or F1 to close)");
        }

        void Draw(int id)
        {
            if (flow == null || flow.Database == null)
            {
                GUILayout.Label("No GameFlowManager / database found in the scene.");
                GUI.DragWindow();
                return;
            }

            GUILayout.Label("Phase: " + flow.Phase);
            GUILayout.Space(4);
            GUILayout.Label("Trigger ending:");
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(240));
            var endings = flow.Database.endings;
            for (int i = 0; i < endings.Count; i++)
            {
                var e = endings[i];
                if (e == null) continue;
                string timeline = (e.timeline != null || e.cutscenePrefab != null) && e.timelineMode != EndingTimelineMode.None ? "  [timeline]" : "";
                if (GUILayout.Button((i + 1) + ". " + e.title + "  (" + e.id + ")" + timeline, GUILayout.Height(30)))
                    Run("ending " + e.id);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(6);
            GUILayout.Label("Command:");
            GUI.SetNextControlName("cmd");
            command = GUILayout.TextField(command);
            var ev = Event.current;
            bool enter = ev.type == EventType.KeyDown && (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter)
                         && GUI.GetNameOfFocusedControl() == "cmd";
            if (GUILayout.Button("Run") || enter)
            {
                Run(command);
                command = "";
                if (enter) ev.Use();
            }
            if (message.Length > 0) GUILayout.Label(message);
            GUI.DragWindow(new Rect(0, 0, 10000, 22));
        }

        void Run(string line)
        {
            var parts = line.Trim().Split(new[] { ' ' }, 2);
            string verb = parts[0].ToLowerInvariant();
            string arg = parts.Length > 1 ? parts[1].Trim() : "";
            var endings = flow.Database.endings;

            if (verb == "endings")
            {
                var sb = new StringBuilder();
                for (int i = 0; i < endings.Count; i++) if (endings[i] != null) sb.Append(i + 1).Append(". ").Append(endings[i].id).Append('\n');
                message = sb.ToString();
                return;
            }
            if (verb == "ending")
            {
                string endingId = arg;
                if (int.TryParse(arg, out int n) && n >= 1 && n <= endings.Count && endings[n - 1] != null) endingId = endings[n - 1].id;
                message = flow.DebugTriggerEnding(endingId) ? "Triggered '" + endingId + "'." : "Unknown ending '" + arg + "'. Type 'endings' to list.";
                if (message.StartsWith("Triggered")) open = false;
                return;
            }
            message = "Commands: ending <number|id>, endings";
        }
    }
}
