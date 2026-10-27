"""Production startup references + real C# form/panel regression harness; not Play Mode."""
from pathlib import Path
import re,subprocess,sys,yaml
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tests/ProductionConfidence'))
from verify import load_documents,asset,guid
scene_path=ROOT/'Assets/Scenes/Game.unity';text=scene_path.read_text(encoding='utf-8')
ids=list(map(int,re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M)))
assert len(ids)==len(set(ids)),'Duplicate serialized object ID'
scene=dict(zip(ids,load_documents(scene_path)))
def component(fid):return scene[fid]['MonoBehaviour']
def gameobject(fid):return scene[fid]['GameObject']
flow=component(217966938);menu=component(flow['mainMenu']['fileID']);events=component(flow['dailyEvents']['fileID'])
dbpath=ROOT/'Assets/Data/Production/GameDatabase_Production.asset';db=asset(dbpath)
assert flow['database']['guid']==guid(dbpath)==events['database']['guid']
assert gameobject(flow['m_GameObject']['fileID'])['m_IsActive']==1 and flow['m_Enabled']==1
for field,script in [('mainMenu','UI/MainMenuController'),('createApplicant','UI/CreateApplicantController'),('resumeEditor','Resume/ResumeEditorController'),('dialogue','Dialogue/DialogueManager'),('ui','UI/UIManager'),('dailyEvents','Events/DailyEventManager')]:
 comp=component(flow[field]['fileID'])
 assert comp['m_Script']['guid']==guid(ROOT/'Assets/Scripts'/(script+'.cs')) and comp['m_Enabled']==1,field
for field,classname in [('nameInput','TMPro.TMP_InputField'),('genderDropdown','TMPro.TMP_Dropdown'),('submitButton','UnityEngine.UI.Button'),('errorText','TMPro.TextMeshProUGUI')]:
 comp=component(menu[field]['fileID']);assert classname in comp['m_EditorClassIdentifier'],field
 assert comp['m_Enabled']==1 and gameobject(comp['m_GameObject']['fileID'])['m_IsActive']==1,field
assert [g['id'] for g in db['genders']]==['male','female']
ui=component(flow['ui']['fileID']);inactive=[]
for field in ['mainMenu','createApplicant','resumeEditor','company','interview','dailyEvent','finalDecision','ending','overlay']:
 group=scene[ui[field]['fileID']]['CanvasGroup'];go=gameobject(group['m_GameObject']['fileID'])
 if not go['m_IsActive']:inactive.append(go['m_Name'])
dialogue=component(flow['dialogue']['fileID']);box=component(dialogue['ui']['fileID'])
for field in ['group','speakerText','bodyText','clickArea']:assert box[field]['fileID'] in scene,field
assert 'UnityEngine.UI.Button' in component(box['clickArea']['fileID'])['m_EditorClassIdentifier']
own_guids={guid(p):p for p in (ROOT/'Assets/Scripts').rglob('*.cs') if Path(str(p)+'.meta').exists()}
for d in scene.values():
 comp=d.get('MonoBehaviour',{})
 if comp.get('m_EditorClassIdentifier','').startswith('Assembly-CSharp::FirstJobber.'):
  assert comp['m_Script']['guid'] in own_guids,'Missing project MonoScript'
 # Existing wallpaper graphics are decorative, never input targets.
 if comp.get('m_EditorClassIdentifier','').endswith('.RawImage'):assert comp['m_RaycastTarget']==0
opening=ROOT/'Assets/Data/Production/DailyEvents/event_day01_opening.asset';ev=asset(opening)
assert any(ref['guid']==guid(opening) for ref in db['events'])
assert ev['exactDay']==1 and ev['trigger']==1 and not ev['repeatable']
assert len(ev['segments'][0]['lines'][0]['dialogueLines'])==5
assert any('InputSystemUIInputModule' in d.get('MonoBehaviour',{}).get('m_EditorClassIdentifier','') for d in scene.values())
print('PASS active Production database, applicant form references, male/female IDs, MonoScripts, Day1 authored event, Dialogue Continue, EventSystem and decorative RawImage raycasts')
print('Authored-inactive roots that UIManager must initialize:',', '.join(inactive))
temp=ROOT/'Temp/StartupFlowTests';temp.mkdir(parents=True,exist_ok=True)
data=Path('C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data');mono=data/'MonoBleedingEdge/bin/mono.exe';out=temp/'RuntimeHarness.exe'
scripts=['UI/UIManager.cs','UI/MainMenuController.cs','UI/CreateApplicantController.cs','Dialogue/DialogueUI.cs','Core/GamePhase.cs']
subprocess.run([str(mono),str(data/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/langversion:9','/nowarn:0649','/out:'+str(out),str(Path(__file__).with_name('RuntimeHarness.cs')),*[str(ROOT/'Assets/Scripts'/p) for p in scripts]],check=True)
subprocess.run([str(mono),str(out)],check=True)
print('UNVERIFIED: native Unity lifecycle/rendering/mouse clicks, actual save-file writes, full Day1 opening -> tutorial -> Resume flow')
