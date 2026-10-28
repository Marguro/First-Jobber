"""Production HR visual ownership audit + actual C# resolver/dialogue/planner tests (not Play Mode)."""
from pathlib import Path
import json,re,subprocess,yaml
ROOT=Path(__file__).resolve().parents[2]
def docs(path):
 text=path.read_text(encoding='utf-8-sig');ids=re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M)
 text=re.sub(r'^%.*\n','',text,flags=re.M);text=re.sub(r'^--- !u!\d+ &-?\d+.*$','---',text,flags=re.M)
 return dict(zip(map(int,ids),yaml.safe_load_all(text)))
def asset(path):return docs(path)[11400000]['MonoBehaviour']
def guid(path):return re.search(r'^guid: (\w+)',Path(str(path)+'.meta').read_text(),re.M)[1]
dbpath=ROOT/'Assets/Data/Production/GameDatabase_Production.asset';db=asset(dbpath)
paths={guid(p):p for p in (ROOT/'Assets/Data/Production').rglob('*.asset')}
hrs={r['guid']:asset(paths[r['guid']]) for r in db['hrCharacters']}
assert len(hrs)==5
report=[]
for ref in db['companies']:
 company=asset(paths[ref['guid']]);assert company['hr']['guid'] in hrs
 hr=hrs[company['hr']['guid']];p=paths[company['hr']['guid']]
 assert hr['m_Script']['guid']==guid(ROOT/'Assets/Scripts/Data/HRCharacterData.cs')
 assert [row['emotion'] for row in hr['emotionVisuals']]==[0,1,2,3]
 assert [s['type'] for s in hr['sequence']]==[0,0,1,1]
 assert len(hr['fixedQuestions'])>=2 and len(hr['resumeLinkedQuestions'])>=2
 backup=ROOT/'.utmp/hr-portrait-before'/p.name
 if backup.exists():
  before=asset(backup)
  assert before['id']==hr['id'],'Stable HR ID changed'
  assert guid(backup)==guid(p),'HR GUID changed'
 report.append(dict(asset=str(p.relative_to(ROOT)),missing_emotions=[v['emotion'] for v in hr['emotionVisuals'] if not v['sprite']['fileID']],default_assigned=bool(hr['defaultPortrait']['fileID'])))
direct=[]
def walk(node,where):
 if isinstance(node,dict):
  if node.get('speaker')=='HR' and node.get('portrait',{}).get('fileID'):direct.append(where)
  for key,value in node.items():walk(value,where+'/'+key)
 elif isinstance(node,list):
  for i,value in enumerate(node):walk(value,where+'/'+str(i))
for p in (ROOT/'Assets/Data/Production/Interview').rglob('*.asset'):walk(asset(p),str(p.relative_to(ROOT)))
scene=docs(ROOT/'Assets/Scenes/Game.unity');flow=scene[217966938]['MonoBehaviour'];interview=scene[flow['interview']['fileID']]['MonoBehaviour']
assert flow['database']['guid']==guid(dbpath)
reaction=scene[interview['hr']['fileID']]['MonoBehaviour'];assert reaction['dialogue']==flow['dialogue']
assert reaction['portrait']['fileID'] in scene
portrait_go=scene[scene[reaction['portrait']['fileID']]['MonoBehaviour']['m_GameObject']['fileID']]['GameObject']
assert not any('AtmosphereSpriteBinding' in (scene[c['component']['fileID']].get('MonoBehaviour',{}).get('m_EditorClassIdentifier') or '') for c in portrait_go['m_Component']), 'A global sprite binding competes with HRCharacterData'
flowcode=(ROOT/'Assets/Scripts/Core/GameFlowManager.cs').read_text()
assert 'dialogue.HrCharacterProvider =' in flowcode and 'return company != null ? company.hr : null;' in flowcode
runtime=(ROOT/'Assets/Scripts/Dialogue/DialogueManager.cs').read_text();assert 'ResolvePortrait(line)' in runtime
assert 'ResolveHrSprite' not in (ROOT/'Assets/Scripts/Interview/HRReactionController.cs').read_text()
temp=ROOT/'Temp/HRPortraitTests';temp.mkdir(parents=True,exist_ok=True)
(temp/'production-audit.json').write_text(json.dumps(dict(hrs=report,legacy_hr_portrait_fields=direct),ensure_ascii=False,indent=2),encoding='utf-8')
print('PASS five existing Company -> HR references, Inspector emotion slots, original HR data/GUIDs, active scene dialogue/portrait connection, and 2 Fixed + 2 Linked configuration')
print('Production question HR portrait overrides found:',len(direct),'(preserved, never required as fallback). Missing art:',sum(len(h['missing_emotions']) for h in report),'emotion slots; defaults:',sum(not h['default_assigned'] for h in report))
data=Path('C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data');mono=data/'MonoBleedingEdge/bin/mono.exe';out=temp/'RuntimeHarness.exe'
files=['Data/DialogueData.cs','Data/HRCharacterData.cs','Data/InterviewQuestionData.cs','Dialogue/DialogueManager.cs','Interview/HRReactionController.cs','Interview/HrInterviewPlanner.cs']
subprocess.run([str(mono),str(data/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/langversion:9','/nowarn:0649,0067','/out:'+str(out),str(Path(__file__).with_name('RuntimeHarness.cs')),*[str(ROOT/'Assets/Scripts'/p) for p in files]],check=True)
subprocess.run([str(mono),str(out)],check=True)
