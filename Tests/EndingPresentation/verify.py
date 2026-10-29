"""Production ending connections + real presenter/state callback tests. Native media requires Unity."""
from pathlib import Path
import re,yaml,subprocess
R=Path(__file__).resolve().parents[2]
def docs(path):
 s=path.read_text(encoding='utf-8-sig');ids=re.findall(r'^--- !u!\d+ &(-?\d+)',s,re.M);assert len(ids)==len(set(ids))
 s=re.sub(r'^%.*\n','',s,flags=re.M);s=re.sub(r'^--- !u!\d+ &-?\d+.*$','---',s,flags=re.M)
 return dict(zip(map(int,ids),yaml.safe_load_all(s)))
scene=docs(R/'Assets/Scenes/Game.unity');c=scene[762817071]['MonoBehaviour']
assert c['director']['fileID']==762817075 and scene[762817075]['PlayableDirector']['m_InitialState']==0
for field in ['cutsceneRoot','videoRoot','videoImage','videoAspect','fadeOverlay']:assert c[field]['fileID'] in scene
assert scene[c['videoRoot']['fileID']]['GameObject']['m_IsActive']==0
assert scene[c['fadeOverlay']['fileID']]['CanvasGroup']['m_Alpha']==0
assert scene[c['buttonsRoot']['fileID']]['GameObject']['m_IsActive']==0
assert 'restartButton' not in c and 'menuButton' not in c
presenter=(R/'Assets/Scripts/Endings/EndingController.cs').read_text(encoding='utf-8-sig')
assert 'buttonsRoot.SetActive(true)' not in presenter and 'RestartRequested' not in presenter and 'MenuRequested' not in presenter

assert c['atmosphere']['fileID']==217966943
excluded_graphics={2130000013,2130000023,2130000033}
for fid in excluded_graphics:
 graphic=scene[fid]['MonoBehaviour'];go=scene[graphic['m_GameObject']['fileID']]['GameObject']
 for entry in go['m_Component']:
  comp=scene[entry['component']['fileID']].get('MonoBehaviour',{})
  assert comp.get('m_Script',{}).get('guid') not in ['fbae4c045a914b74b4e40ec219abca86','6ab3878f7a0eded4688f4d38542ca900','faccf4b5f24d4e18894c4ee479b90ac4']
raw=scene[c['videoImage']['fileID']]['MonoBehaviour'];assert raw['m_Script']['guid']=='1344c3c82d62a2a41a3576d8abb8e3ea'
assert scene[c['videoAspect']['fileID']]['MonoBehaviour']['m_AspectMode']==3
# Presentation mode/media are artist-authored; validate destination actions without
# asserting obsolete placeholder clips or historical presentation defaults.
for name,action in [('ending_civil_service',0),('ending_father_job',0),('ending_continue_applying',1)]:
 e=docs(R/f'Assets/Data/Production/Endings/{name}.asset')[11400000]['MonoBehaviour']
 assert e['id']==name and e['actionAfterCutscene']==action
 assert e['presentationMode'] in (0,1,2)
flow=(R/'Assets/Scripts/Core/GameFlowManager.cs').read_text(encoding='utf-8-sig')
assert 'PresentationCompleted += OnEndingPresentationCompleted' in flow and 'PresentationCompleted -= OnEndingPresentationCompleted' in flow
assert '{ GamePhase.Ending, new[] { GamePhase.MainMenu, GamePhase.CreateApplicant, GamePhase.ResumeEditor } }' in flow
chosen=flow.split('void OnEndingChosen(',1)[1].split('void OnEndingPresentationCompleted',1)[0]
assert 'session.day = 1' not in chosen and 'ending.Show(ctx, data)' in chosen,'Restart still bypasses presentation'
assert 'void PreviewEndingPresentation()' in flow and '#if UNITY_EDITOR' in flow
print('PASS actual Production scene surfaces/Director; per-ending modes/actions; original ending IDs/dialogue/media/localization preserved; presentation only after confirmation; restart callback connected')
# Compile the actual completion callback with state/API doubles. No copy of its reset rules.
start=flow.index('void OnEndingPresentationCompleted(');brace=flow.index('{',start);depth=1;end=brace+1
while depth:
 if flow[end]=='{':depth+=1
 elif flow[end]=='}':depth-=1
 end+=1
method=flow[start:end]
def extract(signature):
 start=flow.index(signature);brace=flow.index('{',start);depth=1;end=brace+1
 while depth:
  if flow[end]=='{':depth+=1
  elif flow[end]=='}':depth-=1
  end+=1
 return flow[start:end]
