// Controller regression tests with UI/Unity doubles, not Unity Play Mode.
using System;
using System.Collections.Generic;
using System.Reflection;
using FirstJobber;
class Harness
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Set(object o, string field, object value) => o.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(o, value);
    static void Invoke(object o, string method) => o.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(o, null);
    static void Main()
    {
        var db = Fixture.Create(); var session = new GameSession { day = 2 };
        var dialogue = new DialogueManager(); var list = new ChoiceListUI();
        var events = new DailyEventManager(); Set(events,"database",db); Set(events,"dialogue",dialogue); Set(events,"choiceList",list);
        foreach (var ev in db.events)
        {
            var isolated = new GameSession { day = ev.exactDay };
            Check(events.HasEligible(ev.trigger, isolated),"Missing trigger for " + ev.id);
            events.Run(ev.trigger,isolated,()=>{});
            Check(isolated.triggeredEvents.Contains(ev.id),"Incorrect day or trigger: " + ev.id);
            while(dialogue.Next!=null) dialogue.Complete();
            if(list.gameObject.active) { list.Click(list.Items[0].id); while(dialogue.Next!=null) dialogue.Complete(); }
            Check(!events.HasEligible(ev.trigger, isolated),"Completed group replay: " + ev.id);
        }
        Console.WriteLine("PASS all 13 authored groups eligible on their assigned day/trigger, and excluded after completion");
        int done = 0;
        events.Run(EventTrigger.BeforeResume,session,()=>done++);
        Check(done==0 && dialogue.Next!=null,"Event must await dialogue");
        for(int i=0;i<5;i++) dialogue.Complete();
        Check(list.gameObject.active && list.Items.Count==2 && dialogue.Next==null && done==0,"Phone popup must stop sequence");
        list.Click("invalid"); Check(done==0,"Invalid phone choice");
        list.Click("later"); list.Click("later");
        Check(session.flags.Count==1 && !list.gameObject.active,"Phone choice must be recorded exactly once");
        while(dialogue.Next!=null) dialogue.Complete();
        Check(done==1,"Phone common outro must complete once");
        events.Run(EventTrigger.BeforeResume,session,()=>done++);
        Check(done==2 && dialogue.Next==null,"Refresh must not replay event");
        session.day=4;
        events.Run(EventTrigger.BeforeResume,session,()=>done++);
        while(dialogue.Next!=null) dialogue.Complete();
        Check(list.Items[1].id=="own_path" && list.gameObject.active,"Day4 father choices");
        list.Click("own_path"); while(dialogue.Next!=null) dialogue.Complete();
        Check(done==3 && session.flags.Count==2,"Day4 common outro");
        var final = new FinalDecisionController(); var endings = new ChoiceListUI();
        Set(final,"endingList",endings); Invoke(final,"Awake");
        var ctx = new GameContext { Database=db,Dialogue=dialogue }; int fired=0; string endingId=null;
        final.EndingChosen += id=>{ fired++; endingId=id; };
        final.Open(ctx); Check(endings.Items.Count==3,"Three endings");
        endings.Click("ending_civil_service"); endings.Click("confirm");
        Check(fired==0 && !endings.gameObject.active,"Preview cannot execute ending");
        dialogue.Complete(); Check(endings.Items[0].id=="confirm" && endings.Items[1].id=="back","Separate confirmation controls");
        endings.Click("back"); Check(fired==0 && endings.Items.Count==3,"Back has no ending side effect");
        endings.Click("ending_continue_applying"); dialogue.Complete(); endings.Click("confirm"); endings.Click("confirm");
        Check(fired==1 && endingId=="ending_continue_applying","Confirm exactly once for matching preview");
        Console.WriteLine("PASS Day2/4 phone pause, shared outro, invalid/double-click guards, once-only events; Day6 preview/back/change/confirm guards");
    }
}
namespace UnityEngine
{
    public class Object {}
    public class MonoBehaviour : Object {}
    public class GameObject { public bool active; public void SetActive(bool v) {active=v;} }
    public class SerializeField : Attribute {}
}
namespace TMPro { public class TMP_Text { public string text; } }
namespace FirstJobber
{
    public enum EventTrigger { OnDayStart, BeforeResume, AfterResumeConfirmed, AfterCompanySelected, BeforeInterview, AfterInterview, AfterRejection, BeforeSleep, OnEnterResume, OnEnterCompanySelection }
    public enum DialoguePlaybackMode { Sequential, RandomOne }
    public enum ChoiceState { Available }
    public struct ChoiceItem { public string id,title,subtitle; public ChoiceState state; }
    public class DialogueLine { public string text; }
    public class DailyEventOption {public string id,label;}
    public class DailyEventSegment {public string id; public DialogueLine[] lines; public DailyEventOption[] options;}
    public class DailyEventData {public string id,requiredFlag,forbiddenFlag,caption;public string[] setFlags;public int exactDay,minDay,priority;public bool repeatable;public EventTrigger trigger;public float flickerSeconds;public object background;public DialogueLine[] lines;public DailyEventSegment[] segments;}
    public class EndingData {public string id,optionLabel,optionDescription;public DialogueLine[] previewLines;}
    public class GameDatabase { public int finalStoryDay=6;public List<DailyEventData> events=new List<DailyEventData>();public List<EndingData> endings=new List<EndingData>();public DialogueLine[] fatherOfferLines;public DialoguePlaybackMode fatherOfferMode;public EndingData FindEnding(string id)=>endings.Find(e=>e.id==id); }
    public class GameSession {public int day;public List<string> triggeredEvents=new List<string>(),flags=new List<string>();public void SetFlag(string f){if(!flags.Contains(f))flags.Add(f);}public bool HasFlag(string f)=>flags.Contains(f);public void Save(){} }
    public class GameContext { public GameDatabase Database; public DialogueManager Dialogue; }
    public class DialogueManager { public Action Next;public void Play(DialogueLine[] lines,Action done,float delay=0,DialoguePlaybackMode mode=DialoguePlaybackMode.Sequential) { Next=done; } public void Complete(){var next=Next;Next=null;next?.Invoke();} }
    public class ChoiceListUI {public UnityEngine.GameObject gameObject=new UnityEngine.GameObject();public event Action<string> ItemChosen;public List<ChoiceItem> Items=new List<ChoiceItem>();bool enabled=true;public void SetItems(IList<ChoiceItem> items,string emptyMessage=null,bool resetScroll=false){Items=new List<ChoiceItem>(items);}public void Clear(){Items.Clear();}public void SetInteractable(bool value){enabled=value;}public void Click(string id){if(enabled)ItemChosen?.Invoke(id);} }
    public class DailyEventPanelUI { public void Clear(){} public void Show(string caption,object background){} }
    public class AtmosphereManager {public static AtmosphereManager Instance;public void PulseFlicker(float f){} }
    public static class LocalizationManager {public static string Get(object owner,string field,string english)=>english;public static string Get(string key,string english)=>english;}
    public class Label {public string Text="";}
    public static class UiText {public static Label WhatWillYouDo=new Label(),NoEndings=new Label(),ConfirmDecision=new Label(),BackToDecisions=new Label();}
}
