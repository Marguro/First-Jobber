using System;
using System.Reflection;
namespace UnityEngine {
 public class Object { public string name="test"; } public class ScriptableObject:Object {} public class MonoBehaviour:Object { public bool isActiveAndEnabled=true; }
 public class AudioClip:Object {}
 public class AudioSource:Object { public bool isActiveAndEnabled=true,isPlaying,mute; public float spatialBlend; public AudioClip clip; public bool loop,ignoreListenerPause; public float volume; public Audio.AudioMixerGroup outputAudioMixerGroup; public int plays; public bool stopped=true,paused; public void Play(){plays++;stopped=false;} public void Stop(){stopped=true;} public void Pause(){paused=true;} public void UnPause(){paused=false;} }
 public static class AudioListener {public static bool pause; public static float volume=1;}
 public static class Time {public static float timeScale=1,unscaledDeltaTime=.1f;}
 public static class Debug {public static void Log(string s,Object o){}public static void LogError(string s,Object o) {throw new Exception(s);} public static void LogWarning(string s,Object o){} }
 public static class Mathf { public static float Max(float a,float b)=>Math.Max(a,b);public static float Clamp01(float a)=>Math.Max(0,Math.Min(1,a));public static float Lerp(float a,float b,float t)=>a+(b-a)*t;}
 public class SerializeField:Attribute{} public class MinAttribute:Attribute{public MinAttribute(float x){}} public class RangeAttribute:Attribute{public RangeAttribute(float a,float b){}} public class TooltipAttribute:Attribute{public TooltipAttribute(string x){}} public class ContextMenu:Attribute{public ContextMenu(string x){}} public class CreateAssetMenuAttribute:Attribute{public string menuName;}
}
namespace UnityEngine.Audio {public class AudioMixerGroup{}}
class Test {
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
 static void Tick(object o,int n=20){for(int i=0;i<n;i++)o.GetType().GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);}
 static void Check(bool b,string n){if(!b)throw new Exception(n);}
 static void Main(){
 var c=new FirstJobber.DayBgmConfiguration();var a=new UnityEngine.AudioSource();var b=new UnityEngine.AudioSource();var p=new FirstJobber.DayBgmController();Set(p,"configuration",c);Set(p,"sourceA",a);Set(p,"sourceB",b);
 for(int d=1;d<=6;d++)c.days.Add(new FirstJobber.DayBgmConfiguration.DayMusic{day=d,audioClip=new UnityEngine.AudioClip()});
 p.PlayDay(1);Tick(p);Check(a.clip==c.days[0].audioClip && a.volume==1,"day1");p.PlayDay(1);Check(a.plays==1,"same day no restart");
 for(int d=2;d<=6;d++){p.PlayDay(d);Tick(p);Check((a.clip==c.days[d-1].audioClip&&!a.stopped)||(b.clip==c.days[d-1].audioClip&&!b.stopped),"day mapping");Check(a.stopped||b.stopped,"no old voice");}
 c.masterBgmVolume=.4f;c.days[5].volumeMultiplier=.5f;Tick(p);Check(Math.Abs(b.volume-.2f)<.001f,"live volumes");
 p.PlayDay(2);Tick(p,2);p.PlayDay(3);Tick(p);Check(a.stopped||b.stopped,"interrupted fade cleanup");
 c.days[3].audioClip=null;c.missingClipBehavior=FirstJobber.MissingBgmBehavior.KeepPrevious;int plays=a.plays+b.plays;p.PlayDay(4);Tick(p);Check(a.plays+b.plays==plays,"keep previous");
 c.days[4].enabled=false;p.PlayDay(5);Tick(p);Check(a.stopped&&b.stopped,"disabled silence");
 p.PlayDay(1);Tick(p);UnityEngine.Time.timeScale=0;Tick(p);Check(a.paused&&b.paused,"pause");p.Silence();Tick(p);Check(a.stopped&&b.stopped,"fade ends during pause");UnityEngine.Time.timeScale=1;Tick(p);
 p.PlayDay(1);Tick(p);Check(!a.stopped||!b.stopped,"restart after ending");c.enableCrossfade=false;p.PlayDay(2);Tick(p);Check(a.stopped||b.stopped,"sequential cleanup");
 Console.WriteLine("PASS six days, no same-day restart, live volumes, interruption, missing/disabled entries, pause, ending silence/restart, sequential fades (API doubles, not Play Mode)");
 }
}