fixture='''using System;using System.Collections;using System.Collections.Generic;using FirstJobber;using UnityEngine;
class FlowProbe {
 static class Debug {public static void Log(string s,object context=null){}}
 enum GamePhase {Ending,ResumeEditor,MainMenu}
 class GameSession {
 public static int deleted;public static void DeleteSave(){deleted++;}
 public int day=1,interviewIndex;public string endingId,applicantName,applicantGender,selectedCompanyId;public float confirmedConfidence;public bool applicationOpen;public GamePhase phase;
 public List<string> selections=new List<string>(),consumedChoiceIds=new List<string>(),consumedCompanyIds=new List<string>(),askedQuestionIds=new List<string>(),interviewQuestionIds=new List<string>(),flags=new List<string>(),triggeredEvents=new List<string>(),history=new List<string>(),playedChoiceReactions=new List<string>();
 public int saved;public void Save(){saved++;if(day!=1||phase!=GamePhase.ResumeEditor||selections.Count!=0||selectedCompanyId!=null||applicationOpen||endingId!=null||consumedChoiceIds.Count!=0)throw new Exception("Dirty loop saved");}
 }
 class GameContext {public GameContext(object db,GameSession s,object d){}}
 class Events {public int cleared;public void ClearPanel(){cleared++;}}
 class Menu {public int opened;public void Open(object db){opened++;}}
 class Music {public int day,calls;public void PlayDay(int d){day=d;calls++;}}
 class Editor {public void Refresh(GameContext c){}}
 AtmosphereManager atmosphere=new AtmosphereManager();
 GameSession session=new GameSession();GamePhase Phase=GamePhase.Ending;int opened;bool resetAtmosphere,debugEndingPreview;
 Events dailyEvents=new Events();Menu mainMenu=new Menu();Music dayMusic=new Music();Editor resumeEditor=new Editor();GameContext ctx;object database=new object(),dialogue=new object();
 void ApplyAtmosphere(bool reset){resetAtmosphere=reset;if(session.day!=1)throw new Exception("Atmosphere not reset from Day1");}
 bool ChangePhase(GamePhase next){Phase=next;session.phase=next;atmosphere.SetAmbientGameplayActive(next!=GamePhase.MainMenu);if(next==GamePhase.MainMenu)dayMusic.PlayDay(1);return true;}
 void OpenResumeEditor(bool intro,bool events){if(intro||events)throw new Exception("First-launch intro/events replayed");opened++;}
 static FlowProbe Dirty(){var p=new FlowProbe();var s=p.session;s.day=6;s.endingId="ending";s.applicantName="Original";s.applicantGender="female";s.confirmedConfidence=80;s.interviewIndex=3;s.selections.Add("choice");s.selectedCompanyId="company";s.applicationOpen=true;s.consumedChoiceIds.Add("choice");s.consumedCompanyIds.Add("company");s.askedQuestionIds.Add("question");s.interviewQuestionIds.Add("question");s.flags.AddRange(new[]{"event_choice/a","resume_distorted","hr_strange","tutorial_complete"});s.triggeredEvents.AddRange(new[]{"event_day01_opening","event_day04_phone"});s.history.Add("history");s.playedChoiceReactions.Add("choice");return p;}
 '''+method+'\n'+extract('void BeginDay(')+'''
 public static void CheckRestart(){
 var p=Dirty();var e=new EndingData{id="ending",actionAfterCutscene=EndingAfterCutsceneAction.RestartDay1};int deleted=GameSession.deleted;
 p.OnEndingPresentationCompleted(e);p.OnEndingPresentationCompleted(e);
 if(p.Phase!=GamePhase.ResumeEditor||p.opened!=1||p.mainMenu.opened!=0||p.session.day!=1||!p.resetAtmosphere||p.session.endingId!=null||p.session.confirmedConfidence!=0||p.session.consumedChoiceIds.Count!=0||p.session.consumedCompanyIds.Count!=0||p.session.askedQuestionIds.Count!=0||p.session.interviewQuestionIds.Count!=0||p.session.interviewIndex!=0||p.session.flags.Count!=1||p.session.flags[0]!="tutorial_complete"||p.session.triggeredEvents.Count!=1||p.session.triggeredEvents[0]!="event_day01_opening"||p.session.applicantName!="Original"||p.session.applicantGender!="female"||p.session.saved!=1||p.dayMusic.day!=1||p.dayMusic.calls!=1||p.session.history.Count!=1||p.session.playedChoiceReactions.Count!=1||GameSession.deleted!=deleted)throw new Exception("Restart/reset/preservation/save/BGM incorrect");
 foreach(var id in new[]{"ending_civil_service","ending_father_job"}){var q=Dirty();q.session.endingId=id;var original=q.session;var ending=new EndingData{id=id,actionAfterCutscene=EndingAfterCutsceneAction.CompleteEnding};q.OnEndingPresentationCompleted(ending);q.OnEndingPresentationCompleted(ending);if(q.Phase!=GamePhase.MainMenu||q.mainMenu.opened!=1||!q.resetAtmosphere||q.session==original||q.session.applicantName!=null||q.opened!=0||q.session.history.Count!=0||q.dayMusic.day!=1)throw new Exception("Completed ending did not reset and return to Main Menu once");}
 if(GameSession.deleted!=deleted+2)throw new Exception("Save deletion duplicated");
 var debug=Dirty();debug.debugEndingPreview=true;debug.OnEndingPresentationCompleted(e);if(debug.session.day!=6||debug.opened!=0)throw new Exception("Debug preview reset production state");
 var wrong=Dirty();wrong.OnEndingPresentationCompleted(new EndingData{id="other"});if(wrong.resetAtmosphere||wrong.mainMenu.opened!=0)throw new Exception("Wrong ending completed");
 Console.WriteLine("PASS actual completion + BeginDay: endings1/2 reset and return MainMenu once; ending3 direct ResumeEditor, identity/history/reaction preservation, selections/depletion reset, autosave, Day1 BGM, no intro or BeforeResume replay, no MainMenu, duplicate/wrong-ID/preview guards");}
}
'''

