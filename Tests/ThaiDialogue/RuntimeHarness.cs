using System;using System.Collections;using System.Collections.Generic;using System.Reflection;using System.Text;using System.Text.RegularExpressions;using FirstJobber;using UnityEngine;using TMPro;
class ThaiDialogueTests {
 const string Thai="เช้าวันแรกของการเป็นเฟิร์สจ๊อบแจ๊บ!";
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Set(object o,string field,object value)=>o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
 static void Call(object o,string method)=>o.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);
 static void Step(DialogueManager m){if(m.Pending!=null)m.Pending.MoveNext();}
 static void Main(){
 var body=new TMP_Text{maxVisibleWords=1,maxVisibleLines=1,firstVisibleCharacter=10};var speaker=new TMP_Text();var view=new DialogueUI();var group=new CanvasGroup();Set(view,"bodyText",body);Set(view,"speakerText",speaker);Set(view,"group",group);Set(view,"logTextRevealDiagnostics",true);
 var manager=new DialogueManager();Set(manager,"ui",view);Set(manager,"minAdvanceDelay",0f);Call(manager,"OnEnable");
 var line=new DialogueLine(Speakers.Player,"English source!");line.locKey="event/event_day01_resume_intro/segments/resume_intro/lines/0/text";
 LocalizationManager.Thai=true;LocalizationManager.Translation=Thai;ThaiTextApplier.EnableNurse=true;
 int count=view.SetLine("ผู้คุมวิญญาณ",Thai,null,"test/page");Check(body.text==Thai&&body.Parsed.EndsWith("!")&&count>Thai.Length,"Real tokenizer must insert invisible word-break elements");Check(body.maxVisibleCharacters==0&&body.maxVisibleWords==int.MaxValue&&body.maxVisibleLines==int.MaxValue&&body.firstVisibleCharacter==0,"Initial text caps not reset");
 Check(speaker.maxVisibleCharacters==int.MaxValue&&speaker.maxVisibleWords==int.MaxValue,"Speaker accidentally capped");
 bool done=false;manager.Play(new[]{line},()=>done=true);Step(manager);Check(body.maxVisibleCharacters>0&&body.maxVisibleCharacters<count,"Progressive reveal was removed");
 // Change preprocessing after the initial cached count, like a late Nurse/font refresh.
 body.textPreprocessor=new ExtraBreaks(body.textPreprocessor);body.ForceMeshUpdate();int changed=body.textInfo.characterCount;Check(changed>count,"Late-processing regression not exercised");
 int frames=0;while(body.maxVisibleCharacters!=int.MaxValue&&frames++<500)Step(manager);
 Check(frames<500&&body.Rendered.EndsWith("!")&&body.Rendered==body.Parsed&&body.text==Thai&&!done,"Natural completion truncated the processed tail or advanced dialogue");
 body.textPreprocessor=new ExtraBreaks(body.textPreprocessor);body.ForceMeshUpdate();Check(body.Rendered==body.Parsed,"Completed page became truncated after late preprocessing");
 Call(view,"RaiseClicked");Step(manager);Check(done,"Continue no longer completes dialogue");
 done=false;manager.Play(new[]{line},()=>done=true);Step(manager);Call(view,"RaiseClicked");Step(manager);Check(body.maxVisibleCharacters==int.MaxValue&&body.Rendered.EndsWith("!")&&!done,"Skip must reveal entire page without advancing it");Call(view,"RaiseClicked");Step(manager);Check(done,"Second click must advance");
 ThaiTextApplier.EnableNurse=false;count=view.SetLine("Player",Thai,null);Check(count==Thai.Length&&body.text==Thai,"Controlled no-Nurse diagnostic changed source");
 LocalizationManager.Thai=false;manager.Play(new[]{new DialogueLine(Speakers.Player,"<b>Hello</b> 😀!")},()=>done=true);Step(manager);Check(body.textInfo.characterCount==8,"Rich markup/surrogate pair counted as visible characters");
 frames=0;while(body.maxVisibleCharacters!=int.MaxValue&&frames++<500)Step(manager);Check(body.Rendered=="Hello 😀!","English/rich text/emoji truncated");manager.Stop();
 Check(Debug.Messages.Exists(s=>s.Contains("completed=True"))&&Debug.Messages.Exists(s=>s.Contains("processedDiffers=True")),"Development diagnostics missing processing/completion state");
 Console.WriteLine("PASS real DialogueUI + DialogueManager and installed Thai tokenizer/C90 adjuster: exact Thai source preserved, complete !, progressive reveal, late processing, completed-page refresh, click-to-reveal/continue, no-Nurse control, English rich text/emoji and speaker caps (TMP API double; not native Play Mode).");
 }
}
class NurseProcessor:ITextPreprocessor {
 readonly PhEngine.ThaiTextCare.PhTokenizer tokenizer=new PhEngine.ThaiTextCare.PhTokenizer(new[]{"เช้า","วัน","แรก","ของ","การ","เป็น","เฟิร์ส","จ๊อบ","แจ๊บ"});
 public string PreprocessText(string text)=>PhEngine.ThaiTextCare.ThaiFontAdjuster.Adjust(string.Join("\u200B",tokenizer.Tokenize(text,true)),PhEngine.ThaiTextCare.ThaiGlyphCorrection.FullC90);
}
class ExtraBreaks:ITextPreprocessor {readonly ITextPreprocessor previous;public ExtraBreaks(ITextPreprocessor p){previous=p;}public string PreprocessText(string t)=>previous.PreprocessText(t).Replace("!","\u200B\u200B\u200B!");}
namespace UnityEngine {
 public class Object {public string name;}public class Sprite:Object{}public struct Color{public static Color white=>new Color();}
 public class GameObject{public bool activeSelf=true;public void SetActive(bool b){activeSelf=b;}}
 public class Coroutine{}public class MonoBehaviour:Object{public GameObject gameObject=new GameObject();public IEnumerator Pending;public Coroutine StartCoroutine(IEnumerator r){Pending=r;return new Coroutine();}public void StopCoroutine(Coroutine c){Pending=null;}}
 public class CanvasGroup:MonoBehaviour{public float alpha;public bool blocksRaycasts,interactable;}
 public class SerializeField:Attribute{}public class MinAttribute:Attribute{public MinAttribute(float f){}}public class TooltipAttribute:Attribute{public TooltipAttribute(string t){}}public class ContextMenu:Attribute{public ContextMenu(string t){}}public class HeaderAttribute:Attribute{public HeaderAttribute(string t){}}public class TextAreaAttribute:Attribute{public TextAreaAttribute(int a=0,int b=0){}}
 public class WaitForSecondsRealtime{public WaitForSecondsRealtime(float f){}}public static class Time{public static float unscaledDeltaTime=.02f;}public static class Mathf{public static int Min(int a,int b)=>Math.Min(a,b);public static int FloorToInt(float a)=>(int)Math.Floor(a);}public static class Random{public static int Range(int a,int b)=>a;}
 public static class Debug{public static List<string> Messages=new List<string>();public static void Log(string s,Object o){Messages.Add(s);}public static void LogWarning(string s,Object o){}}
}
namespace UnityEngine.UI{public class Image:MonoBehaviour{public Sprite sprite;}public class Button:MonoBehaviour{public Clicks onClick=new Clicks();}public class Clicks{public void AddListener(Action a){}public void RemoveListener(Action a){}}}
namespace UnityEngine.InputSystem{public class Keyboard{public static Keyboard current;public Key spaceKey,enterKey;}public class Key{public bool wasPressedThisFrame;}}
namespace DG.Tweening{public enum Ease{Normal}public class Tween{public void Kill(){}public Tween SetEase(Ease e)=>this;public Tween SetUpdate(bool b)=>this;}public static class Extensions{public static Tween DOFade(this CanvasGroup c,float f,float d){c.alpha=f;return new Tween();}}}
namespace TMPro {
 public interface ITextPreprocessor{string PreprocessText(string text);}public class TMP_TextInfo{public int characterCount;}
 public class TMP_Text:MonoBehaviour{public string text,Parsed;public int maxVisibleCharacters,maxVisibleWords,maxVisibleLines,firstVisibleCharacter;public TMP_TextInfo textInfo=new TMP_TextInfo();public ITextPreprocessor textPreprocessor;
 public void ForceMeshUpdate(bool ignoreActiveState=false,bool forceTextReparsing=false){Parsed=Regex.Replace(textPreprocessor!=null?textPreprocessor.PreprocessText(text??""):text??"","<[^>]+>","");int n=0;for(int i=0;i<Parsed.Length;i++,n++)if(char.IsHighSurrogate(Parsed[i])&&i+1<Parsed.Length&&char.IsLowSurrogate(Parsed[i+1]))i++;textInfo.characterCount=n;}
 public string Rendered{get{var b=new StringBuilder();int count=0;for(int i=0;i<Parsed.Length&&count<maxVisibleCharacters;i++,count++){b.Append(Parsed[i]);if(char.IsHighSurrogate(Parsed[i])&&i+1<Parsed.Length&&char.IsLowSurrogate(Parsed[i+1]))b.Append(Parsed[++i]);}return b.ToString();}}
 }
}
namespace FirstJobber {
 public static class ThaiTextApplier{public static bool EnableNurse;public static void Register(TMP_Text t){t.textPreprocessor=EnableNurse?new NurseProcessor():null;}}
 public static class LocalizationManager{public static bool Thai;public static string Translation;public static string Get(string key,string english)=>Thai&&key!=null&&key.StartsWith("event/")?Translation:english;}
 public static class LocalizationKeys{public static string SpeakerKey(string s)=>"speaker/"+s;}
 public class HRCharacterData:UnityEngine.Object{public string DisplayName=>"HR";public Sprite ResolveSprite(HREmotion e,object phase,out Color c){c=Color.white;return null;}}
 public class AtmosphereManager{public static AtmosphereManager Instance;public object AppliedPhase;}
 public class UIAnimationSettings{public bool animationsEnabled;public float dialogueFadeDuration;public DG.Tweening.Ease entranceEase,exitEase;}
}
namespace PhEngine.ThaiTextCare{public enum ThaiGlyphCorrection{None,YoorYingAndToorTaan,FullC90}}
