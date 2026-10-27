// Real ScrollingBackground code with Unity UI/time/event test doubles; not Unity Play Mode.
using System;
using System.Collections.Generic;
using System.Reflection;
using FirstJobber;
using UnityEngine;
using UnityEngine.UI;

class BackgroundHarness
{
    static void Check(bool value,string message) { if(!value) throw new Exception(message); }
    static void Set(object target,string field,object value) => target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    static void Call(object target,string method) => target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
    static void Tick(ScrollingBackground c,float dt=1f) { Time.unscaledDeltaTime=dt;Call(c,"LateUpdate"); }
    static Vector4 V(RawImage layer,string property) => layer.material.GetVector(Shader.PropertyToID(property));
    static float Frac(float value) => value-(float)Math.Floor(value);
    // CPU reference for the shader's periodic coordinate mapping; GPU rendering is validated separately in Unity.
    static Vector2 MotifUV(RawImage layer,Vector2 pixel)
    {
        var cell=V(layer,"_CellSize");var offset=V(layer,"_ScrollOffset");var motif=V(layer,"_MotifOffset");var size=V(layer,"_ElementSize");
        var rotation=V(layer,"_MotifRotation");
        float x=(Frac((pixel.x-offset.x-motif.x)/cell.x+.5f)-.5f)*cell.x;
        float y=(Frac((pixel.y-offset.y-motif.y)/cell.y+.5f)-.5f)*cell.y;
        return new Vector2((rotation.x*x+rotation.y*y)/size.x+.5f,(-rotation.y*x+rotation.x*y)/size.y+.5f);
    }
    static int Bits(int mask) { int count=0;for(int i=0;i<4;i++) if((mask&(1<<i))!=0) count++;return count; }
    static void Main()
    {
        var root=new GameObject("Background",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollingBackground));
        root.GetComponent<RectTransform>().sizeDelta=new Vector2(1920,1080);
        var c=root.GetComponent<ScrollingBackground>();var slots=SlotFixture.Create();
        Set(c,"mode",ScrollingBackground.ScrollMode.FourMotifs);Set(c,"motifSlots",slots);Set(c,"motifRandomSeed",37);Set(c,"wallpaperShader",new Shader());
        var originalMaterials=new Material[4];for(int i=0;i<4;i++) { originalMaterials[i]=slots[i].image.material;slots[i].isBlurred=true; }
        Call(c,"Awake");Call(c,"OnEnable");Tick(c,0);
        Check(c.BlurredCount==0,"Startup did not reset to clear");
        int objects=GameObject.Created,materials=Material.Created;
        Check(materials==8,"Expected four authored materials and exactly four runtime instances");
        c.Speed=150;
        for(int frame=0;frame<3600;frame++)
        {
            var oldUV=MotifUV(slots[0].image,new Vector2(123,27));var delta=c.Direction.normalized*c.Speed/60f;
            Tick(c,1f/60f);
            Check((MotifUV(slots[0].image,new Vector2(123,27)+delta)-oldUV).magnitude<.01f,"UV jump across a scrolling period");
            for(int i=0;i<4;i++)
            {
                var layer=slots[i].image;var cell=V(layer,"_CellSize");var scroll=V(layer,"_ScrollOffset");var motif=V(layer,"_MotifOffset");
                var center=new Vector2(scroll.x+motif.x,scroll.y+motif.y);
                Check((MotifUV(layer,center)-new Vector2(.5f,.5f)).magnitude<.001f,"Incorrect motif centre");
                Check((MotifUV(layer,center+new Vector2(cell.x,cell.y))-new Vector2(.5f,.5f)).magnitude<.001f,"Repeat seam mismatch");
                Check(layer.rectTransform.anchoredPosition==Vector2.zero,"Animated RectTransform found");
                Check(layer.rectTransform.sizeDelta==new Vector2(1920,1080),"Quad geometry changed every frame");
                Check(layer.texture==slots[i].normalSprite.texture && !layer.raycastTarget,"Incorrect clear texture/raycast");
            }
        }
        Check(GameObject.Created==objects && Material.Created==materials,"Per-frame objects/materials created");
        pausedTiltTest(c,slots);
        c.Speed=0;var paused=c.WallpaperOffset;Tick(c,10);Check(c.WallpaperOffset==paused,"Speed zero did not pause");
        foreach(float speed in new[] {10f,50f,150f})
        {
            var before=c.WallpaperOffset;c.Speed=speed;Tick(c);var d=c.Direction.normalized*speed;
            Check((c.WallpaperOffset-new Vector2(Mathf.Repeat(before.x+d.x,960),Mathf.Repeat(before.y+d.y,640))).magnitude<.01f,"Live speed ignored/reset offset");
        }
        c.Direction=new Vector2(-1,.3f);paused=c.WallpaperOffset;Tick(c);var direction=c.Direction.normalized*c.Speed;
        Check((c.WallpaperOffset-new Vector2(Mathf.Repeat(paused.x+direction.x,960),Mathf.Repeat(paused.y+direction.y,640))).magnitude<.01f,"Direction ignored");
        Set(c,"enableScrolling",false);paused=c.WallpaperOffset;Tick(c,10);Check(c.WallpaperOffset==paused,"Scrolling toggle ignored");Set(c,"enableScrolling",true);
        int mask=0;
        for(int n=1;n<=4;n++)
        {
            paused=c.WallpaperOffset;Check(c.AdvanceRandomDeteriorationStep(),"Missing normal motif");
            Check(c.BlurredCount==n && Bits(c.BlurredMask^mask)==1 && (c.BlurredMask&mask)==mask,"Repeated or multiple deterioration selection");
            Check(c.WallpaperOffset==paused,"Sprite change reset UV offset");mask=c.BlurredMask;
        }
        Check(!c.AdvanceRandomDeteriorationStep(),"Already faded motif selected again");
        for(int frame=0;frame<600;frame++)
        {
            Tick(c,.1f);for(int i=0;i<4;i++) Check(slots[i].image.texture==slots[i].blurredSprite.texture,"Faded texture reverted after wrapping");
        }
        foreach(var size in new[] {new Vector2(800,600),new Vector2(1280,720),new Vector2(2560,1080),new Vector2(3840,2160),new Vector2(1920,1080)})
        {
            root.GetComponent<RectTransform>().sizeDelta=size;Tick(c,0);
            Check(V(slots[0].image,"_RectSize").x==size.x && V(slots[0].image,"_RectSize").y==size.y,"Resize ignored");
            Check(GameObject.Created==objects && Material.Created==materials,"Resize spawned objects/materials");
        }
        c.ResetBackgroundState();Check(c.BlurredMask==0,"Reset failed");c.SetBlurredCount(2);mask=c.BlurredMask;c.SetBlurredCount(2);Check(c.BlurredMask==mask,"Count rerolled");
        c.ApplyBlurredMask(10);Check(c.BlurredMask==10,"State restoration failed");c.ResetBackgroundState();
        AtmosphereManager.Raise(new AtmospherePhaseData {blurredBackgroundElements=2});Tick(c,.1f);Check(c.BlurredCount==1,"First phase step failed");
        for(int i=0;i<6;i++) Tick(c);Check(c.BlurredCount==2,"Second phase step failed");
        AtmosphereManager.Raise(new AtmospherePhaseData {blurredBackgroundElements=4});Tick(c,100);Check(c.BlurredCount==3,"Phase replaced everything at once");
        for(int i=0;i<6;i++) Tick(c);Check(c.BlurredCount==4,"Final phase count failed");
        AtmosphereManager.Raise(new AtmospherePhaseData {blurredBackgroundElements=0});
        var update=(Action)Delegate.CreateDelegate(typeof(Action),c,typeof(ScrollingBackground).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic));
        Time.unscaledDeltaTime=1f/60f;for(int i=0;i<200;i++) update();
        var allocationMethod=typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread");
        if(allocationMethod!=null)
        {
            var bytes=(Func<long>)Delegate.CreateDelegate(typeof(Func<long>),allocationMethod);long before=bytes();
            for(int i=0;i<3600;i++) update();long allocated=bytes()-before;
            Check(allocated==0,"Steady-state test-double update allocated managed memory: "+allocated);
            Console.WriteLine("PASS managed controller harness allocation: 0 bytes over 3600 steady-state updates (not a Unity Profiler measurement)");
        }
        Check(GameObject.Created==objects && Material.Created==materials,"Uncontrolled growth");
        Call(c,"OnDisable");for(int i=0;i<4;i++) Check(slots[i].image.material==originalMaterials[i],"Original material not restored");
        Console.WriteLine("PASS four fixed layers/material instances, no moving quads, 60s UV continuity, repeat seams, live speed/direction, resize without growth, normal/faded mapping and existing atmosphere APIs.");
        Console.WriteLine("GPU rendering, draw batches, native UI rebuild cost and FPS require Unity validation/Profiler.");
    }
    static void pausedTiltTest(ScrollingBackground c,BackgroundMotifSlot[] slots)
    {
        var offset=c.WallpaperOffset;
        foreach(float angle in new[] {90f,-45f,180f})
        {
            slots[0].tiltAngle=angle;Tick(c,0);
            var rotation=V(slots[0].image,"_MotifRotation");double radians=angle*Math.PI/180;
            Check(Math.Abs(rotation.x-Math.Cos(radians))<.0001 && Math.Abs(rotation.y-Math.Sin(radians))<.0001,"Tilt was not uploaded live");
            Check(c.WallpaperOffset==offset,"Tilt change reset scrolling");
            var cell=V(slots[0].image,"_CellSize");var size=V(slots[0].image,"_ElementSize");
            Check(Math.Abs(rotation.x)*size.x+Math.Abs(rotation.y)*size.y<=cell.x && Math.Abs(rotation.y)*size.x+Math.Abs(rotation.x)*size.y<=cell.y,"Rotated art crosses repeat seam");
        }
        c.ApplyBlurredMask(1);Check(V(slots[0].image,"_MotifRotation").x<-.99f,"Faded sprite lost tilt");
        c.ResetBackgroundState();slots[0].tiltAngle=0;Tick(c,0);
        Console.WriteLine("PASS live per-motif tilt, offset preservation, faded-state tilt and rotated cell bounds");
    }
}