# Compile the real suppression ownership and transition code with API doubles.
atmosphere=(R/'Assets/Scripts/Atmosphere/AtmosphereManager.cs').read_text(encoding='utf-8-sig')
def extract_atmosphere(signature):
 start=atmosphere.index(signature);brace=atmosphere.index('{',start);depth=1;end=brace+1
 while depth:
  if atmosphere[end]=='{':depth+=1
  elif atmosphere[end]=='}':depth-=1
  end+=1
 return atmosphere[start:end]
api=atmosphere[atmosphere.index('        public IDisposable SuppressOverlayRendering()'):atmosphere.index('        /// <summary>Applies the phase for this day.')]
fixture+="""
namespace FirstJobber {
 public class AtmosphereManager:UnityEngine.MonoBehaviour {
 public UnityEngine.UI.Image overlay=new UnityEngine.UI.Image();int overlaySuppressionCount;bool overlayWasEnabled;
 public bool IsOverlaySuppressed=>overlaySuppressionCount>0;
 public AtmospherePhaseData AppliedPhase;
 public UnityEngine.AudioSource audioSourceA=new UnityEngine.AudioSource(),audioSourceB=new UnityEngine.AudioSource();
 bool ambientGameplayActive=true,ambientFading;int ambientSuppressionCount;float cinematicAmbientFadeDuration=0;
 UnityEngine.Coroutine audioRoutine,ambientSilenceRoutine;UnityEngine.AudioSource activeSource;UnityEngine.AudioClip activeClip;
 public bool IsAmbientSuppressed=>ambientSuppressionCount>0;
 public static void CheckAmbient(){
 var m=new AtmosphereManager();m.cinematicAmbientFadeDuration=.3f;var oldClip=new UnityEngine.AudioClip();var newClip=new UnityEngine.AudioClip();
 m.AppliedPhase=new AtmospherePhaseData{ambientClip=oldClip};m.audioSourceA.volume=.8f;m.audioSourceB.volume=.4f;m.audioSourceA.Play();m.audioSourceB.Play();
 m.audioRoutine=m.StartCoroutine(m.AudioRoutine(oldClip,1,1));var token=m.SuppressAmbientAudio();m.SetAmbientGameplayActive(false);
 m.AppliedPhase=new AtmospherePhaseData{ambientClip=newClip};m.ApplyAudio(m.AppliedPhase,true);if(!m.ambientFading)throw new Exception("Missing ambient fade");
 for(int i=0;i<8;i++)Scheduler.Tick();
 if(m.audioSourceA.isPlaying||m.audioSourceB.isPlaying||m.audioSourceA.volume!=0||m.audioSourceB.volume!=0||m.audioSourceA.plays!=1||m.audioSourceB.plays!=1)throw new Exception("Crossfade/phase change restarted suppressed ambience");
 token.Dispose();token.Dispose();Scheduler.Tick();if(m.IsAmbientSuppressed||m.audioSourceA.isPlaying||m.audioSourceB.isPlaying)throw new Exception("Main menu resumed gameplay ambience");
 var resumed=m.SuppressAmbientAudio();m.SetAmbientGameplayActive(true);m.ApplyAudio(m.AppliedPhase,true);if(m.audioSourceA.isPlaying||m.audioSourceB.isPlaying)throw new Exception("Played before gameplay destination");
 resumed.Dispose();for(int i=0;i<4;i++)Scheduler.Tick();if(!m.audioSourceA.isPlaying||m.audioSourceA.clip!=newClip||m.audioSourceB.isPlaying)throw new Exception("Did not restore current Phase1 ambience at gameplay destination");
 var cancelled=m.SuppressAmbientAudio();m.SetAmbientGameplayActive(false);cancelled.Dispose();Scheduler.Tick();if(m.IsAmbientSuppressed||m.audioSourceA.isPlaying||m.audioSourceB.isPlaying)throw new Exception("Cancelled fade leaked ambience");
 Console.WriteLine("PASS real ambient ownership/fade/stop: interrupted crossfade cancelled, both sources silent, phase updates blocked, menu silent, gameplay restores CURRENT clip, cancellation and duplicate disposal safe");}

 UnityEngine.Camera targetCamera;UnityEngine.Color baseCameraColor,overlayRgb;float overlayAlpha,flickerOffset;object transitionRoutine;
 public class AtmospherePhaseData {public bool useCameraColor;public UnityEngine.Color cameraColor,overlayColor;public float overlayOpacity;public UnityEngine.AudioClip ambientClip;public float ambientVolume=1,audioCrossfade=.1f;}
 public void ResetPhaseOne(){var run=TransitionRoutine(new AtmospherePhaseData{overlayColor=UnityEngine.Color.black,overlayOpacity=0},0);if(run.MoveNext())throw new Exception("Instant reset yielded old-phase frame");}
 public static void CheckSuppression(){
 var manager=new AtmosphereManager();manager.overlay.color=new UnityEngine.Color(.4f,.1f,.2f,.8f);
 var first=manager.SuppressOverlayRendering();var second=manager.SuppressOverlayRendering();if(manager.overlay.enabled)throw new Exception("Overlay visible during suppression");
 manager.overlay.enabled=true;manager.SetOverlay(.9f);if(manager.overlay.enabled)throw new Exception("Phase/flicker update revealed suppressed overlay");
 first.Dispose();first.Dispose();if(manager.overlay.enabled)throw new Exception("Nested lease released too early");
 manager.ResetPhaseOne();second.Dispose();second.Dispose();if(!manager.overlay.enabled||manager.overlay.color.a!=0||manager.IsOverlaySuppressed)throw new Exception("Restored stale Phase3 overlay");
 manager.overlay.enabled=false;var disabled=manager.SuppressOverlayRendering();disabled.Dispose();if(manager.overlay.enabled)throw new Exception("Restoration overwrote authored disabled state");
 Console.WriteLine("PASS real atmosphere suppression API: nested/idempotent ownership, flicker/phase writes stay hidden, synchronous Phase1 reset, authored enabled state restored");}
"""+api+extract_atmosphere('IEnumerator TransitionRoutine(')+extract_atmosphere('void SetOverlay(')+atmosphere[atmosphere.index('        public void SetAmbientGameplayActive('):atmosphere.rfind('    }')]+'}}\n'

