// Actual catalog, sprite binding and overlay material policy. Not native Unity stencil/rasterization tests.
using System;
using System.Collections.Generic;
using System.Reflection;
using FirstJobber;
using UnityEngine;
using UnityEngine.UI;
class PhaseHarness
{
    static void Check(bool v,string s){if(!v)throw new Exception(s);}
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
    static void Call(object o,string n)=>o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
    static AtmosphereOverlayMask Mask(Graphic graphic,AtmosphereSpriteBinding binding)
    {var m=new AtmosphereOverlayMask();Set(m,"graphic",graphic);m.Configure(binding);return m;}
    static void Main()
    {
        var p1=new AtmospherePhaseData();var p2=new AtmospherePhaseData{uiTint=new Color(.8f,.7f,.6f,.4f)};var p3=new AtmospherePhaseData{uiTint=new Color(.5f,.4f,.3f,.1f)};
        var lib=new PhaseUISpriteLibrary{phase2=p2,phase3=p3};var unified=new Sprite();lib.companyPhase3=unified;var originals=new Sprite[5];var second=new Sprite[5];
        for(int i=0;i<5;i++){originals[i]=new Sprite();second[i]=new Sprite();lib.entries.Add(new PhaseUISpriteLibrary.Entry{original=originals[i],phase2Sprite=second[i],phase3Sprite=new Sprite(),family=PhaseUIFamily.CompanyBackground});}
        var partial=new Sprite();var partialP2=new Sprite();lib.entries.Add(new PhaseUISpriteLibrary.Entry{original=partial,phase2Sprite=partialP2});
        var answerUnified=new Sprite();lib.answerPhase3=answerUnified;var answers=new Sprite[5];
        for(int i=0;i<5;i++){answers[i]=new Sprite();lib.entries.Add(new PhaseUISpriteLibrary.Entry{original=answers[i],phase2Sprite=i<2?null:new Sprite(),phase3Sprite=new Sprite(),family=PhaseUIFamily.AnswerBackground});}
        Sprite resolved;
        for(int i=0;i<5;i++)Check(lib.TryResolve(answers[i],p3,out resolved)&&resolved==answerUnified,"Answer family did not enforce ONE shared Phase3 sprite");
        for(int i=0;i<5;i++)
        {Check(lib.TryResolve(originals[i],p1,out resolved)&&resolved==null,"Normal did not use original");Check(lib.TryResolve(originals[i],p2,out resolved)&&resolved==second[i],"Variant Phase2 wrong");Check(lib.TryResolve(originals[i],p3,out resolved)&&resolved==unified,"Unified Phase3 family wrong");}
        var manager=new AtmosphereManager();AtmosphereManager.Instance=manager;manager.AppliedPhase=p3;
        var image=new Image{sprite=originals[0],color=new Color(.8f,.6f,.4f,.37f)};var binding=new AtmosphereSpriteBinding();Set(binding,"image",image);var mask=Mask(image,binding);binding.ConfigureUI(lib,originals[0],mask);
        Call(binding,"OnEnable");Check(image.sprite==unified && image.color.Equals(new Color(.8f,.6f,.4f,.37f)) && binding.DedicatedSpriteActive,"Newly active UI did not immediately use current phase without tint/alpha loss");
        var source=new Material();var protectedMat=mask.GetModifiedMaterial(source);
        Check(protectedMat.GetInt("_Stencil")==128 && protectedMat.GetInt("_StencilWriteMask")==128 && protectedMat.GetInt("_StencilOp")==2,"Dedicated graphic does not protect its coverage");
        Check(source.GetInt("_StencilOp")==0,"Original shared material was mutated");
        var selectedColor=new Color(.3f,.9f,.7f,.65f);
        binding.SetAuthoredVisual(originals[4],selectedColor);Check(image.sprite==unified && binding.BaseSprite==originals[4] && image.color.Equals(selectedColor),"Company data binding overwrote Phase3 or lost selected color/original variant");
        binding.RefreshAuthoredMapping();Check(binding.BaseSprite==originals[4],"Inspector mapping refresh reset a company random variant to the prefab default");
        manager.AppliedPhase=p2;binding.Apply(p2,3);Check(image.sprite==second[4] && image.color.Equals(selectedColor),"Earlier phase did not restore corresponding original variant");
        manager.AppliedPhase=p1;binding.Apply(p1,0);Check(image.sprite==originals[4] && !binding.DedicatedSpriteActive,"Normal did not restore original");
        binding.SetAuthoredVisual(partial,selectedColor);binding.Apply(p2,0);Check(image.sprite==partialP2,"Partial Phase2 missing");binding.Apply(p3,0);
        Check(image.sprite==partial && !binding.DedicatedSpriteActive && Math.Abs(image.color.r-.15f)<.001 && image.color.a==selectedColor.a,"Missing Phase3 used Phase2/empty or failed original+tint fallback");
        var clearMat=mask.GetModifiedMaterial(source);Check(clearMat.GetInt("_Stencil")==0,"Fallback is still exempt from fullscreen atmosphere");
        var foreground=new Image();var foregroundMask=Mask(foreground,null);var clearForeground=foregroundMask.GetModifiedMaterial(source);Check(clearForeground==clearMat,"Normal foreground materials are not shared");
        // Simulate the reserved bit only: protected art skips overlay; a later normal glyph clears it.
        int stencil=128;stencil=(stencil & ~clearForeground.GetInt("_StencilWriteMask"))|(clearForeground.GetInt("_Stencil") & clearForeground.GetInt("_StencilWriteMask"));Check((stencil & 128)==0,"Normal foreground leaked exemption from art behind it");
        int created=Material.Created;for(int i=0;i<100;i++)Check(foregroundMask.GetModifiedMaterial(source)==clearForeground,"Material not reused");Check(Material.Created==created,"Per-refresh material allocation");
        var maskedSource=new Material();maskedSource.SetInt("_Stencil",3);maskedSource.SetInt("_StencilReadMask",3);maskedSource.SetInt("_StencilWriteMask",0);maskedSource.SetInt("_StencilComp",3);
        manager.AppliedPhase=p3;binding.SetAuthoredVisual(originals[2],selectedColor);var nested=mask.GetModifiedMaterial(maskedSource);
        Check(nested.GetInt("_Stencil")==131 && nested.GetInt("_StencilReadMask")==3 && nested.GetInt("_StencilComp")==3,"Existing UGUI mask bits were broken");
        var animated=new Material();animated.SetInt("_ScrollOffset",10);var motionMask=Mask(new Image(),null);Set(motionMask,"syncAnimatedMaterial",true);Call(motionMask,"OnEnable");var animatedResult=motionMask.GetModifiedMaterial(animated);animated.SetInt("_ScrollOffset",99);Canvas.Render();Check(animatedResult.GetInt("_ScrollOffset")==99,"Wallpaper UV animation froze behind cached material");
        Call(motionMask,"OnDisable");Call(foregroundMask,"OnDisable");Call(mask,"OnDisable");Call(binding,"OnDisable");Check(AtmosphereManager.Registered==0,"Bindings leaked after disable");
        Console.WriteLine("PASS actual phase UI policy: original/Phase2/unified Phase3, missing-phase fallback, phase restoration, dynamic original/state updates, authored alpha/color, immediate current phase, overlay exemption/foreground clear/UGUI mask bits, shared cached materials and animated wallpaper uniforms");
    }
}
namespace UnityEngine
{
    public class Object {public string name;public static void Destroy(Object o){}public static void DestroyImmediate(Object o){}}
    public class ScriptableObject:Object{}public class MonoBehaviour:Object{public bool isActiveAndEnabled=true;public T GetComponent<T>()where T:class=>null;public Coroutine StartCoroutine(System.Collections.IEnumerator e)=>new Coroutine();public void StopCoroutine(Coroutine c){}}
    public class Coroutine{}public class Sprite:Object{}public class SpriteRenderer:MonoBehaviour{public Sprite sprite;public Color color;}
    public struct Color{public float r,g,b,a;public Color(float x,float y,float z,float w=1){r=x;g=y;b=z;a=w;}public static Color white=>new Color(1,1,1);public static Color Lerp(Color a,Color b,float t)=>b;}
    public static class Time{public static float unscaledDeltaTime=.1f;}public static class Mathf{public static float Abs(float v)=>Math.Abs(v);}public static class Application{public static bool isPlaying=true;}
    public static class Canvas{public static event Action willRenderCanvases;public static void Render()=>willRenderCanvases?.Invoke();}
    public enum HideFlags{HideAndDontSave}
    public class Material:Object
    {public static int Created;public object shader;public HideFlags hideFlags;Dictionary<string,int> values=new Dictionary<string,int>();public Material(){Created++;SetInt("_StencilReadMask",255);SetInt("_StencilWriteMask",255);SetInt("_StencilComp",8);}public Material(Material m){Created++;CopyPropertiesFromMaterial(m);}public bool HasProperty(string s)=>true;public void CopyPropertiesFromMaterial(Material m){values=new Dictionary<string,int>(m.values);}public int GetInt(string s)=>values.TryGetValue(s,out var v)?v:0;public void SetInt(string s,int v)=>values[s]=v;public void SetFloat(string s,float v)=>SetInt(s,(int)v);public void EnableKeyword(string s){}}
    public class SerializeField:Attribute{}public class TooltipAttribute:Attribute{public TooltipAttribute(string s){}}public class HeaderAttribute:Attribute{public HeaderAttribute(string s){}}public class DisallowMultipleComponent:Attribute{}public class RequireComponent:Attribute{public RequireComponent(Type t){}}public class CreateAssetMenuAttribute:Attribute{public string menuName,fileName;}
}
namespace UnityEngine.Rendering {public enum StencilOp{Keep=0,Replace=2}}
namespace UnityEngine.UI
{
    public interface IMaterialModifier{Material GetModifiedMaterial(Material source);}
    public class Graphic:MonoBehaviour{public void SetMaterialDirty(){}}
    public class Image:Graphic{public Sprite sprite;public Color color;}
}
namespace FirstJobber
{
    public class AtmospherePhaseData:ScriptableObject{public Color uiTint=Color.white;public Sprite GetSprite(string id)=>null;}
    public class AtmosphereManager{public static AtmosphereManager Instance;public AtmospherePhaseData AppliedPhase;public static int Registered;public static void Register(AtmosphereSpriteBinding b){Registered++;}public static void Unregister(AtmosphereSpriteBinding b){Registered--;}}
}
