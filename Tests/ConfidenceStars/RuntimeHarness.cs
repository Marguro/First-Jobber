// Runs real settings, choice view, ResumeChoiceData and calculator code with minimal Unity UI test doubles.
using System;
using System.Collections.Generic;
using System.Reflection;
using FirstJobber;
using UnityEngine;
using UnityEngine.UI;

class ConfidenceStarHarness
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Field(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);
    static void Main()
    {
        var settings = new ConfidenceStarSettings {
            starLevel1 = new Sprite { name = "1" }, starLevel2 = new Sprite { name = "2" },
            starLevel3 = new Sprite { name = "3" }, starLevel4 = new Sprite { name = "4" }, starLevel5 = new Sprite { name = "5" }
        };
        float[] values = { 0, 20, 20.1f, 40, 40.1f, 50, 60, 60.1f, 80, 80.1f, 100 };
        string[] levels = { "5", "5", "4", "4", "3", "3", "3", "2", "2", "1", "1" };
        for (int i = 0; i < values.Length; i++) Check(settings.GetSprite(values[i]).name == levels[i], "Incorrect boundary: " + values[i]);
        Check(settings.GetSprite(float.NaN) == settings.starLevel5 && settings.GetSprite(120) == settings.starLevel1, "Confidence clamp changed");
        settings.level5Max = 10;
        Check(settings.GetSprite(20) == settings.starLevel4, "Inspector threshold is ignored");
        settings.level5Max = 20;

        var view = new ChoiceButtonUI();
        var image = new Image();
        var button = new Button();
        var title = new TMPro.TMP_Text();
        var badge = new TMPro.TMP_Text();
        title.fontStyle = (TMPro.FontStyles)4; // an authored style flag unrelated to depletion
        Field(view, "confidenceStar", image); Field(view, "confidenceStarSettings", settings);
        Field(view, "button", button); Field(view, "titleText", title);
        Field(view, "badgeText", badge);
        typeof(ChoiceButtonUI).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, null);
        int created = GameObject.Created;
        foreach (float value in values)
        {
            var item = new ChoiceItem { id = "choice", title = "A very long original choice label remains intact", showConfidenceStar = true, confidence = value, state = ChoiceState.Available };
            int clicks = 0;
            view.Setup(item, id => { Check(id == "choice", "Wrong choice click"); clicks++; });
            var sprite = image.sprite;
            Check(sprite == settings.GetSprite(value) && image.gameObject.activeSelf && button.interactable, "Unselected choice has no correct star");
            Check(!image.raycastTarget && image.preserveAspect && image.color.Equals(Color.white), "Star alpha/aspect/raycast configuration wrong");
            button.onClick.Invoke(); Check(clicks == 1, "Star blocked click or duplicate listeners");
            item.state = ChoiceState.Selected;
            view.Setup(item, null); Check(image.sprite == sprite, "Selection changed the authored star");
            Check((title.fontStyle & (TMPro.FontStyles)4) != 0, "Selection overwrote the artist's title style");
            item.state = ChoiceState.Consumed;
            view.Setup(item, null); Check(image.sprite == sprite && !button.interactable, "Depletion behavior changed");
            Check(title.text == item.title, "Choice label was replaced");
            view.Setup(new ChoiceItem { id = "company", state = ChoiceState.Available }, null);
            Check(image.sprite == null && !image.gameObject.activeSelf, "Pooled non-resume item retained a star");
        }
        Field(view, "showBadge", false);
        view.Setup(new ChoiceItem {badge="95%",state=ChoiceState.Available},null);
        Check(!badge.gameObject.activeSelf,"Inspector badge visibility was ignored");
        Field(view, "showBadge", true);
        view.Setup(new ChoiceItem {badge="95%",state=ChoiceState.Available},null);
        Check(badge.gameObject.activeSelf && badge.text=="95%","Badge data was not restored");
        view.Setup(new ChoiceItem {state=ChoiceState.Available},null);
        Check(!badge.gameObject.activeSelf,"An empty badge must not reserve layout space");
        for (int day = 1; day <= 5; day++)
            foreach (var choice in AuthoredChoices.Create())
            {
                view.Setup(new ChoiceItem { id = choice.id, showConfidenceStar = true, confidence = choice.Confidence }, null);
                Check(image.sprite == settings.GetSprite(choice.Confidence), "Authored Production value not used after refresh");
            }
        Check(GameObject.Created == created, "Refresh allocated extra star objects");
        Check(ResumeConfidenceCalculator.Average(new[] {100f, 80f, 30f}) == 70f, "Overall average changed");
        var choices = new[] { new ResumeChoiceData { confidencePercentage = 100 }, new ResumeChoiceData { confidencePercentage = 80 }, null };
        Check(ResumeConfidenceCalculator.Calculate(choices, out var count) == 90 && count == 2, "Selected mean/null handling changed");
        Console.WriteLine("PASS A-D, fractional boundaries, editable thresholds, selection/depletion, pooling/rebinding, click dispatch, white alpha/aspect/raycast, authored Production values and Overall Confidence");
        Console.WriteLine("Native Unity layout, category navigation and Play Mode not simulated by these test doubles.");
    }
}

namespace UnityEngine
{
    public class Object { public string name; }
    public class ScriptableObject : Object { }
    public class Sprite : Object { }
    public class GameObject { public static int Created; public bool activeSelf; public GameObject() { Created++; } public void SetActive(bool active) { activeSelf = active; } }
    public class MonoBehaviour : Object { public T GetComponent<T>() where T : class => null; }
    public struct Color
    {
        public float r,g,b,a;
        public Color(float r, float g, float b, float a = 1) { this.r=r; this.g=g; this.b=b; this.a=a; }
        public static Color white => new Color(1,1,1,1);
    }
    public static class Mathf { public static float Max(float a, float b) => Math.Max(a,b); }
    public class SerializeField : Attribute { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string text) { } }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(float a,float b) { } }
    public class TextAreaAttribute : Attribute { public TextAreaAttribute(int a=0,int b=0) { } }
    public class CreateAssetMenuAttribute : Attribute { public string menuName,fileName; }
}
namespace UnityEngine.UI
{
    public class Image { public Sprite sprite; public bool preserveAspect,raycastTarget; public Color color; public GameObject gameObject = new GameObject(); }
    public class Button
    {
        public bool interactable;
        public Event onClick = new Event();
        public class Event { Action actions; public void AddListener(Action action) { actions += action; } public void RemoveListener(Action action) { actions -= action; } public void Invoke() { actions?.Invoke(); } }
    }
}
namespace TMPro
{
    public enum FontStyles { Normal, Strikethrough }
    public class TMP_Text { public string text; public FontStyles fontStyle; public GameObject gameObject = new GameObject(); }
}
namespace FirstJobber
{
    public class ResumeCategoryData : ScriptableObject { }
    public static class ThaiTextApplier { public static void Register(TMPro.TMP_Text text, bool useProjectFont = true) { } }
}
