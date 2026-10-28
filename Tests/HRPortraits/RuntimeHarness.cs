// Real HRCharacterData, HRReactionController, DialogueManager and planner with Unity API doubles.
using System;
using System.Collections.Generic;
using System.Reflection;
using FirstJobber;
using UnityEngine;
using UnityEngine.UI;
class Harness
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
    static void Call(object o,string method)=>o.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);
    static Sprite Art(string name)=>new Sprite{name=name};
    static HREmotionVisual Visual(HREmotion e,Sprite s)=>new HREmotionVisual{emotion=e,sprite=s};
    static void Drain(DialogueManager player)
    {
        for(int i=0;i<2000;i++){var r=player.Pending;if(r==null)return;if(!r.MoveNext() && player.Pending==r)player.Pending=null;}
        throw new Exception("Dialogue did not finish");
    }
    static void Main()
    {
        var an=Art("A neutral");var ai=Art("A interested");var ad=Art("A disappointed");var bi=Art("B interested");var bd=Art("B disappointed");var bn=Art("B neutral");var wrong=Art("foreign legacy portrait");
        var a=new HRCharacterData{id="a",hrName="A",defaultPortrait=Art("A default"),animatorController=new RuntimeAnimatorController()};
        var b=new HRCharacterData{id="b",hrName="B"};
        a.emotionVisuals.AddRange(new[]{Visual(HREmotion.Neutral,an),Visual(HREmotion.Interested,ai),Visual(HREmotion.Disappointed,ad),Visual(HREmotion.Uncomfortable,null)});
        b.emotionVisuals.AddRange(new[]{Visual(HREmotion.Neutral,bn),Visual(HREmotion.Interested,bi),Visual(HREmotion.Disappointed,bd)});
        var company=new CompanyData{hr=a};var player=new DialogueManager();var ui=new DialogueUI();Set(player,"ui",ui);player.HrCharacterProvider=()=>company.hr;
        var reaction=new HRReactionController();var image=new Image();var animator=new Animator();Set(reaction,"dialogue",player);Set(reaction,"portrait",image);Set(reaction,"animator",animator);Call(reaction,"OnEnable");
        var shared=new InterviewQuestionData{id="shared",hrEmotion=HREmotion.Interested,questionDialogue=new[]{
            new DialogueLine{speaker=Speakers.HR,emotion=HREmotion.Interested,portrait=wrong,dialogueLines=new List<string>{"Interested question"}},
            new DialogueLine{speaker=Speakers.HR,emotion=HREmotion.Disappointed,portrait=wrong,dialogueLines=new List<string>{"Response page 1","Response page 2"}}
        }};
        reaction.SetCharacter(a);player.Play(shared.questionDialogue,null,1f);Drain(player);
        Check(ui.Portraits.Count==3 && ui.Portraits[0]==ai && ui.Portraits[1]==ad && ui.Portraits[2]==ad,"A per-block/multipage portrait");
        Check(image.sprite==ad && image.enabled,"Main HR portrait must agree with dialogue portrait");
        ui.Portraits.Clear();company.hr=b;reaction.SetCharacter(b);player.Play(shared.questionDialogue,null,1f);Drain(player);
        Check(ui.Portraits.Count==3 && ui.Portraits[0]==bi && ui.Portraits[1]==bd && ui.Portraits[2]==bd && image.sprite==bd,"Same shared question must use B, never A");
        Check(animator.runtimeAnimatorController==null,"B without animator must clear A controller");
        Check(shared.questionDialogue[0].portrait==wrong,"Runtime must not mutate legacy serialized references");
        Check(a.ResolveSprite(HREmotion.Uncomfortable,null,out _)==an,"Null requested row must fall back to same-HR Neutral");
        var def=new HRCharacterData{defaultPortrait=Art("default")};Check(def.ResolveSprite(HREmotion.Interested,null,out _)==def.defaultPortrait,"Default fallback");
        var phase=new AtmospherePhaseData();var ap=Art("A phase");var bp=Art("B phase");
        a.atmosphereVariants.Add(new HRAtmosphereVariant{phase=phase,sprites=new List<AtmosphereHrSpriteOverride>{new AtmosphereHrSpriteOverride{emotion=HREmotion.Interested,sprite=ap}}});
        b.atmosphereVariants.Add(new HRAtmosphereVariant{phase=phase,sprites=new List<AtmosphereHrSpriteOverride>{new AtmosphereHrSpriteOverride{emotion=HREmotion.Interested,sprite=bp}}});
        Check(a.ResolveSprite(HREmotion.Interested,phase,out _)==ap && b.ResolveSprite(HREmotion.Interested,phase,out _)==bp,"Per-HR phase overrides");
        Check(a.ResolveSprite(HREmotion.Disappointed,phase,out _)==ad,"Missing phase expression uses standard requested expression");
        AtmosphereManager.Instance=new AtmosphereManager{AppliedPhase=phase};
        Check(player.ResolvePortrait(shared.questionDialogue[0])==bp,"Dialogue must use current phase for current HR");
        AtmosphereManager.Instance=null;
        foreach(string speaker in new[]{"Player","Father","Narrator"})Check(player.ResolvePortrait(new DialogueLine{speaker=speaker,portrait=wrong})==wrong,"Non-HR portrait changed");
        company.hr=null;reaction.SetCharacter(null);Check(player.ResolvePortrait(shared.questionDialogue[0])==null && image.sprite==null && !image.enabled,"Missing context must clear old HR, never use line override");
        company.hr=new HRCharacterData{id="empty"};int warnings=Debug.Warnings;
        for(int i=0;i<5;i++)Check(player.ResolvePortrait(shared.questionDialogue[0])==null,"Empty HR fallback");
        Check(Debug.Warnings==warnings+1,"Warn once per unconfigured HR, not per page");
        company.hr=b;player.GenderIdProvider=()=>"female";ui.Portraits.Clear();
        player.Play(new[]{new DialogueLine{speaker="HR",text="male",genderIds=new[]{"male"},variantGroup="gender",emotion=HREmotion.Interested},new DialogueLine{speaker="HR",text="female",genderIds=new[]{"female"},variantGroup="gender",emotion=HREmotion.Disappointed}},null,1f);Drain(player);
        Check(ui.Portraits.Count==1 && ui.Portraits[0]==bd,"Gender filtering and emotion mapping must stay independent");
        // Real planner still returns two Fixed + two ResumeLinked with the same shared asset.
        var choice=new ResumeChoiceData();var other=new ResumeChoiceData();shared.linkedChoice=choice;shared.answers.Add(new InterviewAnswer());
        var linked2=new InterviewQuestionData{id="linked2",linkedChoice=other};linked2.answers.Add(new InterviewAnswer());
        foreach(var hr in new[]{a,b})
        {
            company.hr=hr;
            for(int i=0;i<2;i++){var q=new InterviewQuestionData{id=hr.id+"fixed"+i,hrEmotion=HREmotion.Disappointed};q.answers.Add(new InterviewAnswer());hr.fixedQuestions.Add(q);}
            hr.resumeLinkedQuestions.AddRange(new[]{shared,linked2});
            hr.sequence=new List<InterviewSlot>{new InterviewSlot(),new InterviewSlot(),new InterviewSlot{type=InterviewSlotType.ResumeLinked},new InterviewSlot{type=InterviewSlotType.ResumeLinked}};
            hr.fixedPickMode=hr.resumeLinkedPickMode=QuestionPickMode.Random;
            var fixedLine=new DialogueLine("HR","Fixed question",hr.fixedQuestions[0].hrEmotion);
            Check(player.ResolvePortrait(fixedLine)==(hr==a?ad:bd),"Fixed question must resolve the current HR's Disappointed portrait");
            var result=new List<InterviewQuestionData>();
            for(int i=0;i<20;i++)
            {
                HrInterviewPlanner.Plan(hr,new GameSession(),new[]{choice,other},company,result);
                Check(result.Count==4 && hr.fixedQuestions.Contains(result[0]) && hr.fixedQuestions.Contains(result[1]) && result.Contains(shared) && result.Contains(linked2),"2 Fixed + 2 shared Linked changed");
                Check(new HashSet<InterviewQuestionData>(result).Count==4,"Duplicate questions");
            }
        }
        Console.WriteLine("PASS A-J logic: same shared question/current HR, fixed emotions, per-block/multipage, Neutral/Default/empty, company change/animator clear, per-HR phase, non-HR preservation, gender filtering, warnings once, no mutation and real planner 2+2 (40 runs). Native rendering/Play Mode not executed.");
    }
}
namespace UnityEngine
{
    public class Object{public string name;}
    public class ScriptableObject:Object{}
    public class Sprite:Object{}
    public class RuntimeAnimatorController:Object{}
    public class Animator{public RuntimeAnimatorController runtimeAnimatorController;}
    public class Coroutine{}
    public class MonoBehaviour:Object{public System.Collections.IEnumerator Pending;public Coroutine StartCoroutine(System.Collections.IEnumerator r){Pending=r;return new Coroutine();}public void StopCoroutine(Coroutine r){Pending=null;}}
    public class RectTransform {public Vector2 anchoredPosition;public void Rotate(float x,float y,float z){}}
    public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 operator+(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);}
    public struct Color {public float r,g,b,a;public static Color white=>new Color{r=1,g=1,b=1,a=1};public static Color operator*(Color a,Color b)=>new Color{r=a.r*b.r,g=a.g*b.g,b=a.b*b.b,a=a.a*b.a};}
    public class SerializeField:Attribute{}public class HideInInspector:Attribute{}public class HeaderAttribute:Attribute{public HeaderAttribute(string s){}}public class TooltipAttribute:Attribute{public TooltipAttribute(string s){}}public class MinAttribute:Attribute{public MinAttribute(float n){}}public class TextAreaAttribute:Attribute{public TextAreaAttribute(int a=0,int b=0){}}public class CreateAssetMenuAttribute:Attribute{public string menuName,fileName;}
    public static class Debug {public static int Warnings;public static void LogWarning(string s,Object c=null){Warnings++;}}
    public class WaitForSecondsRealtime{public WaitForSecondsRealtime(float n){}}
    public static class Time{public static float unscaledDeltaTime=1,unscaledTime=0;}
    public static class Mathf {public static int Min(int a,int b)=>Math.Min(a,b);public static int FloorToInt(float n)=>(int)Math.Floor(n);public static float Sin(float f)=>(float)Math.Sin(f);}
    public static class Random{static System.Random r=new System.Random(123);public static int Range(int a,int b)=>r.Next(a,b);public static float Range(float a,float b)=>a;}
}
namespace UnityEngine.UI{public class Image {public Sprite sprite;public Color color;public bool enabled=true;}}
namespace TMPro{public class TMP_Text{public string text;}}
namespace FirstJobber
{
    public class DialogueUI {public event Action Clicked;public List<Sprite> Portraits=new List<Sprite>();public void Show(){}public void Hide(){}public int SetLine(string s,string t,Sprite p){Portraits.Add(p);return 0;}public void SetContinueIndicator(bool b){}public void SetVisibleCharacters(int n){}}
    public static class LocalizationManager {public static string Get(string k,string en)=>en;}
    public static class LocalizationKeys {public static string SpeakerKey(string s)=>"speaker/"+s;}
    public class ResumeCategoryData:ScriptableObject{}
    public class ResumeChoiceData:ScriptableObject{public ResumeCategoryData category;public bool HasTag(string s)=>false;}
    public class CompanyData:ScriptableObject{public HRCharacterData hr;public bool HasTag(string s)=>false;}
    public class GameSession{public List<string> askedQuestionIds=new List<string>();}
    public class AtmospherePhaseData{}
    public class AtmosphereHrSpriteOverride{public HREmotion emotion;public Sprite sprite;}
    public class AtmosphereManager {public static AtmosphereManager Instance;public AtmospherePhaseData AppliedPhase;public Color HrTint=>Color.white;public static event Action<AtmospherePhaseData> PhaseChanged;}
    public static class InterviewQuestionSelector{public static InterviewQuestionData GetEmergencyQuestion()=>new InterviewQuestionData();}
}
