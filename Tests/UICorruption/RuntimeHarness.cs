using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FirstJobber;
using DG.Tweening;
class CorruptionTests {
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
 static object Get(object o,string n)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
 static void Call(object o,string n)=>o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
 static void Check(bool b,string why){if(!b)throw new Exception(why);}
 static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
 static T Make<T>()where T:Component,new()=>new GameObject().Add(new T());
 static UICorruptionSurface Surface(UICorruptionSettings s,string id,ChoiceState state,out TMP_Text label){
  var surface=Make<UICorruptionSurface>();label=surface.gameObject.Add(new TMP_Text());label.text="มหาวิทยาลัยกากินัง บ้านหนองขึ่ม";label.font=new object();label.fontSharedMaterial=new object();label.rectTransform.anchoredPosition=new Vector2(13,47);
  Set(surface,"settings",s);Set(surface,"inkHost",surface.transform);Set(surface,"textSource",label);Call(surface,"OnEnable");surface.BindChoice(id,state);return surface;
 }
 static void Main(){
  var s=AuthoredCorruption.Create();var normal=s.phases[0];var phase2=s.phases[1];var phase3=s.phases[2];
  foreach(ChoiceState st in Enum.GetValues(typeof(ChoiceState)))Check(s.Severity(normal,st,UICorruptionRole.Choice,1)==0,"Phase1 clean all states");
  foreach(ChoiceState st in Enum.GetValues(typeof(ChoiceState)))Check(s.Severity(phase3,st,UICorruptionRole.Choice,1)>s.Severity(phase2,st,UICorruptionRole.Choice,1),"Phase3 stronger than Phase2");
  float avail=s.Severity(phase3,ChoiceState.Available,UICorruptionRole.Choice,1), selected=s.Severity(phase3,ChoiceState.Selected,UICorruptionRole.Choice,1),consumed=s.Severity(phase3,ChoiceState.Consumed,UICorruptionRole.Choice,1);
  Check(avail<selected && selected<consumed,"distinct Available/Selected/Consumed strengths");
  Check(s.Severity(phase3,ChoiceState.Disabled,UICorruptionRole.Choice,1)<consumed,"other Disabled is not Consumed");
  Check(s.Severity(phase3,ChoiceState.Available,UICorruptionRole.DialogueFrame,1)<avail,"essential dialogue protected");
  AtmosphereManager.Instance=new AtmosphereManager{AppliedPhase=normal.phase};TMP_Text source;var a=Surface(s,"choice-c1",ChoiceState.Consumed,out source);
  Check(a.CurrentSeverity==0 && Get(a,"ink")==null && Get(a,"ghost")==null,"Phase1 no generated effects");
  AtmosphereManager.Instance.AppliedPhase=phase2.phase;AtmosphereManager.Raise(phase2.phase);Check(a.CurrentSeverity>0 && Get(a,"ink")!=null && Get(a,"ghost")!=null,"existing phase event applies new layers immediately");
  var ink=(UIInkCorruptionGraphic)Get(a,"ink");var ghost=(TMP_Text)Get(a,"ghost");Check(!ink.raycastTarget&&!ghost.raycastTarget,"effects never intercept clicks");
  Check(object.ReferenceEquals(ghost.font,source.font)&&object.ReferenceEquals(ghost.fontSharedMaterial,source.fontSharedMaterial)&&ghost.text==source.text,"echo reuses exact localized text/font/material");
  uint stable=a.CurrentSeed;int allocated=GameObject.created;for(int i=0;i<40;i++){a.BindChoice("choice-c2",ChoiceState.Selected);a.BindChoice("choice-c1",ChoiceState.Consumed);}Check(a.CurrentSeed==stable&&allocated==GameObject.created,"pool/category rebind has stable marks and no object growth");
  source.text="Long English University Name";TMPro_EventManager.TEXT_CHANGED_EVENT.Call(source);Check(ghost.text==source.text,"English language updates echo without touching source");source.text="ศิลปกรรมศาสตร์";TMPro_EventManager.TEXT_CHANGED_EVENT.Call(source);Check(ghost.text==source.text,"Thai language updates echo");
  AtmosphereManager.Instance.AppliedPhase=phase3.phase;AtmosphereManager.Raise(phase3.phase);Check(a.CurrentSeverity==consumed,"Phase3 consumed severity");
  typeof(UICorruptionSurface).GetMethod("ResetStatics",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);Time.unscaledTime=100;a.TryBurst();var seq=(Sequence)Get(a,"burst");Check(seq!=null,"burst scheduled");seq.First();Check(!Near(ghost.rectTransform.anchoredPosition.x,((Vector2)Get(a,"restGhost")).x),"only ghost receives transient displacement");Check(Near(source.rectTransform.anchoredPosition.x,13)&&Near(source.rectTransform.anchoredPosition.y,47),"original hitbox/label remains fixed");
  TMP_Text second;var b=Surface(s,"choice-c3",ChoiceState.Consumed,out second);Check(Get(b,"burst")==null,"global budget prevents synchronized burst");
  s.reduceMotion=true;s.RefreshPresentation();Check(Get(a,"burst")==null&&Get(a,"schedule")==null&&Get(b,"schedule")==null,"reduced motion cancels burst and scheduling");Check(ghost.gameObject.activeSelf,"reduced motion retains static print treatment");Check(Near(ink.rectTransform.anchoredPosition.x,0),"cancel restores effect transform");
  s.reduceMotion=false;s.enableTransientGlitches=false;s.RefreshPresentation();Check(Get(a,"schedule")==null,"transient enable flag works");
  s.masterIntensity=0;s.RefreshPresentation();Check(!ink.gameObject.activeSelf&&!ghost.gameObject.activeSelf,"master zero restores clean state");s.masterIntensity=1;s.enableTransientGlitches=true;s.RefreshPresentation();Check(ink.gameObject.activeSelf,"settings change reapplies cached layers");
  Call(a,"OnDisable");Call(b,"OnDisable");DOTween.CompleteAll();Check(!ink.gameObject.activeSelf&&Get(a,"schedule")==null,"disable cleanup no stale callbacks");
  var drawing=Make<UIInkCorruptionGraphic>();var vh=new VertexHelper();drawing.Configure(stable,.15f,true,new Rect(.07f,.2f,.62f,.6f),s.inkColor);Draw(drawing,vh);int modest=vh.currentVertCount;drawing.Configure(stable,.8f,true,new Rect(.07f,.2f,.62f,.6f),s.inkColor);Draw(drawing,vh);Check(vh.currentVertCount>modest&&vh.currentVertCount<=16,"heavier ink uses bounded geometry");drawing.Configure(stable,0,true,new Rect(),s.inkColor);Draw(drawing,vh);Check(vh.currentVertCount==0,"no ink at zero strength");
  RemnantsProbe.Test(s);
  Console.WriteLine("PASS actual phase/state policy, stable pooled layers, localization/font/material echoes, global burst limit, reduced motion, interruption cleanup, fixed source RectTransforms, bounded ink mesh, remnant visibility vs real ChoiceManager availability/depletion (API doubles; not native Play Mode).");
 }
 static void Draw(UIInkCorruptionGraphic g,VertexHelper h)=>typeof(UIInkCorruptionGraphic).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,new object[]{h});
}
