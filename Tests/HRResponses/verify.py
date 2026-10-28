"""Production answer-response audit and actual response coroutine tests. Does not run Unity Play Mode."""
from pathlib import Path
import collections,re,subprocess,yaml
R=Path(__file__).resolve().parents[2]
def asset(path):
 text=path.read_text(encoding='utf-8-sig');text=re.sub(r'^%.*\n|^--- !u!.*\n','',text,flags=re.M)
 return yaml.safe_load(text)['MonoBehaviour']
def guid(path):return re.search(r'^guid: (\w+)',Path(str(path)+'.meta').read_text(),re.M)[1]
db=asset(R/'Assets/Data/Production/GameDatabase_Production.asset')
hrpaths={guid(p):p for p in (R/'Assets/Data/Production/HR').glob('*.asset')}
active=set()
for ref in db['hrCharacters']:
 hr=asset(hrpaths[ref['guid']])
 assert [slot['type'] for slot in hr['sequence']]==[0,0,1,1]
 for ref in hr['fixedQuestions']+hr['resumeLinkedQuestions']:active.add(ref['guid'])
assert active=={guid(p) for p in (R/'Assets/Data/Production/Interview').rglob('*.asset')},'Audit must cover the actual active HR question pools'
counts=collections.Counter()
for p in (R/'Assets/Data/Production/Interview').rglob('*.asset'):
 for answer in asset(p).get('answers',[]):
  counts['answers']+=1
  if answer.get('applyHrReactionEmotion'):counts['answer_level_reactions']+=1
target=R/'Assets/Data/Production/Interview/FixedQuestions/Q_P_prod_hr_appfair_fixed_03.asset'
q=asset(target);answer=next(a for a in q['answers'] if a['id']=='a1')
assert answer['applyHrReactionEmotion']==1 and answer['hrReactionEmotion']==2 and answer['portraitOnlyDuration']==.8
assert not (answer['hrResponse'][0].get('speaker') or '').strip() and answer['hrResponse'][0]['emotion']==2
backup=R/'.utmp/hr-response-before'/target.name
if backup.exists():
 before=asset(backup);after=asset(target)
 for a in after['answers']:
  for field in ['applyHrReactionEmotion','hrReactionEmotion','portraitOnlyDuration']:a.pop(field,None)
 assert before==after,'Existing content, legacy response, emotion, timing or associations changed'
 assert Path(str(backup)+'.meta').read_bytes()==Path(str(target)+'.meta').read_bytes(),'Question GUID changed'
print('PASS Production audit:',dict(counts),'; Appfair fixed03/a1 Disappointed migrated with original response block/data/GUID preserved')
temp=R/'Temp/HRResponseTests';temp.mkdir(parents=True,exist_ok=True)
base=(R/'Tests/HRPortraits/RuntimeHarness.cs').read_text(encoding='utf-8');support=base[base.index('namespace UnityEngine'):]
support='using System; using System.Collections.Generic; using UnityEngine;\n'+support
support=support.replace('public static void LogWarning(string s,Object c=null){Warnings++;}','public static void LogWarning(string s,Object c=null){Warnings++;}public static void LogError(string s,Object c=null){throw new Exception(s);}')
support=support.replace('public class WaitForSecondsRealtime{public WaitForSecondsRealtime(float n){}}','public class WaitForSecondsRealtime{public float Seconds;public WaitForSecondsRealtime(float n){Seconds=n;}}')
support=support.replace('public static int Min(int a,int b)=>Math.Min(a,b);','public static int Min(int a,int b)=>Math.Min(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);')
support=support.replace('public class DialogueUI {public event Action Clicked;','public class DialogueUI {public bool Visible;public int Shows;public event Action Clicked;')
support=support.replace('public void Show(){}public void Hide(){}','public void Show(){Shows++;Visible=true;}public void Hide(){Visible=false;}')
support=support.replace('public static string Get(string k,string en)=>en;','public static string Get(string k,string en)=>en;public static string Get(ScriptableObject owner,string k,string en)=>en;')
support=support.replace('public static string SpeakerKey(string s)=>"speaker/"+s;','public static string SpeakerKey(string s)=>"speaker/"+s;public static string Answer(InterviewQuestionData q,InterviewAnswer a,int i,string field)=>"question/"+q.id+"/answers/"+a.id+"/"+field;')
support=support.replace('public ResumeCategoryData category;','public DialogueLine[] hrReactions;public ResumeCategoryData category;')
support=support.replace('public HRCharacterData hr;','public DialogueLine[] hrIntro;public HRCharacterData hr;')
support=support.replace('public class GameSession{public List<string> askedQuestionIds','public class GameSession{public int interviewIndex;public List<string> interviewQuestionIds=new List<string>();public bool HasFlag(string f)=>false;public List<string> askedQuestionIds')
support=support.replace('public static InterviewQuestionData GetEmergencyQuestion()=>new InterviewQuestionData();','public static InterviewQuestionData GetEmergencyQuestion()=>new InterviewQuestionData();public static void Pick(GameDatabase db,GameSession session,IList<ResumeChoiceData> selected,CompanyData company,int count,List<InterviewQuestionData> result){}')
(temp/'Support.cs').write_text(support,encoding='utf-8')
data=Path('C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data');mono=data/'MonoBleedingEdge/bin/mono.exe';out=temp/'RuntimeHarness.exe'
files=['Data/DialogueData.cs','Data/HRCharacterData.cs','Data/InterviewQuestionData.cs','Dialogue/DialogueManager.cs','Interview/HRReactionController.cs','Interview/HrInterviewPlanner.cs','Interview/InterviewController.cs','Core/GamePhase.cs']
subprocess.run([str(mono),str(data/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/langversion:9','/nowarn:0649,0067','/out:'+str(out),str(temp/'Support.cs'),str(Path(__file__).with_name('RuntimeHarness.cs')),*[str(R/'Assets/Scripts'/p) for p in files]],check=True)
subprocess.run([str(mono),str(out)],check=True)
