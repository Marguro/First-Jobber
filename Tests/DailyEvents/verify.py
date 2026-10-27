"""Read-only serialized production audit and controller regression tests (not Play Mode)."""
from pathlib import Path
import collections,csv,json,re,subprocess,yaml
R=Path(__file__).resolve().parents[2]
def docs(p):
 s=p.read_text(encoding='utf-8-sig');ids=re.findall(r'^--- !u!\d+ &(-?\d+)',s,re.M)
 s=re.sub(r'^%.*\n','',s,flags=re.M);s=re.sub(r'^--- !u!\d+ &-?\d+.*$','---',s,flags=re.M)
 return dict(zip(map(int,ids),yaml.safe_load_all(s)))
def asset(p):return docs(p)[11400000]['MonoBehaviour']
def guid(p):return re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1]
source=json.loads((R/'ContentImport/DailyEvents/day_events_production_th.json').read_text(encoding='utf-8'))
dbpath=R/'Assets/Data/Production/GameDatabase_Production.asset';db=asset(dbpath)
table=asset(R/'Assets/Resources/Localization/LocalizationTable_TH.asset')['entries']
assert len({e['key'] for e in table})==len(table),'Duplicate localization keys'
entries={e['key']:e for e in table}
with (R/'Assets/Localization/translation_template.csv').open(encoding='utf-8-sig',newline='') as f:rows=list(csv.DictReader(f))
assert len({r['key'] for r in rows})==len(rows),'Duplicate CSV keys'
csvmap={r['key']:r for r in rows}
def localized(k,en,th):
 assert en and entries[k]['english']==en and entries[k]['thai']==th,k
 assert csvmap[k]['english']==en and csvmap[k]['thai']==th,k+' CSV'
events=[];count=0;cues=0
for src in source['events']:
 p=R/'Assets/Data/Production/DailyEvents'/ (src['id']+'.asset');ev=asset(p);events.append(ev)
 assert ev['id']==src['id'] and ev['exactDay']==src['day'] and not ev['repeatable']
 assert ev['trigger']=={'before_resume':1,'on_enter_resume':8,'on_enter_company_selection':9,'after_rejection_before_day_advance':6,'day_start_story_only':0}[src['trigger']]
 assert ev['m_Script']['guid']==guid(R/'Assets/Scripts/Data/DailyEventData.cs')
 assert len(ev['segments'])==len(src['segments'])
 for seg,auth in zip(ev['segments'],src['segments']):
  assert seg['id']==auth['id'];prefix='event/'+ev['id']+'/segments/'+seg['id']
  if auth['type']=='dialogue':
   line=seg['lines'][0];assert line['speaker']==auth['speaker'];assert len(line['dialogueLines'])==len(auth['lines_th'])
   for i,(en,th) in enumerate(zip(line['dialogueLines'],auth['lines_th'])):localized(prefix+'/lines/0/lines/'+str(i),en,th);count+=1
  elif auth['type']=='choice_popup':
   assert [o['id'] for o in seg['options']]==[o['id'] for o in auth['options']]
   for op,a in zip(seg['options'],auth['options']):localized(prefix+'/options/'+op['id']+'/label',op['label'],a['label_th'])
  else:
   assert seg['lines'][0]['eventId']==auth['event_id'];cues+=1
 assert dict(fileID=11400000,guid=guid(p),type=2) in db['events']
assert len(db['events'])==13 and count==54 and cues==4 and db['finalStoryDay']==6
ending_assets=[];previewcount=0
for op,ref in zip(source['day6_final_decision']['options'],db['endings']):
 p=R/'Assets/Data/Production/Endings'/(op['id']+'.asset');ev=asset(p);ending_assets.append(ev)
 assert ev['m_Script']['guid']==guid(R/'Assets/Scripts/Data/EndingData.cs')
 assert ref['guid']==guid(p) and ev['id']==op['id']
 localized('ending/'+ev['id']+'/optionLabel',ev['optionLabel'],op['label_th'])
 for i,(en,th) in enumerate(zip(ev['previewLines'][0]['dialogueLines'],op['preview_dialogue_th'])):localized('ending/'+ev['id']+'/previewLines/0/lines/'+str(i),en,th);previewcount+=1