namespace UnityEngine
{
    public class Object { public string name; public HideFlags hideFlags; public static void Destroy(Object obj) { } public static void DestroyImmediate(Object obj) { } }
    public class Component : Object
    { public GameObject gameObject; public Transform transform => gameObject.transform; public T GetComponent<T>() where T:Component => gameObject.GetComponent<T>(); }
    public class MonoBehaviour : Component { }
    public class Transform : Component
    { public Vector3 localScale; public void SetParent(Transform parent,bool worldPositionStays) { } }
    public class RectTransform : Transform
    { public Vector2 sizeDelta,anchorMin,anchorMax,pivot,anchoredPosition; public Rect rect => new Rect(sizeDelta.x,sizeDelta.y); }
    public class GameObject : Object
    {
        public static int Created;
        Dictionary<Type,Component> components = new Dictionary<Type,Component>();
        public Transform transform;
        public bool activeSelf=true;
        public GameObject(string name,params Type[] types)
        {
            Created++; this.name=name;
            foreach(var type in types)
            { var c=(Component)Activator.CreateInstance(type); c.gameObject=this; components[type]=c; if(c is Transform t) transform=t; }
        }
        public T GetComponent<T>() where T:Component => (T)components[typeof(T)];
        public void SetActive(bool value) { activeSelf=value; }
    }
    public enum HideFlags { DontSave }
    public struct Rect { public float x,y,width,height; public Rect(float w,float h):this(0,0,w,h) { } public Rect(float x,float y,float w,float h) { this.x=x;this.y=y;width=w;height=h; } public Vector2 size=>new Vector2(width,height); }
    public class Texture : Object { public int width,height; }
    public class Texture2D : Texture { public Texture2D(int w,int h) { width=w;height=h; } }
    public class Sprite : Object { public Texture2D texture=new Texture2D(400,400); public Rect textureRect=new Rect(400,400); public Rect rect=>textureRect; }
    public class Shader : Object { public static int PropertyToID(string value)=>value.GetHashCode(); }
    public class Material : Object
    {
        public static int Created; public Shader shader;
        readonly Dictionary<int,Vector4> vectors=new Dictionary<int,Vector4>(8);
        public Material(Shader shader) { this.shader=shader;Created++; }
        public void SetVector(int id,Vector4 v) { vectors[id]=v; }
        public Vector4 GetVector(int id)=>vectors[id];
    }
    public struct Vector2
    {
        public float x,y;
        public Vector2(float x,float y) { this.x=x;this.y=y; }
        public static Vector2 one=>new Vector2(1,1); public static Vector2 zero=>new Vector2(0,0);
        public float sqrMagnitude=>x*x+y*y; public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
        public Vector2 normalized=>magnitude>0?this/magnitude:zero;
        public static Vector2 Scale(Vector2 a,Vector2 b)=>new Vector2(a.x*b.x,a.y*b.y);
        public static Vector2 Lerp(Vector2 a,Vector2 b,float k)=>a+(b-a)*k;
        public static Vector2 operator+(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);
        public static Vector2 operator-(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);
        public static Vector2 operator-(Vector2 a)=>zero-a;
        public static Vector2 operator*(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);
        public static Vector2 operator/(Vector2 a,float b)=>new Vector2(a.x/b,a.y/b);
        public static bool operator==(Vector2 a,Vector2 b)=>a.x==b.x&&a.y==b.y;
        public static bool operator!=(Vector2 a,Vector2 b)=>!(a==b);
        public override bool Equals(object obj)=>obj is Vector2 v&&this==v;
        public override int GetHashCode()=>x.GetHashCode()^y.GetHashCode();
    }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; } public static Vector3 one=>new Vector3(1,1,1); }
    public struct Vector4 { public float x,y,z,w; public Vector4(float x,float y,float z,float w) { this.x=x;this.y=y;this.z=z;this.w=w; } public static Vector4 zero=>new Vector4(0,0,0,0); }
    public struct Color { public float r,g,b,a; public static Color white=>new Color {r=1,g=1,b=1,a=1}; }
    public static class Mathf
    {
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Min(float a,float b)=>Math.Min(a,b);
        public static float Abs(float a)=>Math.Abs(a);
        public static float Repeat(float a,float b)=>a-(float)Math.Floor(a/b)*b;
        public static float Lerp(float a,float b,float k)=>a+(b-a)*k;
        public static float SmoothStep(float a,float b,float k)=>Lerp(a,b,k*k*(3-2*k));
        public static int CeilToInt(float a)=>(int)Math.Ceiling(a);
        public static int FloorToInt(float a)=>(int)Math.Floor(a);
        public static int Clamp(int a,int lo,int hi)=>Math.Max(lo,Math.Min(hi,a));
    }
    public static class Time { public static float unscaledDeltaTime=1f,deltaTime=1f; }
    public static class Application { public static bool isPlaying=true; }
    public static class Random { public static int Range(int a,int b)=>new System.Random(42).Next(a,b); }
    public class SerializeField:Attribute { }
    public class HideInInspector:Attribute { }
    public class HeaderAttribute:Attribute { public HeaderAttribute(string s) { } }
    public class TooltipAttribute:Attribute { public TooltipAttribute(string s) { } }
    public class MinAttribute:Attribute { public MinAttribute(float n) { } }
    public class RangeAttribute:Attribute { public RangeAttribute(float a,float b) { } }
    public class ContextMenu:Attribute { public ContextMenu(string s) { } }
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=true)] public class RequireComponent:Attribute { public RequireComponent(Type t) { } }
}
namespace UnityEngine.UI
{
    public class RectMask2D:Component { public Vector4 padding; }
    public class RawImage:Component { public Texture texture; public Material material; public Color color; public bool enabled=true,raycastTarget; public RectTransform rectTransform=>(RectTransform)transform; }
    public class Image:Component { public Sprite sprite; public Color color; public bool enabled=true,raycastTarget,preserveAspect; public RectTransform rectTransform=>(RectTransform)transform; }
}
namespace FirstJobber
{
    public class AtmospherePhaseData { public int blurredBackgroundElements=-1; }
    public class AtmosphereManager
    {
        public static AtmosphereManager Instance=new AtmosphereManager();
        public AtmospherePhaseData AppliedPhase;
        public static event Action<AtmospherePhaseData> PhaseChanged;
        public static void Raise(AtmospherePhaseData phase) { Instance.AppliedPhase=phase;PhaseChanged?.Invoke(phase); }
    }
}
