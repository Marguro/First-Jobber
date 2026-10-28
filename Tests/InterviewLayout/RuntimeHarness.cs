// Actual scoped layout helper with RectTransform/TMP API doubles; not native Unity layout/rendering.
using System;
using System.Reflection;
using FirstJobber;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
class LayoutHarness
{
    static void Set(object o,string f,object v)=>o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Main()
    {
        var preset=new InterviewDialogueLayout();var root=new RectTransform{sizeDelta=new Vector2(100,200),anchoredPosition=new Vector2(3,4)};
        var speaker=new TMP_Text{fontSize=44,color=new Color(.1f,.2f,.3f),text="Real HR name"};var body=new TMP_Text{fontSize=40,color=new Color(.4f,.5f,.6f),text="Original authored dialogue"};
        speaker.margin=new Vector4(0,0,486.85376f,0);speaker.textWrappingMode=TextWrappingModes.Normal;
        speaker.fontSizeMin=18;speaker.fontSizeMax=72;body.textWrappingMode=TextWrappingModes.Normal;
        var arrow=new RectTransform{sizeDelta=new Vector2(162,68)};var image=new Image{sprite=new Sprite(),color=new Color(.2f,.3f,.4f),type=Image.Type.Sliced,preserveAspect=false};var originalSprite=image.sprite;
        var rootMarker=new RectTransform{sizeDelta=new Vector2(1840,293),anchoredPosition=new Vector2(40,20)};var speakerMarker=new RectTransform{anchoredPosition=new Vector2(20,30)};var bodyMarker=new RectTransform{anchoredPosition=new Vector2(40,50)};var arrowMarker=new RectTransform{sizeDelta=new Vector2(60,34)};var paper=new Sprite();var binding=new AtmosphereSpriteBinding{enabled=true,BaseSprite=image.sprite,BaseColor=image.color,Target=image};
        Set(preset,"dialogueRoot",root);Set(preset,"speakerText",speaker);Set(preset,"bodyText",body);Set(preset,"continueIndicator",arrow);Set(preset,"background",image);Set(preset,"backgroundBinding",binding);
        Set(preset,"rootLayout",rootMarker);Set(preset,"speakerLayout",speakerMarker);Set(preset,"bodyLayout",bodyMarker);Set(preset,"continueLayout",arrowMarker);Set(preset,"paperSprite",paper);
        for(int cycle=0;cycle<4;cycle++)
        {
            root.sizeDelta=new Vector2(100+cycle,200);
            preset.SetInterviewActive(true);
            Check(root.sizeDelta.x==1840 && root.anchoredPosition.x==40 && speaker.rectTransform.anchoredPosition.x==20 && body.rectTransform.anchoredPosition.x==40 && arrow.sizeDelta.x==60,"Authored marker layout not applied");
            Check(image.sprite==paper && image.type==Image.Type.Simple && image.preserveAspect && binding.enabled && speaker.fontSize==32 && body.fontSize==28,"Interview paper style not applied or phase binding was disabled");
            Check(speaker.margin.z==0 && speaker.textWrappingMode==TextWrappingModes.NoWrap && speaker.enableAutoSizing && speaker.fontSizeMin==26 && speaker.fontSizeMax==32,"Old speaker margin/wrapping still collapses the name area");
            Check(speaker.horizontalAlignment==HorizontalAlignmentOptions.Left && body.textWrappingMode==TextWrappingModes.Normal,"Speaker alignment or body wrapping changed incorrectly");
            // Phase changes inside one interview must not snap an artist's live Rect Tool adjustments back.
            root.anchoredPosition=new Vector2(77,88);preset.SetInterviewActive(true);Check(root.anchoredPosition.x==77,"Every interview sub-phase reapplied positions");
            preset.SetInterviewActive(false);
            Check(root.sizeDelta.x==100+cycle && root.anchoredPosition.x==3 && root.anchoredPosition.y==4 && arrow.sizeDelta.x==162,"Leaving interview did not restore original layout");
            Check(image.sprite==originalSprite && image.type==Image.Type.Sliced && !image.preserveAspect && binding.enabled && speaker.fontSize==44 && body.fontSize==40,"Other screens' style was not restored");
            Check(speaker.margin.z==486.85376f && speaker.textWrappingMode==TextWrappingModes.Normal && !speaker.enableAutoSizing && speaker.fontSizeMin==18 && speaker.fontSizeMax==72,"Speaker settings leaked to other screens");
            Check(speaker.text=="Real HR name" && body.text=="Original authored dialogue","Layout helper rewrote content/tokens");
        }
        binding.enabled=false;preset.SetInterviewActive(true);preset.SetInterviewActive(false);Check(!binding.enabled,"Restore enabled an originally disabled atmosphere binding");
        foreach(var name in new[]{"Player","Father","Narrator","CCTV-Head HR","A Long English Human Resources Director","หัวหน้าฝ่ายทรัพยากรบุคคลประจำบริษัท"})
        {speaker.text=name;preset.SetInterviewActive(true);Check(speaker.text==name && speaker.textWrappingMode==TextWrappingModes.NoWrap,"Speaker name rewritten/wrapping enabled");preset.SetInterviewActive(false);}
        Console.WriteLine("PASS actual interview dialogue preset: Rect Tool markers, scoped paper style, no repeated per-phase snapping, default layout/style/atmosphere restoration, recapture after artist edits, no dialogue content mutation");
    }
}
namespace UnityEngine
{
    public class Object{}public class MonoBehaviour:Object{}public class Sprite:Object{}
    public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}}
    public struct Vector3{public float x,y,z;}
    public struct Vector4{public float x,y,z,w;public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}public static Vector4 zero=>new Vector4();}
    public static class Mathf{public static float Min(float a,float b)=>Math.Min(a,b);}
    public struct Color{public float r,g,b,a;public Color(float a,float b,float c,float d=1){r=a;g=b;this.b=c;this.a=d;}public static Color white=>new Color(1,1,1);}
    public class RectTransform:Object {public Vector2 anchorMin,anchorMax,anchoredPosition,sizeDelta,pivot;public Vector3 localScale;}
    public class SerializeField:Attribute{}public class HeaderAttribute:Attribute{public HeaderAttribute(string s){}}public class MinAttribute:Attribute{public MinAttribute(float n){}}
    public class TooltipAttribute:Attribute{public TooltipAttribute(string s){}}
}
namespace TMPro{
 public enum TextWrappingModes{NoWrap,Normal}public enum TextOverflowModes{Overflow,Ellipsis}public enum HorizontalAlignmentOptions{Left,Right}public enum VerticalAlignmentOptions{Top,Middle}
 public class TMP_Text{public RectTransform rectTransform=new RectTransform();public Color color;public float fontSize,fontSizeMin,fontSizeMax;public string text;public Vector4 margin;public TextWrappingModes textWrappingMode;public TextOverflowModes overflowMode;public HorizontalAlignmentOptions horizontalAlignment;public VerticalAlignmentOptions verticalAlignment;public bool enableAutoSizing;}}
namespace UnityEngine.UI{public class Image {public enum Type{Simple,Sliced}public Sprite sprite;public Color color;public Type type;public bool preserveAspect;}}
namespace FirstJobber{public class AtmosphereSpriteBinding {public bool enabled;public Sprite BaseSprite;public Color BaseColor;public Image Target;public void SetAuthoredVisual(Sprite sprite,Color color){BaseSprite=sprite;BaseColor=color;if(Target!=null){Target.sprite=sprite;Target.color=color;}}public void Apply(object phase,float fade){}}}
