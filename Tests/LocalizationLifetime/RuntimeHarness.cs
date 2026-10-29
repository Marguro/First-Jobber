using System;
using System.Collections.Generic;
using System.Reflection;
using FirstJobber;
using UnityEngine;
class DestroyedLocalizationTests {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Main(){
 var dead=new DailyEventData{id=null,name="lost_native_event"};dead.DestroyForTest();
 Check(!ReferenceEquals(dead,null)&&dead==null&&dead is DailyEventData,"Test must reproduce Unity fake-null + C# pattern match");
 Check(LocalizationKeys.Prefix(dead)==""&&LocalizationKeys.Of(dead,"caption")=="","Destroyed wrapper generated a key");
 Check((string)typeof(LocalizationKeys).GetMethod("IdOr",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{null,dead})=="","IdOr accessed destroyed name");
 int emitted=0;LocalizationKeys.VisitAsset(dead,(k,v)=>emitted++);Check(emitted==0&&dead.NameReads==0,"Destroyed object reached localization visitor/name getter");
 Check(LocalizationKeys.Of(new DailyEventData{name="legacy_event"},"caption")=="event/legacy_event/caption","Valid legacy name fallback changed");
 var db=ProductionEventFixture.Create();db.events.Insert(2,dead);db.events.Add(null);var actual=new Dictionary<string,string>();
 for(int pass=0;pass<10;pass++)LocalizationKeys.Visit(db,(key,text)=>{Check(!string.IsNullOrEmpty(key)&&!key.StartsWith("/"),"Malformed key");actual[key]=text;});
 foreach(var expected in ProductionEventFixture.Expected())Check(actual.TryGetValue(expected.Key,out var en)&&en==expected.Value,"Valid Production event missed or changed: "+expected.Key);
 int live=0;foreach(var e in db.events){if(e==null)continue;live++;foreach(var seg in e.segments)foreach(var line in seg.lines??new DialogueLine[0])Check(line.locKey.StartsWith("event/"+e.id+"/segments/"+seg.id+"/lines/"),"Event pages lost keys");}
 Check(live==13&&dead.NameReads==0,"Valid events were skipped or destroyed name read");
 Check(Debug.Warnings.Exists(w=>w.Contains("Production.events[2]")&&w.Contains("destroyed")),"Invalid entry lacks database/index diagnostic");
 var q=new InterviewQuestionData();q.DestroyForTest();Check(LocalizationKeys.Answer(q,new InterviewAnswer{id="answer"},0,"text")=="","Destroyed question generated malformed answer key");
 Console.WriteLine("PASS real LocalizationKeys: destroyed Unity wrappers rejected before name/ID dispatch; no malformed keys; contextual diagnostics; all Production event pages/options retain stable keys over ten initialization passes (API doubles).");
 }
}
