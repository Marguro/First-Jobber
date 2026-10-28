// Actual InterviewController response coroutine, DialogueManager, HRCharacterData and HRReactionController.
// Unity UI/lifecycle/clock are doubles: native rendering and wall-clock timing are not Play Mode verified.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FirstJobber;
using UnityEngine;
using UnityEngine.UI;
class ResponseHarness
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
    static void Call(object o,string method)=>o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
    static IEnumerator Response(InterviewController controller,InterviewAnswer answer)=>(IEnumerator)typeof(InterviewController).GetMethod("PlayHrResponse",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,new object[]{answer});
    static void DrainDialogue(DialogueManager player)
    {
        for(int i=0;i<1000;i++){var r=player.Pending;if(r==null)return;Call(player,"OnClicked");if(!r.MoveNext()&&player.Pending==r)player.Pending=null;}
        throw new Exception("Dialogue stuck");
    }
    static List<float> Run(IEnumerator routine,DialogueManager player,Action<float> onWait=null)
    {
        var waits=new List<float>();var stack=new Stack<IEnumerator>();stack.Push(routine);
        for(int step=0;step<10000;step++)
        {
            if(stack.Count==0)return waits;
            var top=stack.Peek();if(!top.MoveNext()){stack.Pop();continue;}
            if(top.Current is IEnumerator nested){stack.Push(nested);continue;}
            if(top.Current is WaitForSecondsRealtime wait){waits.Add(wait.Seconds);onWait?.Invoke(wait.Seconds);}
            else DrainDialogue(player);
        }
        throw new Exception("Interview or response stuck");
    }
    static HRCharacterData Character(string id)
    {
        var hr=new HRCharacterData{id=id,hrName=id};
        foreach(HREmotion emotion in Enum.GetValues(typeof(HREmotion)))hr.emotionVisuals.Add(new HREmotionVisual{emotion=emotion,sprite=new Sprite{name=id+emotion}});
        return hr;
    }
    static void Main()
    {
        var a=Character("A");var b=Character("B");var player=new DialogueManager();var ui=new DialogueUI();Set(player,"ui",ui);
        var portrait=new Image();var reaction=new HRReactionController();Set(reaction,"portrait",portrait);Set(reaction,"dialogue",player);Call(reaction,"OnEnable");reaction.SetCharacter(a);
        var company=new CompanyData{hr=a};player.HrCharacterProvider=()=>company.hr;
        var ctx=new GameContext{SelectedCompany=company,Dialogue=player,Session=new GameSession(),Database=new GameDatabase(),Choices=new ChoiceManager()};
        var controller=new InterviewController();Set(controller,"ctx",ctx);Set(controller,"hr",reaction);Set(controller,"neutralDelay",.4f);
        var silent=new InterviewAnswer{applyHrReactionEmotion=true,hrReactionEmotion=HREmotion.Uncomfortable,portraitOnlyDuration=.8f,hrResponse=new DialogueLine[0]};
        var pending=Response(controller,silent);
        Check(pending.MoveNext() && pending.Current is WaitForSecondsRealtime,"Silent reaction must yield before reset");
        Check(((WaitForSecondsRealtime)pending.Current).Seconds==.8f && reaction.Current==HREmotion.Uncomfortable && portrait.enabled,"Silent duration or emotion incorrect");
        Check(ui.Shows==0 && !ui.Visible && player.Pending==null,"Silent response opened an empty box or waits for hidden input");
        Check(!pending.MoveNext() && reaction.Current==HREmotion.Uncomfortable,"Response helper reset emotion immediately");
        company.hr=b;reaction.SetCharacter(b);Run(Response(controller,silent),player);
        Check(portrait.sprite.name=="BUncomfortable","Shared answer borrowed previous HR portrait");
        var spoken=new InterviewAnswer{applyHrReactionEmotion=true,hrReactionEmotion=HREmotion.Disappointed,hrResponse=new[]{new DialogueLine{speaker="HR",emotion=HREmotion.Disappointed,portrait=new Sprite{name="foreign"},dialogueLines=new List<string>{"Response one","Response two"}}}};
        var waits=Run(Response(controller,spoken),player);
        Check(waits.Count==1 && waits[0]==.4f && ui.Portraits.Count==2 && ui.Portraits[0].name=="BDisappointed" && ui.Portraits[1].name=="BDisappointed","Spoken response changed pages/portrait or gained a silent delay");
        var randomResponse=new InterviewAnswer{hrResponseMode=DialoguePlaybackMode.RandomOne,hrResponse=new[]{new DialogueLine("HR","One",HREmotion.Interested),new DialogueLine("HR","Two",HREmotion.Disappointed)}};
        int pages=ui.Portraits.Count;
        Run(Response(controller,randomResponse),player);
        Check(ui.Portraits.Count==pages+1,"RandomOne response played multiple candidates");
        int shows=ui.Shows;
        waits=Run(Response(controller,new InterviewAnswer{hrResponse=new DialogueLine[0]}),player);
        Check(waits.Count==1 && waits[0]==.4f && ui.Shows==shows,"Unconfigured old answers acquired a new delay or blank page");
        var legacy=new InterviewAnswer{hrResponse=new[]{new DialogueLine{speaker="HR",emotion=HREmotion.Interested}},portraitOnlyDuration=.6f};
        waits=Run(Response(controller,legacy),player);Check(reaction.Current==HREmotion.Interested && waits[0]==.6f && ui.Shows==shows,"Legacy emotion-only HR block was skipped or shown as blank text");
        player.GenderIdProvider=()=>"female";
        silent.hrResponse=new[]{new DialogueLine{speaker="HR",genderIds=new[]{"male"},text="Male-only response"}};
        Run(Response(controller,silent),player);Check(ui.Shows==shows && reaction.Current==HREmotion.Uncomfortable,"Pacing must use gender-applicable content");
        player.GenderIdProvider=null;silent.hrResponse=new DialogueLine[0];
        var phase=new AtmospherePhaseData();var variant=new Sprite{name="B phase uncomfortable"};
        b.atmosphereVariants.Add(new HRAtmosphereVariant{phase=phase,sprites=new List<AtmosphereHrSpriteOverride>{new AtmosphereHrSpriteOverride{emotion=HREmotion.Uncomfortable,sprite=variant}}});
        AtmosphereManager.Instance=new AtmosphereManager{AppliedPhase=phase};Run(Response(controller,silent),player);Check(portrait.sprite==variant,"Silent phase variant failed");AtmosphereManager.Instance=null;
        b.emotionVisuals.RemoveAll(v=>v.emotion==HREmotion.Uncomfortable);Run(Response(controller,silent),player);Check(portrait.sprite.name=="BNeutral","Silent response bypassed same-HR fallback");
        player.Play(new[]{new DialogueLine("HR","Next neutral question",HREmotion.Neutral)},null,1f);DrainDialogue(player);Check(reaction.Current==HREmotion.Neutral,"Next explicit Neutral must update portrait");
        var nonHr=new Sprite{name="father"};Check(player.ResolvePortrait(new DialogueLine{speaker="Father",portrait=nonHr})==nonHr,"Non-HR portrait changed");
        // Run the complete actual interview coroutine. Fake mouse dispatch attempts a second answer too.
        company.hr=a;reaction.SetCharacter(a);var choice=new ResumeChoiceData();ctx.Choices.Selected.Add(choice);
        a.sequence=new List<InterviewSlot>{new InterviewSlot(),new InterviewSlot(),new InterviewSlot{type=InterviewSlotType.ResumeLinked},new InterviewSlot{type=InterviewSlotType.ResumeLinked}};
        for(int i=0;i<4;i++)
        {
            var q=new InterviewQuestionData{id="question"+i,questionText="Question "+i,hrEmotion=HREmotion.Interested};
            q.answers.Add(new InterviewAnswer{id="first",applyHrReactionEmotion=true,hrReactionEmotion=HREmotion.Uncomfortable,portraitOnlyDuration=.8f});
            q.answers.Add(new InterviewAnswer{id="second",applyHrReactionEmotion=true,hrReactionEmotion=HREmotion.Disappointed});
            for(int extra=0;extra<i;extra++)q.answers.Add(new InterviewAnswer{id="extra"+extra,text="Additional authored answer"});
            if(i<2)a.fixedQuestions.Add(q);else{q.linkedChoice=choice;a.resumeLinkedQuestions.Add(q);}
        }
        var list=new ChoiceListUI{AutoAnswer=true};Set(controller,"answerList",list);Call(controller,"Awake");
        int finished=0;controller.Finished+=()=>finished++;int silentHolds=0;
        controller.Begin(ctx);
        waits=Run(controller.Pending,player,seconds=>{if(seconds==.8f){silentHolds++;Check(reaction.Current==HREmotion.Uncomfortable && !ui.Visible && list.VisibleCount==0,"Silent reaction did not persist with dialogue/old answers closed");}});
        Check(finished==1 && silentHolds==4 && ctx.Session.askedQuestionIds.Count==4 && list.Submissions==4,"Four-question interview failed to advance or double answer triggered extra reactions");
        list.DisplayedCounts.Sort();Check(string.Join(",",list.DisplayedCounts)=="2,3,4,5","Interview hardcoded answer count instead of authored data");
        Console.WriteLine("PASS actual response/interview coroutines: silent hold without dialogue/input, shared HR switch, spoken multipage, legacy empty blocks, opt-in pacing, gender filtering, phase/fallback, next Neutral, non-HR preservation, four questions and rapid duplicate submission guards. No native Play Mode timing/rendering claim.");
    }
}
namespace FirstJobber
{
    public enum ChoiceState{Available}
    public struct ChoiceItem{public string id,title;public ChoiceState state;}
    public class ChoiceListUI
    {
        public bool AutoAnswer;public int Submissions;public event Action<string> ItemChosen;List<ChoiceItem> items;
        public List<int> DisplayedCounts=new List<int>();public int VisibleCount=>items==null?0:items.Count;
        public void Clear(){items=null;}public void SetItems(List<ChoiceItem> values,string empty="",bool reset=false){items=new List<ChoiceItem>(values);DisplayedCounts.Add(items.Count);}
        public void SetInteractable(bool value){if(value&&AutoAnswer){Submissions++;ItemChosen?.Invoke(items[0].id);ItemChosen?.Invoke(items[1].id);}}
    }
    public class ChoiceManager{public List<ResumeChoiceData> Selected=new List<ResumeChoiceData>();public void GetSelectedChoices(List<ResumeChoiceData> result){result.Clear();result.AddRange(Selected);}}
    public class GameDatabase{public DialogueLine[] defaultHrIntro,hrReviewLines;public DialoguePlaybackMode defaultHrIntroMode,hrReviewMode;public int questionsPerInterview=4;}
    public class GameContext{public CompanyData SelectedCompany;public HRCharacterData SelectedHr=>SelectedCompany.hr;public DialogueManager Dialogue;public GameSession Session;public ChoiceManager Choices;public GameDatabase Database;}
}
