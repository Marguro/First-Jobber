// Standalone regression harness. Unity APIs and unrelated content types are test doubles;
// the confidence resolver, GameDatabase and DialogueManager are compiled from the real sources.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FirstJobber;
using UnityEngine;

class RuntimeHarness
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }

    static void Drain(DialogueManager player)
    {
        for (int i = 0; i < 1000; i++)
        {
            var running = player.Pending;
            if (running == null) return;
            if (!running.MoveNext() && player.Pending == running) player.Pending = null;
        }
        throw new Exception("Dialogue did not complete within 1000 simulated frames.");
    }

    static void Main()
    {
        var config = ProductionFixture.Create(); // generated from the actual serialized Production asset
        var db = new GameDatabase { confidenceDialogue = config, requireDayBasedConfidence = true };
        // Startup stamps keys before resolving Apply dialogue, including incomplete Inspector data.
        LocalizationKeys.Visit(db, null);
        Check(config.days[0].high[0].dialogue.locKey == "confidence/apply/day_01/high/candidate_00/text",
            "Production candidate key changed");
        var incomplete = new ConfidenceDialogueDatabase { days = null };
        incomplete.fallbackHigh.Add(new ConfidenceCandidate { id = "fallback", dialogue = new DialogueLine(Speakers.Player, "Fallback") });
        db.confidenceDialogue = incomplete;
        var exported = new Dictionary<string, string>();
        LocalizationKeys.Visit(db, (key, text) => exported[key] = text);
        Check(exported["confidence/apply/fallback/high/fallback/text"] == "Fallback",
            "Null days prevented fallback localization");
        incomplete.days = new List<ConfidenceDayConfig> { null, new ConfidenceDayConfig { day = 1, high = null, low = null } };
        LocalizationKeys.Visit(db, null);
        db.confidenceDialogue = config;
        Console.WriteLine("PASS startup localization: Production keys, null days, fallback export, null day/pools");
        var player = new DialogueManager();
        var ui = new DialogueUI();
        typeof(DialogueManager).GetField("ui", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(player, ui);
        var samples = new[] { (1, 80f), (1, 30f), (2, 80f), (2, 30f), (3, 80f),
            (3, 50f), (4, 80f), (4, 30f), (5, 80f), (5, 30f) };
        foreach (var sample in samples)
        {
            var day = config.days.Find(d => d.day == sample.Item1);
            var expected = sample.Item2 > 50 ? day.high : day.low;
            var pool = db.ResolveApplyReaction(sample.Item1, sample.Item2, out var mode);
            Check(mode == DialoguePlaybackMode.RandomOne && pool.Length == expected.Count, "Wrong pool or mode");
            for (int i = 0; i < pool.Length; i++) Check(pool[i] == expected[i].dialogue, "Wrong day/branch");
            for (int repeat = 0; repeat < 100; repeat++)
            {
                int shown = 0, completed = 0;
                DialogueLine picked = null;
                Action<DialogueLine> observe = line => { shown++; picked = line; };
                player.LineShown += observe;
                player.Play(pool, () => completed++, 1f, mode);
                Drain(player);
                player.LineShown -= observe;
                Check(shown == 1 && completed == 1 && Array.IndexOf(pool, picked) >= 0,
                    "RandomOne must play exactly one candidate and finish once");
            }
            Console.WriteLine("PASS Day " + sample.Item1 + " / " + sample.Item2 + "% (100 runs)");
        }
        for (int day = 1; day <= 5; day++)
            Check(db.ResolveApplyReaction(day, 50f, out _)[0] == config.days[day - 1].low[0].dialogue,
                "Exactly 50 must be Low on every day");

        // One randomly selected block retains every page, and the next sequence starts only on completion.
        var multipage = new DialogueLine { dialogueLines = new List<string> { "page 1", "page 2", "page 3" } };
        int before = ui.Pages.Count;
        bool done = false;
        player.Play(new[] { multipage }, () => {
            Check(ui.Pages.Count == before + 3, "HR started before confidence pages completed");
            player.Play(new[] { new DialogueLine(Speakers.HR, "HR introduction") }, () => done = true, 1f);
        }, 1f, DialoguePlaybackMode.RandomOne);
        Drain(player);
        Check(done && ui.Pages.Count == before + 4, "Confidence -> HR chain did not finish");
        Check(ui.Pages[before] == "page 1" && ui.Pages[before + 1] == "page 2" && ui.Pages[before + 2] == "page 3",
            "Selected candidate pages must stay in order");

        // Manual advancement must wait instead of firing the completion callback early.
        done = false;
        player.Play(new[] { new DialogueLine(Speakers.Player, "manual") }, () => done = true);
        Check(player.Pending.MoveNext() && !done, "Manual dialogue completed before a click");
        typeof(DialogueManager).GetMethod("OnClicked", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player, null);
        Drain(player);
        Check(done, "Manual click did not complete dialogue");

        // Null/empty configuration must finish without playing legacy content or changing confidence branch.
        db.confidenceDialogue = new ConfidenceDialogueDatabase { days = null };
        var empty = db.ResolveApplyReaction(5, 80f, out var emptyMode);
        Check(empty == null && emptyMode == DialoguePlaybackMode.RandomOne, "Empty Production config used legacy content");
        done = false;
        player.Play(empty, () => done = true, mode: emptyMode);
        Check(done && !player.IsPlaying, "Empty pool stalled Apply");
        db.confidenceDialogue.fallbackHigh.Add(new ConfidenceCandidate { dialogue = multipage });
        Check(db.ResolveApplyReaction(99, 80f, out _)[0] == multipage, "Authored fallback not used");
        Check(db.ResolveApplyReaction(99, 50f, out _) == null, "Low incorrectly used High fallback");
        db.confidenceDialogue = config;
        Check(db.ResolveApplyReaction(99, 80f, out _)[0] == config.days[4].high[0].dialogue, "Missing day fallback failed");
        config.days[2].high.Clear();
        Check(db.ResolveApplyReaction(3, 80f, out _)[0] == config.days[1].high[0].dialogue,
            "Empty daily High pool must use an authored High fallback");
        Check(db.ResolveApplyReaction(3, 50f, out _)[0] == config.days[2].low[0].dialogue,
            "Empty High pool changed the daily Low pool");
        db.confidenceDialogue = null;
        Check(db.ResolveApplyReaction(1, 80f, out var missingMode) == null && missingMode == DialoguePlaybackMode.RandomOne,
            "Missing/broken Production reference fell back to legacy dialogue");
        done = false;
        player.Play(db.ResolveApplyReaction(1, 50f, out missingMode), () => done = true, mode: missingMode);
        Check(done, "Missing Production reference stalled Apply");
        db.requireDayBasedConfidence = false; // existing demo databases opt into legacy behavior by default
        Check(db.ResolveApplyReaction(1, 80f, out _) == db.highConfidenceConfirmedLines, "Demo legacy behavior changed");
        Console.WriteLine("PASS 50% boundary, multipage order, callback chain, manual advance, empty/null/missing-day fallbacks, Demo compatibility");
    }
}

