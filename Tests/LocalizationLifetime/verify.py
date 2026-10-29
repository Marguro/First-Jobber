"""Production event GUID audit + real LocalizationKeys with simulated Unity destroyed-object semantics.
This does not execute Unity Play Mode or Web/IL2CPP serialization.
"""
from pathlib import Path
import re,json,subprocess,yaml
R=Path(__file__).resolve().parents[2]
def asset(p):
 s=p.read_text(encoding='utf-8-sig');s=re.sub(r'^%.*\n','',s,flags=re.M);s=re.sub(r'^--- !u!\d+ &-?\d+.*$','---',s,flags=re.M);return next(yaml.safe_load_all(s))['MonoBehaviour']
def guid(p):return re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1]
paths={guid(p):p for p in (R/'Assets/Data/Production/DailyEvents').glob('*.asset')}
db=asset(R/'Assets/Data/Production/GameDatabase_Production.asset');events=[]
for ref in db['events']:
 assert ref['fileID']==11400000 and ref['guid'] in paths
 e=asset(paths[ref['guid']]);assert e['m_Script']['guid']==guid(R/'Assets/Scripts/Data/DailyEventData.cs');events.append(e)
assert len(events)==13 and len({e['id'] for e in events})==13 and {e['exactDay'] for e in events}==set(range(1,7))
entries={e['key']:e for e in asset(R/'Assets/Resources/Localization/LocalizationTable_TH.asset')['entries']}
T=R/'Temp/LocalizationLifetimeTests';T.mkdir(parents=True,exist_ok=True)
support=(R/'Tests/ProductionConfidence/RuntimeHarness.cs').read_text(encoding='utf-8-sig');support='using System;using System.Collections;using System.Collections.Generic;using FirstJobber;using UnityEngine;\n'+support[support.index('namespace UnityEngine'):]
support=support.replace('public class Object { public string name; }', '''public class Object {bool dead;string label="";public int NameReads;public string name {get{NameReads++;if(dead)throw new Exception("Accessed name of destroyed Unity object");return label;}set{label=value;}}public void DestroyForTest(){dead=true;}public static bool operator==(Object a,Object b){bool an=ReferenceEquals(a,null)||(!ReferenceEquals(a,null)&&a.dead),bn=ReferenceEquals(b,null)||(!ReferenceEquals(b,null)&&b.dead);return an||bn?an==bn:ReferenceEquals(a,b);}public static bool operator!=(Object a,Object b)=>!(a==b);public override bool Equals(object o)=>ReferenceEquals(this,o);public override int GetHashCode()=>base.GetHashCode();}''')
support=support.replace('public static void LogWarning(string s, Object context = null) { }','public static List<string> Warnings=new List<string>();public static void LogWarning(string s, Object context = null) {Warnings.Add(s);}')
support=re.sub(r'^    public class DailyEvent(?:Option|Segment|Data) .*\n','',support,flags=re.M)
(T/'Support.cs').write_text(support,encoding='utf-8')
cs=lambda s:json.dumps(s,ensure_ascii=True)
def lines(blocks):
 return 'new DialogueLine[]{'+','.join('new DialogueLine{speaker='+cs(b.get('speaker'))+',text='+cs(b.get('text'))+',dialogueLines=new List<string>{'+','.join(cs(x) for x in b.get('dialogueLines') or [])+'}}' for b in blocks or [])+'}'
code='using System;using System.Collections.Generic;using FirstJobber;using UnityEngine;\nstatic class ProductionEventFixture {public static GameDatabase Create(){var db=new GameDatabase{name="Production"};'
expected={}
for e in events:
 code+='db.events.Add(new DailyEventData{id='+cs(e['id'])+',name='+cs(e['m_Name'])+',caption='+cs(e.get('caption'))+',lines='+lines(e.get('lines'))+',segments=new DailyEventSegment[]{'
 for seg in e.get('segments') or []:
  code+='new DailyEventSegment{id='+cs(seg['id'])+',lines='+lines(seg.get('lines'))+',options=new DailyEventOption[]{'+','.join('new DailyEventOption{id='+cs(o['id'])+',label='+cs(o['label'])+'}' for o in seg.get('options') or [])+'}},'
 code+='}});'
 for k,v in entries.items():
  if k.startswith('event/'+e['id']+'/'):
   assert v['english'] and v['thai'];expected[k]=v['english']
code+='return db;}public static Dictionary<string,string> Expected()=>new Dictionary<string,string>{'+','.join('{'+cs(k)+','+cs(v)+'}' for k,v in expected.items())+'};}'
(T/'ProductionFixture.cs').write_text(code,encoding='utf-8')
D=Path('C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data');mono=D/'MonoBleedingEdge/bin/mono.exe';exe=T/'Lifetime.exe'
files=['Data/GameDatabase.cs','Data/GenderData.cs','Data/DialogueData.cs','Data/ConfidenceDialogueDatabase.cs','Data/DailyEventData.cs','Core/GamePhase.cs','Dialogue/DialogueManager.cs','Localization/LocalizationKeys.cs']
subprocess.run([str(mono),str(D/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/langversion:9','/nowarn:0649,0067','/out:'+str(exe),str(T/'Support.cs'),str(T/'ProductionFixture.cs'),str(Path(__file__).with_name('RuntimeHarness.cs')),*[str(R/'Assets/Scripts'/f) for f in files]],check=True)
subprocess.run([str(mono),str(exe)],check=True)
print('PASS disk: all 13 Production GUIDs/scripts/IDs valid, Days1-6; '+str(len(expected))+' existing event English/Thai entries checked. No assets rewritten.')
print('UNVERIFIED: native imported object identity/lifetime, repeated Unity Play Mode, Save/Continue integration and Web build.')
