using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FirstJobber;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace UnityEngine {
 public class Object {public string name="test";}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();public T[] GetComponentsInChildren<T>(bool includeInactive)=>new T[0];}
 public class MonoBehaviour:Component {public bool enabled=true;public bool isActiveAndEnabled=>enabled&&gameObject.activeSelf;public Coroutine StartCoroutine(IEnumerator x)=>new Coroutine();}
 public class Coroutine {} public class ScriptableObject:Object {}
 public class GameObject:Object {
  public bool activeSelf=true;public bool activeInHierarchy=>activeSelf;public RectTransform transform;readonly List<object> parts=new List<object>();
  public GameObject(){transform=new RectTransform();transform.gameObject=this;parts.Add(transform);}
  public T Add<T>(T c)where T:Component {c.gameObject=this;parts.Add(c);return c;}
  public T GetComponent<T>()where T:class {foreach(var p in parts)if(p is T)return p as T;return null;}
  public void SetActive(bool v){activeSelf=v;}
 }
 public class Transform:Component {public Transform parent;public Vector3 localScale=Vector3.one;}
 public class RectTransform:Transform {public Vector2 anchoredPosition;}
 public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 one=>new Vector2(1,1);public static Vector2 Scale(Vector2 a,Vector2 b)=>new Vector2(a.x*b.x,a.y*b.y);public static Vector2 operator+(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);public static Vector2 operator-(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);public static Vector2 operator*(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);}
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 one=>new Vector3(1,1,1);public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);}
 public class CanvasGroup:MonoBehaviour {public float alpha=1;public bool interactable=true,blocksRaycasts=true;}
 public class AudioClip:Object {} public class Sprite:Object {}
 public class AudioSource:MonoBehaviour {public float volume;public Audio.AudioMixerGroup outputAudioMixerGroup;public int shots;public AudioClip last;public void PlayOneShot(AudioClip c,float v){shots++;last=c;}}
 public static class Time {public static int frameCount=1;public static float unscaledDeltaTime=.1f,unscaledTime;}
 public static class Mathf {public static float Clamp01(float v)=>Math.Max(0,Math.Min(1,v));public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static float MoveTowards(float a,float b,float c)=>Math.Abs(b-a)<=c?b:a+Math.Sign(b-a)*c;public static bool Approximately(float a,float b)=>Math.Abs(a-b)<.00001f;}
 public static class RectTransformUtility {public static bool RectangleContainsScreenPoint(RectTransform r,Vector2 p,object cam)=>p.x>=0;}
 public static class Debug {public static void LogWarning(string s,Object o=null){} }
 public class WaitForSecondsRealtime {public WaitForSecondsRealtime(float f){}}
 public class SerializeField:Attribute{}public class HeaderAttribute:Attribute{public HeaderAttribute(string s){}} public class TooltipAttribute:Attribute{public TooltipAttribute(string s){}}public class RangeAttribute:Attribute{public RangeAttribute(float a,float b){}}public class MinAttribute:Attribute{public MinAttribute(float a){}}public class CreateAssetMenuAttribute:Attribute{public string menuName;}public class DisallowMultipleComponent:Attribute{}public class RequireComponent:Attribute{public RequireComponent(Type t){}}public class RuntimeInitializeOnLoadMethodAttribute:Attribute{public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){}}public enum RuntimeInitializeLoadType {SubsystemRegistration}
}
namespace UnityEngine.Audio {public class AudioMixerGroup {}}
namespace UnityEngine.UI {
 public class LayoutGroup:MonoBehaviour {}
 public class Image:MonoBehaviour {public Sprite sprite;}
 public class Button:MonoBehaviour {
  public bool interactable=true; public ActionList onClick=new ActionList();
  public bool IsInteractable(){if(!interactable)return false;for(Transform t=transform;t!=null;t=t.parent){var c=t.GetComponent<CanvasGroup>();if(c!=null&&!c.interactable)return false;}return true;}
 }
 public class ActionList {readonly List<Action> listeners=new List<Action>();public void AddListener(Action a)=>listeners.Add(a);public void RemoveListener(Action a)=>listeners.Remove(a);public void Invoke(){foreach(var a in listeners.ToArray())a();}}
}
namespace UnityEngine.EventSystems {
 public class BaseEventData {}public class PointerEventData:BaseEventData {public enum InputButton{Left,Right}public InputButton button;public Vector2 position=new Vector2(1,1);public object pressEventCamera;}
 public interface IPointerEnterHandler{void OnPointerEnter(PointerEventData d);}public interface IPointerExitHandler{void OnPointerExit(PointerEventData d);}public interface IPointerDownHandler{void OnPointerDown(PointerEventData d);}public interface IPointerUpHandler{void OnPointerUp(PointerEventData d);}public interface ISubmitHandler{void OnSubmit(BaseEventData d);}
}
namespace TMPro {public class TMP_Text:MonoBehaviour{public string text;public int maxVisibleCharacters;public TMP_Info textInfo=new TMP_Info();public void ForceMeshUpdate(){textInfo.characterCount=text==null?0:text.Length;}}public class TMP_Info {public int characterCount;}}
namespace UnityEngine.InputSystem {public class Keyboard {public static Keyboard current;public Key spaceKey=new Key(),enterKey=new Key();}public class Key {public bool wasPressedThisFrame;}}
namespace DG.Tweening {
 public enum Ease {OutCubic,InQuad,OutQuad}
 public class Tween {public bool active=true,nested;public float delay;public Action end,completion;public virtual void Kill(){active=false;}public virtual void Complete(){if(!active)return;end?.Invoke();active=false;completion?.Invoke();}}
 public class Sequence:Tween {readonly List<Tween> children=new List<Tween>();public Sequence Append(Tween t){children.Add(t);t.nested=true;return this;}public Sequence Join(Tween t)=>Append(t);public override void Kill(){base.Kill();foreach(var t in children)t.Kill();}public override void Complete(){if(!active)return;foreach(var t in children)t.Complete();base.Complete();}}
 public static class DOTween {public static List<Tween> all=new List<Tween>();public static Tween New(Action end){var t=new Tween{end=end};all.Add(t);return t;}public static Sequence Sequence(){var t=new Sequence();all.Add(t);return t;}public static void CompleteAll(){foreach(var t in all.ToArray())if(!t.nested)t.Complete();all.RemoveAll(t=>!t.active);}}
 public static class Extensions {
  public static T SetDelay<T>(this T t,float d)where T:Tween{t.delay=d;return t;}public static T SetEase<T>(this T t,Ease e)where T:Tween=>t;public static T SetUpdate<T>(this T t,bool b)where T:Tween=>t;public static T OnComplete<T>(this T t,Action a)where T:Tween{t.completion=a;return t;}
  public static Tween DOScale(this Transform r,Vector3 v,float d)=>DOTween.New(()=>r.localScale=v);public static Tween DOAnchorPos(this RectTransform r,Vector2 v,float d)=>DOTween.New(()=>r.anchoredPosition=v);public static Tween DOFade(this CanvasGroup r,float v,float d)=>DOTween.New(()=>r.alpha=v);
 }
 public class DOTweenAnimation:MonoBehaviour {public enum AnimationType{Scale}public AnimationType animationType;public bool isFrom,isRelative,optionalBool0;public Vector3 endValueV3;public Tween tween;public float delay;public void RewindThenRecreateTweenAndPlay(){tween=DOTween.New(()=>{});}}
}
namespace FirstJobber {
 public enum GamePhase {MainMenu,CreateApplicant,ResumeEditor,CompanySelection,InterviewIntroduction,InterviewResumeReview,InterviewQuestion,InterviewReaction,Rejection,Sleep,DayTransition,DailyEvent,FinalDecision,Ending}
 public class AtmospherePhaseData:ScriptableObject {}public class AtmosphereManager {public static AtmosphereManager Instance;public AtmospherePhaseData AppliedPhase;}
 public class ChoiceListUI:MonoBehaviour {public void PreparePresentation(float delay){}}
 public class InterviewDialogueLayout:MonoBehaviour {public void SetInterviewActive(bool x){}}
 public class ResumeEditorController:MonoBehaviour {public event Action Confirmed;public void Raise()=>Confirmed?.Invoke();}
 public class CompanySelectionController:MonoBehaviour {public event Action<string> CompanyConfirmed;public void Raise()=>CompanyConfirmed?.Invoke("company");}
 public class InterviewController:MonoBehaviour {public event Action Finished;public void Raise()=>Finished?.Invoke();}
}
class Tests {
 static void Set(object o,string k,object v)=>o.GetType().GetField(k,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
 static void Call(object o,string k)=>o.GetType().GetMethod(k,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
 static T Make<T>()where T:Component,new()=>new GameObject().Add(new T());
 static void Check(bool b,string s){if(!b)throw new Exception(s);}
 static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
 static void Main(){
  var settings=new UIAnimationSettings();var sfxSettings=new UISfxSettings();AudioClip first=new AudioClip(),second=new AudioClip();sfxSettings.click.clips=new[]{first,second};
  var last=first;for(int i=0;i<100;i++){var next=sfxSettings.Choose(sfxSettings.click,last);Check(next!=last,"avoid immediate repeat");last=next;}
  sfxSettings.click.clips=new[]{(AudioClip)null,first};Check(sfxSettings.Choose(sfxSettings.click,first)==first,"single valid clip");sfxSettings.click.clips=new AudioClip[2];Check(sfxSettings.Choose(sfxSettings.click,null)==null,"empty clips safe");sfxSettings.click.clips=new[]{first,second};
  sfxSettings.transition.clips=new[]{first,second};sfxSettings.finishTask.clips=new[]{first,second};
  var voice=Make<AudioSource>();var player=Make<UISfxPlayer>();Set(player,"settings",sfxSettings);Set(player,"source",voice);Call(player,"Awake");player.Play(UISfxKind.Click);player.Play(UISfxKind.Click);Check(voice.shots==1,"same group frame dedupe");Time.frameCount++;
  var button=Make<Button>();var visual=new GameObject().transform;visual.parent=button.transform;visual.localScale=new Vector3(.9f,.9f,.9f);var group=button.gameObject.Add(new CanvasGroup());
  var feedback=button.gameObject.Add(new UIButtonFeedback());Set(feedback,"settings",settings);Set(feedback,"visual",visual);Set(feedback,"entranceGroup",group);Call(feedback,"Awake");
  button.onClick.Invoke();Check(voice.shots==1,"programmatic invoke silent");
  var pointer=new PointerEventData();feedback.OnPointerDown(pointer);feedback.OnPointerUp(pointer);button.onClick.Invoke();Check(voice.shots==2,"one accepted mouse click");button.onClick.Invoke();Check(voice.shots==2,"no duplicate click");
  Time.frameCount++;button.interactable=false;feedback.OnPointerDown(pointer);feedback.OnPointerUp(pointer);button.onClick.Invoke();Check(voice.shots==2,"disabled click silent");
  Time.frameCount++;button.interactable=true;feedback.OnPointerDown(pointer);feedback.OnPointerUp(pointer);button.interactable=false;button.onClick.Invoke();Check(voice.shots==3,"successful callback disabling button still sounds");
  Time.frameCount++;button.interactable=true;button.onClick.Invoke();button.interactable=false;feedback.OnSubmit(new BaseEventData());Check(voice.shots==4,"keyboard accepted before callback disables button");
  button.interactable=true;feedback.Reveal(4);Check(!group.interactable&&group.alpha==0,"stagger initially blocks hidden button");DOTween.CompleteAll();Check(group.interactable&&group.alpha==1&&Near(visual.localScale.y,.9f),"reveal restores exact pose");
  feedback.OnPointerEnter(pointer);feedback.SelectedFeedback();Call(feedback,"OnDisable");DOTween.CompleteAll();Check(Near(visual.localScale.x,.9f),"button interruption no scale drift");
  feedback.OnPointerEnter(pointer);DOTween.CompleteAll();settings.animationsEnabled=false;feedback.OnPointerExit(pointer);Check(Near(visual.localScale.x,.9f),"disabling animations restores authored button scale");settings.animationsEnabled=true;
  var motion=Make<UIVisualMotion>();var rect=(RectTransform)motion.transform;rect.anchoredPosition=new Vector2(43,-92);rect.localScale=new Vector3(1.2f,1.2f,1.2f);Set(motion,"settings",settings);Set(motion,"visual",rect);Call(motion,"Awake");
  for(int i=0;i<30;i++){motion.Enter();motion.Exit();motion.Enter();DOTween.CompleteAll();Check(Near(rect.anchoredPosition.x,43)&&Near(rect.anchoredPosition.y,-92)&&Near(rect.localScale.x,1.2f),"reopen preserves authored pose");}
  var pro=motion.gameObject.Add(new DOTweenAnimation());Set(motion,"proEntrance",pro);rect.localScale=new Vector3(1.2f,1.2f,1.2f);motion.Enter();Check(Near(pro.endValueV3.x,1.2f*.985f),"Pro entrance uses authored scale, not absolute one");Call(motion,"OnDisable");Set(motion,"proEntrance",null);
  motion.Pulse();Call(motion,"OnDisable");DOTween.CompleteAll();Check(Near(rect.localScale.x,1.2f),"motion disable cancels callbacks");
  var phase=new AtmospherePhaseData();AtmosphereManager.Instance=new AtmosphereManager{AppliedPhase=phase};settings.phaseMotion.Add(new UIAnimationSettings.PhaseMotion{phase=phase,amount=.35f});Check(Near(settings.MotionAmount,.35f),"configured existing atmosphere restraint");
  var ui=Make<UIManager>();var menu=Make<CanvasGroup>();var resume=Make<CanvasGroup>();var company=Make<CanvasGroup>();var interview=Make<CanvasGroup>();Set(ui,"mainMenu",menu);Set(ui,"resumeEditor",resume);Set(ui,"company",company);Set(ui,"interview",interview);Set(ui,"animationSettings",settings);Call(ui,"Awake");int transitions=0;ui.SectionChanged+=()=>transitions++;
  ui.ShowPanelFor(GamePhase.MainMenu,true);Check(menu.alpha==1&&transitions==0,"first menu no transition SFX");ui.ShowPanelFor(GamePhase.ResumeEditor);ui.ShowPanelFor(GamePhase.CompanySelection);DOTween.CompleteAll();Check(company.interactable&&company.alpha==1&&!resume.interactable&&resume.alpha==0&&!menu.interactable,"rapid transition final input ownership");
  ui.ShowPanelFor(GamePhase.InterviewIntroduction);DOTween.CompleteAll();int before=transitions;ui.ShowPanelFor(GamePhase.InterviewReaction);Check(transitions==before,"same interview section does not restart transition");
  var r=Make<ResumeEditorController>();var c=Make<CompanySelectionController>();var hr=Make<InterviewController>();var observer=Make<UIPresentationAudio>();Set(observer,"ui",ui);Set(observer,"resume",r);Set(observer,"companies",c);Set(observer,"interview",hr);Set(observer,"sfx",player);Call(observer,"OnEnable");Time.frameCount++;int shots=voice.shots;r.Raise();Check(voice.shots==shots+1,"successful resume completion");Time.frameCount++;c.Raise();Time.frameCount++;hr.Raise();Check(voice.shots==shots+3,"apply/interview completion");Call(observer,"OnDisable");Time.frameCount++;r.Raise();Check(voice.shots==shots+3,"observer listeners cleaned");
  var dialogue=Make<DialogueUI>();var dg=dialogue.gameObject.Add(new CanvasGroup());Set(dialogue,"group",dg);Set(dialogue,"animationSettings",settings);Call(dialogue,"Awake");dialogue.Show();dialogue.Hide();dialogue.Show();DOTween.CompleteAll();Check(dg.alpha==1&&dg.blocksRaycasts,"dialogue rapid hide/show restores visibility");dialogue.Hide();Check(!dg.blocksRaycasts,"dialogue exit immediately releases pointer");DOTween.CompleteAll();Check(dg.alpha==0,"dialogue exit ends hidden");
  Console.WriteLine("PASS SFX random/null handling, input gating/dedup/cleanup, button and foreground pose restoration, phase restraint, rapid panel interruption, section/finish events, dialogue visibility (API doubles; not Unity Play Mode).");
 }
}