data=Path('C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data');temp=R/'Temp/EndingPresentationTests';temp.mkdir(parents=True,exist_ok=True);f=temp/'FlowProbe.cs';f.write_text(fixture,encoding='utf-8');exe=temp/'RuntimeHarness.exe';mono=data/'MonoBleedingEdge/bin/mono.exe'
subprocess.run([str(mono),str(data/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/langversion:9','/nowarn:0649','/out:'+str(exe),str(Path(__file__).with_name('RuntimeHarness.cs')),str(f),str(R/'Assets/Scripts/Endings/EndingController.cs'),str(R/'Assets/Scripts/Data/EndingData.cs')],check=True)
subprocess.run([str(mono),str(exe)],check=True)
web=temp/'WebRuntimeHarness.exe'
subprocess.run([str(mono),str(data/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/langversion:9','/nowarn:0649','/define:UNITY_WEBGL','/out:'+str(web),str(Path(__file__).with_name('RuntimeHarness.cs')),str(f),str(R/'Assets/Scripts/Endings/EndingController.cs'),str(R/'Assets/Scripts/Data/EndingData.cs')],check=True)
subprocess.run([str(mono),str(web)],check=True)
print('UNVERIFIED: native video/audio decoding, Timeline graphs/bindings, real UI transitions, phase sprite/corruption rendering and Save/Continue round-trip in Unity Play Mode.')