namespace UnityEngine
{
    public class Object { public string name; }
    public class ScriptableObject : Object { }
    public class Sprite : Object { }
    public struct Color { public static Color white => new Color(); }
    public class Coroutine { }
    public class MonoBehaviour : Object
    {
        public IEnumerator Pending;
        public Coroutine StartCoroutine(IEnumerator r) { Pending = r; return new Coroutine(); }
        public void StopCoroutine(Coroutine r) { Pending = null; }
    }
    public class SerializeField : Attribute { }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string s) { } }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string s) { } }
    public class TextAreaAttribute : Attribute { public TextAreaAttribute(int a = 0, int b = 0) { } }
    public class MinAttribute : Attribute { public MinAttribute(float n) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    public class HideInInspector : Attribute { }
    public class CreateAssetMenuAttribute : Attribute { public string menuName, fileName; }
    public class WaitForSecondsRealtime { public WaitForSecondsRealtime(float n) { } }
    public static class Time { public static float unscaledDeltaTime = 1f; }
    public static class Mathf
    {
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int FloorToInt(float a) => (int)Math.Floor(a);
    }
    public static class Random
    {
        static System.Random rng = new System.Random(12345);
        public static int Range(int a, int b) => rng.Next(a, b);
    }
    public static class Debug { public static void LogWarning(string s, Object context = null) { } }
}

namespace FirstJobber
{
    public class DialogueUI
    {
        public event Action Clicked;
        public List<string> Pages = new List<string>();
        public void Hide() { }
        public void Show() { }
        public int SetLine(string speaker, string text, Sprite portrait) { Pages.Add(text); return 0; }
        public void SetContinueIndicator(bool value) { }
        public void SetVisibleCharacters(int n) { }
    }
    public static class LocalizationManager { public static string Get(string key, string english) => english; }
    public class ResumeCategoryData : ScriptableObject { public string id, displayName, emptyMessage; }
    public class ResumeChoiceData : ScriptableObject
    { public string id, displayText, description; public ResumeCategoryData category; public DialogueLine[] playerReaction, hrReactions; }
    public class CompanyData : ScriptableObject { public string id, companyName, jobTitle, salary, industry, description; public HRCharacterData hr; public DialogueLine[] playerReaction, hrIntro, rejection; }
    public class HRChoiceReaction { public ResumeChoiceData choice; public DialogueLine[] lines; }
    public class HRCharacterData : ScriptableObject
    {
        public string id, hrName;
        public DialogueLine[] introduction, resumeReview, rejection;
        public List<HRChoiceReaction> choiceReactions;
        public List<InterviewQuestionData> fixedQuestions = new List<InterviewQuestionData>(), resumeLinkedQuestions = new List<InterviewQuestionData>();
        public InterviewQuestionData fallbackQuestion;
        public string DisplayName => hrName;
        public Sprite ResolveSprite(HREmotion emotion, AtmospherePhaseData phase, out Color tint) { tint = Color.white; return null; }
        public void Validate(List<string> p) { }
    }
    public class InterviewQuestionData : ScriptableObject
    { public string id, questionText; public DialogueLine[] questionDialogue; public List<InterviewAnswer> answers; public ResumeChoiceData linkedChoice; public ResumeCategoryData linkedCategory; }
    public class AtmospherePhaseData { }
    public class AtmosphereManager { public static AtmosphereManager Instance; public AtmospherePhaseData AppliedPhase; }
    public class InterviewAnswer { public string id, text; public DialogueLine[] playerReaction, hrResponse; }
    public class DailyEventOption { public string id, label; }
    public class DailyEventSegment { public string id; public DialogueLine[] lines; public DailyEventOption[] options; }
    public class DailyEventData : ScriptableObject { public string id, caption; public DialogueLine[] lines; public DailyEventSegment[] segments; }
    public class EndingData : ScriptableObject { public string id, optionLabel, title, optionDescription, finalText; public DialogueLine[] lines, previewLines; }
}
