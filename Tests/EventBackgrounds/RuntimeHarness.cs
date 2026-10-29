using System;using System.Reflection;using FirstJobber;using UnityEngine;using UnityEngine.UI;using TMPro;
class BackgroundTests {
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
 static void Check(bool b,string why){if(!b)throw new Exception(why);}
 static void Main(){
 var db=Fixture.Create();var manager=new DailyEventManager();var dialogue=new DialogueManager();var choices=new ChoiceListUI();var view=new DailyEventPanelUI();var image=new Image();var group=new CanvasGroup();var panels=new UIManager();panels.CurrentPanel=panels.DailyPanel;
 Set(view,"background",image);Set(view,"presentationGroup",group);Set(view,"captionText",new TMP_Text());Set(view,"panels",panels);Set(manager,"database",db);Set(manager,"dialogue",dialogue);Set(manager,"choiceList",choices);Set(manager,"eventPanel",view);
 foreach(string id in new[]{"event_day01_opening","event_day02_before_resume","event_day04_before_resume","event_day06_final_story"}){
 var ev=db.events.Find(e=>e.id==id);var art=new Sprite{name=id};ev.background=art;panels.CurrentPanel=id=="event_day06_final_story"?new CanvasGroup():panels.DailyPanel;
 bool done=false;DialogueManager.BeforePlay=()=>Check(image.sprite==art&&image.enabled&&group.alpha==1,"Background must precede every dialogue: "+id);
 manager.Run(ev.trigger,new GameSession{day=ev.exactDay},()=>{Check(group.alpha==0&&image.sprite==null,"Cleanup must precede navigation/next event");done=true;});
 int pages=0;while(!done&&pages++<100){if(dialogue.Next!=null)dialogue.Complete();else if(manager.IsWaitingForChoice){Check(image.sprite==art&&group.alpha==1,"Phone choices lost event background");choices.Click(choices.Items[0].id);}else throw new Exception("Stuck event");}
 Check(done&&!group.blocksRaycasts&&!image.raycastTarget,"Event incomplete or background intercepts clicks");
 }
 DialogueManager.BeforePlay=null;
 panels.CurrentPanel=panels.DailyPanel;view.Show("",null);Check(group.alpha==1,"Daily-screen black fallback changed");
 panels.CurrentPanel=new CanvasGroup();view.Show("",null);Check(group.alpha==0,"A no-background Resume reaction must not cover the Resume Editor");
 view.Show("",null,true);Check(group.alpha==1,"OnDayStart no-art fallback changed");
 for(int i=0;i<40;i++){var first=new Sprite();var next=new Sprite();view.Show("",first);view.Clear();view.Show("",next);Check(image.sprite==next&&group.alpha==1,"Sequential event reused stale art");}
 view.Clear();Check(group.alpha==0&&image.sprite==null,"Final cleanup left background visible");
 Console.WriteLine("PASS real DailyEventManager + DailyEventPanelUI: all four triggers render before dialogue, Day6 independent visibility, phone choices retain image, cleanup before callback, fallback policy and 40 sequential reopen cycles (API doubles; not Play Mode).");
 }
}
