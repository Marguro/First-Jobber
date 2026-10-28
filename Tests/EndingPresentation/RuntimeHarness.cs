// Real EndingController/EndingData with a coroutine scheduler and Unity API doubles.
// Does not validate native VideoPlayer codecs, Timeline graphs, audio or rendering.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FirstJobber;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.Video;
class Harness
{
    static void Check(bool v,string message){if(!v)throw new Exception(message);}
    static void Set(object o,string f,object v)=>o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
    static void Call(object o,string m)=>o.GetType().GetMethod(m,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
    static void Tick(int count=1){for(int i=0;i<count;i++){Time.realtimeSinceStartup+=.1f;Scheduler.Tick();}}
    static EndingController New(out GameContext ctx,out VideoPlayer video,out PlayableDirector director,out AspectRatioFitter aspect)
    {
        var c=new EndingController();ctx=new GameContext();video=new VideoPlayer();director=new PlayableDirector();aspect=new AspectRatioFitter();
        Set(c,"videoPlayer",video);Set(c,"director",director);Set(c,"videoRoot",new GameObject());Set(c,"videoImage",new RawImage());Set(c,"videoAspect",aspect);Set(c,"fadeOverlay",new CanvasGroup());Set(c,"buttonsRoot",new GameObject());Set(c,"audioSource",new AudioSource());Set(c,"prepareTimeout",1f);Set(c,"playbackGrace",1f);Call(c,"Awake");return c;
    }
    static EndingData Data(EndingPresentationMode mode)=>new EndingData{id="ending",presentationMode=mode,fadeInDuration=0,fadeOutDuration=0,lines=new DialogueLine[0]};
    static void Main()
    {
        GameContext ctx;VideoPlayer video;PlayableDirector director;AspectRatioFitter aspect;
        var c=New(out ctx,out video,out director,out aspect);int done=0;c.PresentationCompleted+=e=>done++;
        var e=Data(EndingPresentationMode.None);c.Show(ctx,e);c.Show(ctx,e);Tick(5);Check(done==1,"None/duplicate Show completion wrong");
        var noVideo=Data(EndingPresentationMode.Video);c.Show(ctx,noVideo);Tick(5);Check(done==2,"Missing video softlocked");
        var v=Data(EndingPresentationMode.Video);v.videoClip=new VideoClip{width=640,height=480,audioTrackCount=2};c.Show(ctx,v);Tick(3);
        Check(video.isPlaying && !video.isLooping && !video.playOnAwake && video.AudioEnabled==2,"Video/audio configuration wrong");
        Check(Math.Abs(aspect.aspectRatio-4f/3)<.001f,"Video aspect ratio wrong");
        var raw=(RawImage)c.GetType().GetField("videoImage",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c);
        Check(raw.texture==video.targetTexture && raw.texture!=null,"RawImage and VideoPlayer must share the live target");
        Check(raw.uvRect.width==1 && raw.uvRect.height==1,"Video UVs must sample the whole frame");
        video.End();video.End();Tick(5);Check(done==3 && video.targetTexture==null && RenderTexture.Alive==0,"Video natural completion/cleanup duplicated or leaked");
        c.Show(ctx,v);Tick(3);c.RequestSkip();c.RequestSkip();Tick(5);Check(done==4 && !video.isPlaying,"Skip completion duplicated");
        v.allowSkip=false;c.Show(ctx,v);Tick(3);c.RequestSkip();Tick(3);Check(done==4 && video.isPlaying,"Allow Skip=false ignored");video.Error();Tick(5);Check(done==5,"Video error softlocked");
        video.AutoPrepare=false;c.Show(ctx,v);Tick(30);Check(done==6,"Prepare timeout softlocked");video.AutoPrepare=true;
        c.Show(ctx,v);Tick(3);Tick(40);Check(done==7,"Playback watchdog did not recover");
        var t=Data(EndingPresentationMode.Timeline);t.timeline=new PlayableAsset{duration=2};c.Show(ctx,t);Tick(3);director.Stop();Tick(5);Check(done==8,"Timeline stopped event completion wrong");
        c.Show(ctx,t);Tick(3);director.state=PlayState.Paused;Tick(2);Check(done==8,"A paused director was incorrectly treated as completion");director.Stop();Tick(5);Check(done==9,"Paused Timeline cannot complete");
        t.allowSkip=false;c.Show(ctx,t);Tick(3);c.RequestSkip();Tick(2);Check(done==9,"Timeline skip restriction ignored");director.Stop();Tick(5);
        t.allowSkip=true;c.Show(ctx,t);Tick(3);c.RequestSkip();Tick(5);Check(done==11,"Timeline skip failed");
        var missing=Data(EndingPresentationMode.Timeline);c.Show(ctx,missing);Tick(5);Check(done==12,"Missing Timeline softlocked");
        t.timeline.duration=double.PositiveInfinity;c.Show(ctx,t);Tick(5);Check(done==13,"Infinite Timeline softlocked");t.timeline.duration=2;
        var key=new UnityEngine.Timeline.AudioTrack{name="Audio"};var original=new AudioSource();var general=new AudioSource();var specific=new AudioSource();
        t.timeline.outputs=new[]{new PlayableBinding{sourceObject=key,outputTargetType=typeof(AudioSource)}};director.SetGenericBinding(key,original);
        Set(c,"timelineBindings",new[]{new EndingController.TimelineBinding{trackName="Audio",target=general},new EndingController.TimelineBinding{endingId="ending",trackName="Audio",target=specific}});
        c.Show(ctx,t);Tick(3);Check(director.GetGenericBinding(key)==specific,"Ending-specific binding did not win");director.Stop();Tick(5);Check(director.GetGenericBinding(key)==original,"Timeline binding was not restored");
        c.Show(ctx,v);Tick(3);Call(c,"OnDisable");Tick(5);Check(done==14 && RenderTexture.Alive==0 && video.EndListeners==0 && video.ErrorListeners==0,"Disable completed a cancelled ending or leaked listeners/resources");
        var restart=Data(EndingPresentationMode.None);restart.actionAfterCutscene=EndingAfterCutsceneAction.RestartDay1;Check(restart.RestartsCycle,"Restart action ignored");restart.actionAfterCutscene=EndingAfterCutsceneAction.CompleteEnding;Check(!restart.RestartsCycle,"Complete action ignored");restart.resetCycle=true;Check(restart.RestartsCycle,"Legacy restart flag lost");
        FlowProbe.CheckRestart();
        Console.WriteLine("PASS actual ending presenter: None, Video natural/skip/error/timeouts/audio/aspect, Timeline stop/pause/skip/bindings/invalid duration, duplicate guards, cancellation, resource/subscription cleanup and restart action");
    }
}
namespace UnityEngine
{
    public class Object {public string name;}
    public class ScriptableObject:Object {}
    public class GameObject:Object
    {
        public bool activeSelf=true;public Transform transform;readonly Dictionary<Type,Component> components=new Dictionary<Type,Component>();
        public GameObject(){transform=new Transform(this);}public void SetActive(bool v)=>activeSelf=v;
        public T AddComponent<T>()where T:Component,new(){var c=new T();c.gameObject=this;components[typeof(T)]=c;return c;}
        public Component GetComponent(Type t)=>components.TryGetValue(t,out var c)?c:null;
        public T[] GetComponentsInChildren<T>(bool inactive)where T:Component=>new T[0];
    }
    public class Transform:Object {public GameObject gameObject;public Transform(GameObject go){gameObject=go;}}
    public class Component:Object {public GameObject gameObject=new GameObject();public Transform transform=>gameObject.transform;}
    public class MonoBehaviour:Component
    {
        public T GetComponent<T>()where T:Component=>gameObject.GetComponent(typeof(T)) as T;
        public Coroutine StartCoroutine(IEnumerator e){var c=new Coroutine(e);Scheduler.All.Add(c);return c;}
        public void StopCoroutine(Coroutine c){c.Stopped=true;}
        public static GameObject Instantiate(GameObject go,Transform parent)=>new GameObject();public static void Destroy(Object o){}
    }
    public class Coroutine {public bool Stopped;public Stack<IEnumerator> Stack=new Stack<IEnumerator>();public Coroutine(IEnumerator e){Stack.Push(e);}}
    public static class Scheduler
    {
        public static List<Coroutine> All=new List<Coroutine>();public static void Tick(){foreach(var c in All.ToArray()){if(c.Stopped)continue;while(c.Stack.Count>0){var e=c.Stack.Peek();if(!e.MoveNext()){c.Stack.Pop();continue;}if(e.Current is IEnumerator nested){c.Stack.Push(nested);continue;}break;}}All.RemoveAll(c=>c.Stopped||c.Stack.Count==0);}
    }
    public class Sprite:Object {}public class AudioClip:Object {}public class AudioSource:Component {public void PlayOneShot(AudioClip clip){}}
    public class CanvasGroup:Component {public float alpha;public bool blocksRaycasts;}
    public class Texture:Object {}public enum RenderTextureFormat{ARGB32}
    public class RenderTexture:Texture {public static int Alive;bool released;public RenderTexture(int w,int h,int d,RenderTextureFormat f){Alive++;}public bool Create()=>true;public void Release(){if(!released){Alive--;released=true;}}}
    public struct Color {public static Color black=>new Color();public static Color white=>new Color();}
    public static class Time {public static float realtimeSinceStartup;public static float unscaledDeltaTime=.1f;}
    public static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static int RoundToInt(float v)=>(int)Math.Round(v);public static float Lerp(float a,float b,float t)=>a+(b-a)*t;}
    public static class Debug {public static void Log(string s,Object o){}public static void LogWarning(string s,Object o){}public static void LogError(string s,Object o){throw new Exception(s);}}
    public class SerializeField:Attribute {}public class HeaderAttribute:Attribute {public HeaderAttribute(string s){}}public class TooltipAttribute:Attribute {public TooltipAttribute(string s){}}public class MinAttribute:Attribute {public MinAttribute(float n){}}public class TextAreaAttribute:Attribute {}public class HideInInspector:Attribute {}public class CreateAssetMenuAttribute:Attribute {public string menuName,fileName;}
}
namespace TMPro {public class TMP_Text:Component {public string text;}}
namespace UnityEngine.UI
{
    public class Image:Component {public Sprite sprite;public Color color;}
    public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}}
    public class RawImage:Component {public Texture texture;public Rect uvRect;}
    public class AspectRatioFitter:Component {public enum AspectMode{FitInParent}public AspectMode aspectMode;public float aspectRatio;}
    public class ClickEvent {public void AddListener(Action a){}public void RemoveListener(Action a){}}
    public class Button:Component {public ClickEvent onClick=new ClickEvent();}
}
namespace UnityEngine.InputSystem
{
    public class Key {public bool wasPressedThisFrame;}
    public class Keyboard {public static Keyboard current;public Key escapeKey,spaceKey,enterKey;}
    public class Mouse {public static Mouse current;public Key leftButton;}
}
namespace UnityEngine.Playables
{
    public struct PlayableBinding {public UnityEngine.Object sourceObject;public Type outputTargetType;}
    public class PlayableAsset:UnityEngine.Object {public double duration=2;public IEnumerable<PlayableBinding> outputs=new PlayableBinding[0];}
    public enum PlayState{Paused,Playing}public enum DirectorUpdateMode{UnscaledGameTime}public enum DirectorWrapMode{None}
    public class PlayableDirector:Component
    {
        public bool playOnAwake;public PlayableAsset playableAsset;public PlayState state;public DirectorUpdateMode timeUpdateMode;public DirectorWrapMode extrapolationMode;public double time;public double duration=>playableAsset.duration;
        public event Action<PlayableDirector> stopped;readonly Dictionary<UnityEngine.Object,UnityEngine.Object> bindings=new Dictionary<UnityEngine.Object,UnityEngine.Object>();
        public void Play(){state=PlayState.Playing;}public void Stop(){state=PlayState.Paused;stopped?.Invoke(this);}public UnityEngine.Object GetGenericBinding(UnityEngine.Object k)=>bindings.TryGetValue(k,out var v)?v:null;public void SetGenericBinding(UnityEngine.Object k,UnityEngine.Object v)=>bindings[k]=v;public void ClearGenericBinding(UnityEngine.Object k)=>bindings.Remove(k);
    }
}
namespace UnityEngine.Timeline {public class AudioTrack:UnityEngine.Object {}}
namespace UnityEngine.Video
{
    public class VideoClip:UnityEngine.Object {public uint width,height;public ushort audioTrackCount;}
    public enum VideoSource{VideoClip}public enum VideoRenderMode{RenderTexture}public enum VideoAudioOutputMode{Direct}public enum VideoTimeUpdateMode{UnscaledGameTime}
    public class VideoPlayer:Component
    {
        public bool AutoPrepare=true,isPrepared,isPlaying,playOnAwake,isLooping,waitForFirstFrame;public VideoClip clip;public uint width=>clip.width;public uint height=>clip.height;public double length=1;
        public VideoSource source;public VideoRenderMode renderMode;public VideoAudioOutputMode audioOutputMode;public VideoTimeUpdateMode timeUpdateMode;public ushort controlledAudioTrackCount;public int AudioEnabled;public RenderTexture targetTexture;
        Action<VideoPlayer> end;Action<VideoPlayer,string> error;public int EndListeners=>end?.GetInvocationList().Length??0;public int ErrorListeners=>error?.GetInvocationList().Length??0;
        public event Action<VideoPlayer> prepareCompleted;
        public event Action<VideoPlayer> loopPointReached {add{end+=value;}remove{end-=value;}}public event Action<VideoPlayer,string> errorReceived {add{error+=value;}remove{error-=value;}}
        public void Prepare(){isPrepared=AutoPrepare;AudioEnabled=controlledAudioTrackCount;if(isPrepared)prepareCompleted?.Invoke(this);}public void Play(){isPlaying=true;}public void Stop(){isPlaying=false;isPrepared=false;}public void End()=>end?.Invoke(this);public void Error()=>error?.Invoke(this,"test decoder error");public void EnableAudioTrack(ushort i,bool v){}public void SetDirectAudioMute(ushort i,bool v){}public void SetDirectAudioVolume(ushort i,float v){}
    }
}
namespace FirstJobber
{
    public class DialogueLine {}public class DialogueManager {public void Play(DialogueLine[] lines,Action callback){callback();}}
    public class GameContext {public DialogueManager Dialogue=new DialogueManager();}
    public static class LocalizationManager {public static string Get(EndingData e,string field,string english)=>english;}
}

// Music handoff is exercised separately in Tests/DayBgm; this harness isolates ending presentation.
namespace FirstJobber { public class DayBgmController : UnityEngine.MonoBehaviour { public System.Collections.IEnumerator SilenceAndWait() { yield break; } } }