assert previewcount==4 and ending_assets[2]['resetCycle']==1
assert ending_assets[1]['cutscenePrefab']['guid']=='4fd6f73861b6cce408d94eb4116497eb'
scene=docs(R/'Assets/Scenes/Game.unity');flow=scene[217966938]['MonoBehaviour'];manager=scene[217966939]['MonoBehaviour']
assert flow['database']['guid']==guid(dbpath)==manager['database']['guid']
assert flow['dialogue']==manager['dialogue'] and flow['dailyEvents']['fileID']==217966939
choice=scene[manager['choiceList']['fileID']]['MonoBehaviour'];assert choice['itemPrefab']['guid']=='0e40d531b2bffcc44b12b87cf431a6e7'
go=scene[choice['m_GameObject']['fileID']]['GameObject'];rt=scene[go['m_Component'][0]['component']['fileID']]['RectTransform']
assert rt['m_Father']['fileID']==1939456818
assert scene[1172304822]['MonoBehaviour']['endingList']['fileID']==411229344
print('PASS active scene/Production references; 13 groups, 54 exact Thai pages, 4 cues, 4 phone options, 3 endings, 4 exact previews, EN/TH/CSV keys')
# Existing serialized assets/GUIDs are not replaced, and unrelated table entries remain intact.
backup=R/'.utmp/daily-events-before-import'
if backup.exists():
 old=asset(backup/'Assets/Resources/Localization/LocalizationTable_TH.asset')['entries']
 assert all(entries[e['key']]==e for e in old),'Existing translation changed'
 before=asset(backup/'Assets/Data/Production/GameDatabase_Production.asset')
 for field in before:
  if field not in ('events','endings'):assert before[field]==db[field],field
 print('PASS existing Production fields and localization entries preserved')
temp=R/'Temp/DailyEventTests';temp.mkdir(parents=True,exist_ok=True)
def cs(v):
 if v is None:return 'null'
 if isinstance(v,str):return json.dumps(v,ensure_ascii=True)
 if isinstance(v,list):return 'new DialogueLine[] {'+','.join('new DialogueLine { text='+cs(x['dialogueLines'][0])+' }' for x in v)+'}'
lines=['using FirstJobber; static class Fixture { public static GameDatabase Create() { var db=new GameDatabase();']
for ev in events:
 lines.append('db.events.Add(new DailyEventData { id='+cs(ev['id'])+',exactDay='+str(ev['exactDay'])+',trigger=(EventTrigger)'+str(ev['trigger'])+',segments=new DailyEventSegment[] {')
 for seg in ev['segments']:
  options=','.join('new DailyEventOption {id='+cs(o['id'])+',label='+cs(o['label'])+'}' for o in seg['options'])
  lines.append('new DailyEventSegment {id='+cs(seg['id'])+',lines='+cs(seg['lines'])+',options=new DailyEventOption[] {'+options+'}},')
 lines.append('}});')
for e in ending_assets:lines.append('db.endings.Add(new EndingData { id='+cs(e['id'])+',optionLabel='+cs(e['optionLabel'])+',previewLines='+cs(e['previewLines'])+'});')
lines.append('return db;} }');fixture=temp/'Fixture.cs';fixture.write_text('\n'.join(lines),encoding='utf-8')
data=Path('C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data');mono=data/'MonoBleedingEdge/bin/mono.exe';out=temp/'Harness.exe'
subprocess.run([str(mono),str(data/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/langversion:9','/nowarn:0649','/out:'+str(out),str(fixture),str(Path(__file__).with_name('RuntimeHarness.cs')),str(R/'Assets/Scripts/Events/DailyEventManager.cs'),str(R/'Assets/Scripts/Endings/FinalDecisionController.cs')],check=True)
subprocess.run([str(mono),str(out)],check=True)
print('UNVERIFIED: Unity Play Mode, rendering, clicks, Timeline playback and 6-day end-to-end flow')
