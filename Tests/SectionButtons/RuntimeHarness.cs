// Actual ChoiceListUI pooling, manual-region height reporting and ThaiTextApplier policy.
// Unity native layout/raycast/rendering are not simulated by these API doubles.
using System;
using System.Collections.Generic;
using System.Reflection;
using FirstJobber;
using UnityEngine;
using TMPro;
class SectionHarness
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Set(object o,string f,object v)=>o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
    static void Call(object o,string f)=>o.GetType().GetMethod(f,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
    static List<ChoiceButtonUI> Pool(object o)=>(List<ChoiceButtonUI>)o.GetType().GetField("pool",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    static void Main()
    {
        var resume=new ChoiceButtonUI{name="Resume"};var actions=new ChoiceButtonUI{name="Actions"};var list=new ChoiceListUI();
        Set(list,"itemPrefab",resume);Set(list,"content",new RectTransform());
        int created=ChoiceButtonUI.Created;
        var choices=new[]{new ChoiceItem{id="a"},new ChoiceItem{id="b"},new ChoiceItem{id="c"}};
        var confirmations=new[]{new ChoiceItem{id="confirm"},new ChoiceItem{id="back"}};
        list.SetItems(choices);var original=Pool(list);int clicked=0;list.ItemChosen+=id=>{Check(id=="a","Wrong callback");clicked++;};original[0].Click();Check(clicked==1,"Click callback changed");
        list.SetItemsUsingPrefab(confirmations,actions);
        Check(!original[0].gameObject.activeSelf && Pool(list)[0].Source==actions,"Ending role switch kept old buttons");
        for(int i=0;i<100;i++){list.SetItems(choices);list.SetItemsUsingPrefab(confirmations,actions);}
        Check(ChoiceButtonUI.Created==created+5,"Repeated choice/action switches grew the hierarchy");
        int errors=Debug.Errors;list.SetItemsUsingPrefab(confirmations,null);
        Check(Debug.Errors==errors+1 && !Pool(list)[0].gameObject.activeSelf && ChoiceButtonUI.Created==created+5,"Missing section prefab silently fell back");
        list.SetItems(choices);Check(Pool(list)==original,"Default role did not return to its original pool");
        Check(original[0].transform.anchoredPosition.x==13 && original[0].transform.sizeDelta.y==96,"Data binding overwrote authored geometry");
        Console.WriteLine("PASS actual list: independent prefab pools, 100 ending role switches without growth, callback binding, explicit missing-reference error, no geometry writes");

        var root=new RectTransform();var region=new RectTransform{parent=root,anchorMin=new Vector2(0,0),anchorMax=new Vector2(1,1),sizeDelta=new Vector2(0,-24)};
        var text=new TMP_Text{WantedHeight=120};text.rectTransform.parent=region;text.rectTransform.anchorMin=new Vector2(0,0);text.rectTransform.anchorMax=new Vector2(1,1);text.rectTransform.sizeDelta=new Vector2(0,0);text.rectTransform.anchoredPosition=new Vector2(22,31);
        var height=new ChoiceButtonContentHeight();height.transform=root;Set(height,"textRegions",new[]{text});height.CalculateLayoutInputVertical();
        Check(height.preferredHeight==144 && text.rectTransform.anchoredPosition.x==22 && text.rectTransform.anchoredPosition.y==31 && root.rect.height==96,"Responsive measurement moved internal objects instead of reporting height");
        text.rectTransform.anchorMax=new Vector2(1,0);height.CalculateLayoutInputVertical();Check(height.preferredHeight==-1,"Fixed manual regions must remain fixed");
        Console.WriteLine("PASS actual content-height reporter: wrapped text requirement, nested manual anchors, no RectTransform mutation, fixed-region ownership");

        var manager=new ThaiTextApplier();typeof(ThaiTextApplier).GetField("instance",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,manager);Call(manager,"OnEnable");
        var projectFont=new TMP_FontAsset{name="Project"};var fontA=new TMP_FontAsset{name="Artist A"};var fontB=new TMP_FontAsset{name="Artist B"};
        LocalizationManager.ThaiTable=new LocalizationTable{font=projectFont,correction=PhEngine.ThaiTextCare.ThaiGlyphCorrection.FullC90};
        var own=new TMP_Text{font=fontA,Owner=new ChoiceButtonUI{UsesProjectLocalizationFont=false}};
        var inactive=new TMP_Text{font=fontB,Owner=new ChoiceButtonUI{UsesProjectLocalizationFont=false}};inactive.gameObject.SetActive(false);
        var shared=new TMP_Text{font=fontB};var sceneButton=new TMP_Text{font=fontB,Policy=new UIFontPolicy()};
        ThaiTextApplier.Register(own,false);LocalizationManager.Switch(true);
        Check(own.font==fontA && inactive.font==fontB && sceneButton.font==fontB && shared.font==projectFont,"Global font scan overwrote independently authored fonts");
        Check(own.GetComponent<PhEngine.ThaiTextCare.ThaiTextNurse>().Correction==PhEngine.ThaiTextCare.ThaiGlyphCorrection.None,"Private font inherited incompatible project-only glyph substitutions");
        own.Policy=new UIFontPolicy{useProjectFont=true};Call(manager,"ApplyAll");Check(own.font==projectFont,"Explicit project-font opt-in ignored");
        own.Policy.useProjectFont=false;Call(manager,"ApplyAll");Check(own.font==fontA,"Returning to authored font lost its original assignment");
        LocalizationManager.Switch(false);Check(own.font==fontA && sceneButton.font==fontB,"Language switch overwrote authored font");
        Console.WriteLine("PASS actual font policy: independent section fonts, inactive preview sections, scene buttons, opt-in/restore, EN/TH changes and safe custom-font correction");
    }
}
namespace UnityEngine
{
    public class Object{public string name;}
    public class GameObject:Object
    {
        public bool activeSelf=true;public bool activeInHierarchy=>activeSelf;readonly Dictionary<Type,object> components=new Dictionary<Type,object>();
        public GameObject(string name=""){this.name=name;}public void SetActive(bool v){activeSelf=v;}
        public T GetComponent<T>()where T:class=>components.TryGetValue(typeof(T),out var v)?v as T:null;
        public T AddComponent<T>()where T:class,new(){var v=new T();components[typeof(T)]=v;return v;}
    }
    public class MonoBehaviour:Object
    {
        public GameObject gameObject=new GameObject();
        public RectTransform transform=new RectTransform();
        protected ChoiceButtonUI Instantiate(ChoiceButtonUI prefab,RectTransform parent)=>new ChoiceButtonUI{Source=prefab};
        protected static void DontDestroyOnLoad(GameObject go){}
        protected static T[] FindObjectsByType<T>(FindObjectsInactive i,FindObjectsSortMode s)where T:class=>TMP_Text.All.ToArray() as T[];
    }
    public enum FindObjectsInactive{Include}public enum FindObjectsSortMode{None}
    public class RectTransform:Object{public RectTransform parent;public Vector2 anchorMin,anchorMax=new Vector2(1,1),sizeDelta=new Vector2(800,96),anchoredPosition=new Vector2(13,17);public Rect rect=new Rect{width=300,height=96};public void SetSiblingIndex(int i){}public void SetAsLastSibling(){}}
    public struct Rect{public float width,height;}
    public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
    public class CanvasGroup{public bool interactable,blocksRaycasts;public float alpha;}
    public static class Canvas{public static void ForceUpdateCanvases(){}}
    public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);}
    public static class Time{public static float unscaledTime;}
    public static class Debug{public static int Errors;public static void LogError(string s,Object context){Errors++;}}
    public class SerializeField:Attribute{}public class TooltipAttribute:Attribute{public TooltipAttribute(string s){}}public class DisallowMultipleComponent:Attribute{}public class RequireComponent:Attribute{public RequireComponent(Type t){}}
    public enum RuntimeInitializeLoadType{BeforeSceneLoad}public class RuntimeInitializeOnLoadMethodAttribute:Attribute{public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){}}
}
namespace UnityEngine.UI
{
    public interface ILayoutElement{void CalculateLayoutInputHorizontal();void CalculateLayoutInputVertical();float minWidth{get;}float maxWidth{get;}float preferredWidth{get;}float flexibleWidth{get;}float minHeight{get;}float maxHeight{get;}float preferredHeight{get;}float flexibleHeight{get;}int layoutPriority{get;}}
    public class ScrollRect{public float verticalNormalizedPosition;}
    public static class LayoutUtility{public const float DefaultMaxSize=float.MaxValue;}
    public static class LayoutRebuilder{public static void MarkLayoutForRebuild(RectTransform t){}}
}
namespace TMPro
{
    public class TMP_FontAsset:UnityEngine.Object{}
    public class TMP_InputField{}
    public class TMP_Text:UnityEngine.Object
    {
        public static List<TMP_Text> All=new List<TMP_Text>();public GameObject gameObject=new GameObject();public RectTransform rectTransform=new RectTransform();public string text="Text";public float WantedHeight;public TMP_FontAsset font;public ChoiceButtonUI Owner;public UIFontPolicy Policy;
        public TMP_Text(){All.Add(this);}public Vector2 GetPreferredValues(string s,float width,float height)=>new Vector2(width,WantedHeight);
        public T GetComponentInParent<T>(bool include=false)where T:class=>typeof(T)==typeof(ChoiceButtonUI)?Owner as T:Policy as T;
        public T GetComponent<T>()where T:class=>gameObject.GetComponent<T>();public void SetAllDirty(){}
    }
}
namespace PhEngine.ThaiTextCare
{
    public enum ThaiGlyphCorrection{None,FullC90}
    public class ThaiTextNurse{public bool enabled=true,IsTokenize;public ThaiGlyphCorrection Correction;public void NotifyChange(){}}
}
namespace FirstJobber
{
    public struct ChoiceItem{public string id;}
    public class ChoiceButtonUI:UnityEngine.Object{public static int Created;public ChoiceButtonUI Source;public bool UsesProjectLocalizationFont=true;public RectTransform transform=new RectTransform();public GameObject gameObject=new GameObject();Action<string> callback;string id;public ChoiceButtonUI(){Created++;}public void Setup(ChoiceItem item,Action<string> click){id=item.id;callback=click;}public void Click(){callback?.Invoke(id);}}
    public class LocalizationTable{public TMP_FontAsset font;public PhEngine.ThaiTextCare.ThaiGlyphCorrection correction;}
    public static class LocalizationManager{public static LocalizationTable ThaiTable;public static bool Thai;public static LocalizationTable Table=>Thai?ThaiTable:null;public static event Action LanguageChanged;public static void Switch(bool thai){Thai=thai;LanguageChanged?.Invoke();}}
}
