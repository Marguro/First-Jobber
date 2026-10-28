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
raw=scene[c['videoImage']['fileID']]['MonoBehaviour'];assert raw['m_Script']['guid']=='1344c3c82d62a2a41a3576d8abb8e3ea'
assert scene[c['videoAspect']['fileID']]['MonoBehaviour']['m_AspectMode']==3
backup=R/'.utmp/ending-presentation-before'
for name,mode,action in [('ending_civil_service',0,0),('ending_father_job',2,0),('ending_continue_applying',0,1)]:
 path=R/f'Assets/Data/Production/Endings/{name}.asset';e=docs(path)[11400000]['MonoBehaviour']
 assert e['presentationMode']==mode and e['actionAfterCutscene']==action and e['videoClip']['fileID']==0
 if (backup/path.name).exists():
  old=docs(backup/path.name)[11400000]['MonoBehaviour'];assert all(e[k]==v for k,v in old.items()),'Authored ending data changed'
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
fixture='''using System;using System.Collections.Generic;using FirstJobber;
class FlowProbe {
 enum GamePhase {Ending,ResumeEditor,MainMenu}
 class State {public int day=6,interviewIndex=3;public string endingId="ending",applicantName="Original",applicantGender="female";public float confirmedConfidence=80;
 public List<string> consumedChoiceIds=new List<string>{"choice"},consumedCompanyIds=new List<string>{"company"},askedQuestionIds=new List<string>{"question"},interviewQuestionIds=new List<string>{"question"},flags=new List<string>{"event_choice/a","resume_distorted","hr_strange","tutorial_complete"},triggeredEvents=new List<string>{"event_day01_opening","event_day04_phone"};}
 State session=new State();GamePhase Phase=GamePhase.Ending;int opened;bool resetAtmosphere,debugEndingPreview;
 void ApplyAtmosphere(bool reset){resetAtmosphere=reset;}bool ChangePhase(GamePhase next){Phase=next;return true;}void BeginDay(bool intro){if(intro)throw new Exception("Unexpected intro");opened++;}
 '''+method+'''
 public static void CheckRestart(){var p=new FlowProbe();var e=new EndingData{id="ending",actionAfterCutscene=EndingAfterCutsceneAction.RestartDay1};p.OnEndingPresentationCompleted(e);p.OnEndingPresentationCompleted(e);
 if(p.Phase!=GamePhase.ResumeEditor||p.opened!=1||p.session.day!=1||!p.resetAtmosphere||p.session.endingId!=null||p.session.confirmedConfidence!=0||p.session.consumedChoiceIds.Count!=0||p.session.consumedCompanyIds.Count!=0||p.session.askedQuestionIds.Count!=0||p.session.interviewQuestionIds.Count!=0||p.session.interviewIndex!=0||p.session.flags.Count!=1||p.session.flags[0]!="tutorial_complete"||p.session.triggeredEvents.Count!=1||p.session.triggeredEvents[0]!="event_day01_opening"||p.session.applicantName!="Original"||p.session.applicantGender!="female")throw new Exception("Actual restart callback did not preserve/reset the expected state");
 var debug=new FlowProbe{debugEndingPreview=true};debug.OnEndingPresentationCompleted(e);if(debug.session.day!=6||debug.opened!=0)throw new Exception("Debug preview executed a production restart");
 Console.WriteLine("PASS actual GameFlow completion callback: restart once, Day1 Resume phase, identity preserved, depletion/questions/atmosphere reset, established event/tutorial rules retained; debug preview does not reset");}
}
'''
data=Path('C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data');temp=R/'Temp/EndingPresentationTests';temp.mkdir(parents=True,exist_ok=True);f=temp/'FlowProbe.cs';f.write_text(fixture,encoding='utf-8');exe=temp/'RuntimeHarness.exe';mono=data/'MonoBleedingEdge/bin/mono.exe'
subprocess.run([str(mono),str(data/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/langversion:9','/nowarn:0649','/out:'+str(exe),str(Path(__file__).with_name('RuntimeHarness.cs')),str(f),str(R/'Assets/Scripts/Endings/EndingController.cs'),str(R/'Assets/Scripts/Data/EndingData.cs')],check=True)
subprocess.run([str(mono),str(exe)],check=True)
print('UNVERIFIED: native Unity VideoPlayer decoding/audio/frames, Timeline graph/bindings, fullscreen fades/raycast layering and complete Day6 confirmation → presentation → action in Play Mode.')
