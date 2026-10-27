"""Audit active Production resume localization; --fix repairs only its missing translations.

English/Thai are matched within each stable Choice ID against authored import content.
No assets, playback configuration or historical localization keys are recreated.
"""
import collections,csv,io,json,re,shutil,subprocess,sys
from pathlib import Path
import yaml
ROOT=Path(__file__).resolve().parents[2]
def documents(path):
 text=path.read_text(encoding='utf-8-sig')
 ids=re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M)
 text=re.sub(r'^%.*\n','',text,flags=re.M)
 text=re.sub(r'^--- !u!\d+ &-?\d+.*$','---',text,flags=re.M)
 return dict(zip(map(int,ids),yaml.safe_load_all(text)))
def asset(path):return documents(path)[11400000]['MonoBehaviour']
def guid(path):return re.search(r'^guid: (\w+)',Path(str(path)+'.meta').read_text(),re.M)[1]
def pages(block):
 lines=block.get('dialogueLines') or []
 return [(f'lines/{i}',text) for i,text in enumerate(lines)] if any(x and x.strip() for x in lines) else [('text',block.get('text'))]
dbpath=ROOT/'Assets/Data/Production/GameDatabase_Production.asset'
db=asset(dbpath)
paths={guid(p):p for p in (ROOT/'Assets/Data/Production/Resume').rglob('*.asset')}
source=json.loads((ROOT/'ContentImport/FirstJobber_Production_Content/production_content_bilingual.json').read_text(encoding='utf-8'))
authored={c['id']:c for cat in source['categories'] for c in cat['choices']}
categories=[asset(paths[ref['guid']]) for ref in db['categories']]
choices=[asset(paths[ref['guid']]) for ref in db['choices']]
assert len(choices)==15 and {c['id'] for c in choices}==set(authored), 'Production differs from expected 15 choices'
expected={};summary=[]
for cat in categories:
 ref=next(c for c in source['categories'] if c['id']==cat['id'])
 assert cat['displayName']==ref['display']['en']
 expected['category/'+cat['id']+'/displayName']=(cat['displayName'],ref['display']['th'])
 expected['category/'+cat['id']+'/emptyMessage']=(cat['emptyMessage'],'ไม่มีตัวเลือกเหลือในหมวดนี้แล้ว')
for choice in choices:
 ref=authored[choice['id']];prefix='choice/'+choice['id']+'/'
 assert choice['displayText']==ref['display']['en'],choice['id']+' source name'
 expected[prefix+'displayText']=(choice['displayText'],ref['display']['th'])
 assert not choice.get('description') and not choice.get('hrReactions'), 'New authored fields require audit'
 # Mapping by the exact English text within this choice tolerates blocks split in Inspector.
 translations={page['en']:page['th'] for block in ref['playerReaction']['blocks'] for page in block['lines']}
 count=0
 for i,block in enumerate(choice['playerReaction']):
  assert block['speaker']=='Player' and not block.get('genderIds'), 'Unexpected speaker/gender variant'
  for suffix,en in pages(block):
   assert en and en in translations,(choice['id'],i,en)
   expected[prefix+f'playerReaction/{i}/'+suffix]=(en,translations[en]);count+=1
 assert count==len(translations),'Lost authored reaction page'
 summary.append(dict(id=choice['id'],english=choice['displayText'],thai=ref['display']['th'],reaction_pages=count,mode=choice['playerReactionMode']))
tablepath=ROOT/'Assets/Resources/Localization/LocalizationTable_TH.asset'
csvpath=ROOT/'Assets/Localization/translation_template.csv'
table=asset(tablepath);entries=table['entries'];counts=collections.Counter(e['key'] for e in entries)
assert all(n==1 for n in counts.values()),'Duplicate table keys'
lookup={e['key']:e for e in entries}
prefixes=tuple('choice/'+c['id']+'/' for c in choices)+tuple('category/'+c['id']+'/' for c in categories)
orphans=[k for k in lookup if k.startswith(prefixes) and k not in expected]
placeholder=re.compile(r'\bUniversity\s*[-:]?\s*[A-E]\b|มหา(?:วิทยาลัย|ลัย)\s*[-:]?\s*[A-E]\b',re.I)
assert not any(placeholder.search(str(e.get(field) or '')) for e in entries if e['key'].startswith(prefixes) for field in ['english','thai']), 'Old university placeholder'
missing=[k for k in expected if k not in lookup]
empty=[k for k in expected if k in lookup and not (lookup[k].get('thai') or '').strip()]
stale=[k for k,(en,th) in expected.items() if k in lookup and lookup[k]['english']!=en]
incorrect=[k for k,(en,th) in expected.items() if k in lookup and lookup[k].get('thai') and lookup[k]['thai']!=th]
with csvpath.open(encoding='utf-8-sig',newline='') as f:rows=list(csv.reader(f))
assert all(n==1 for n in collections.Counter(r[0] for r in rows).values()),'Duplicate CSV keys'
csvlookup={r[0]:r for r in rows[1:]}
csvissues=[k for k,(en,th) in expected.items() if csvlookup.get(k)!=[k,en,th]]
report=dict(choices=summary,active_keys=len(expected),reaction_pages=sum(x['reaction_pages'] for x in summary),missing_keys=missing,empty_thai=empty,stale_english=stale,incorrect_thai=incorrect,duplicate_keys=0,historical_orphan_keys=orphans,csv_issues=csvissues)
temp=ROOT/'Temp/ResumeLocalizationAudit';temp.mkdir(parents=True,exist_ok=True)
if '--fix' in sys.argv:
 backup=ROOT/'.utmp/resume-localization-before-fix'
 backup.mkdir(parents=True,exist_ok=True)
 for path in [tablepath,csvpath]:
  target=backup/path.name
  if not target.exists():shutil.copy2(path,target)
 if not (backup/'audit-before.json').exists():
  (backup/'audit-before.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
 # Update just the relevant scalars; keep original unrelated YAML and pending CSV text.
 text=tablepath.read_text(encoding='utf-8-sig');node=yaml.compose(text)
 body=next(v for k,v in node.value if k.value=='MonoBehaviour')
 nodes=next(v for k,v in body.value if k.value=='entries')
 edits=[]
 for entry in nodes.value:
  fields={k.value:v for k,v in entry.value};key=fields['key'].value
  if key not in expected:continue
  en,th=expected[key]
  for field,value in [('english',en),('thai',th)]:
   if lookup[key].get(field)!=value:
    n=fields[field];a=n.start_mark.index
    replacement=json.dumps(value,ensure_ascii=True)
    if a>0 and text[a-1]==':':replacement=' '+replacement
    end=n.end_mark.index
    if n.tag.endswith(':null'):
     while end<len(text) and text[end] in ' \t':end+=1
    edits.append((a,end,replacement))
 for a,b,value in sorted(edits,reverse=True):text=text[:a]+value+text[b:]
 for key in missing:
  en,th=expected[key];text+='  - key: '+key+'\n    english: '+json.dumps(en,ensure_ascii=True)+'\n    thai: '+json.dumps(th,ensure_ascii=True)+'\n'
 tablepath.write_text(text,encoding='utf-8')
 raw=csvpath.read_bytes();bom=raw.startswith(b'\xef\xbb\xbf');text=raw.decode('utf-8-sig');lines=text.splitlines(keepends=True)
 reader=csv.reader(io.StringIO(text,newline=''));previous=0;chunks=[];seen=set()
 for row in reader:
  end=reader.line_num;original=''.join(lines[previous:end]);previous=end
  if row and row[0] in expected:
   key=row[0];seen.add(key);en,th=expected[key]
   if row!=[key,en,th]:
    stream=io.StringIO(newline='');csv.writer(stream,lineterminator='\r\n' if original.endswith('\r\n') else '\n').writerow([key,en,th]);original=stream.getvalue()
  chunks.append(original)
 for key,(en,th) in expected.items():
  if key not in seen:
   stream=io.StringIO(newline='');csv.writer(stream,lineterminator='\r\n').writerow([key,en,th]);chunks.append(stream.getvalue())
 csvpath.write_bytes((b'\xef\xbb\xbf' if bom else b'')+''.join(chunks).encode('utf-8'))
 print('FIXED table:',len(missing),'new keys;',len(empty),'empty Thai;',len(stale),'stale English;',len(incorrect),'incorrect Thai; CSV:',len(csvissues),'rows')
else:
 assert not (missing or empty or stale or incorrect or csvissues),json.dumps(report,ensure_ascii=True)
 scene=documents(ROOT/'Assets/Scenes/Game.unity');flow=scene[217966938]['MonoBehaviour']
 assert flow['database']['guid']==guid(dbpath) and flow['resumeEditor']['fileID']==1381945727
 assert flow['dialogue']['fileID']==217966941
 backup=ROOT/'.utmp/resume-localization-before-fix'
 if backup.exists():
  before=asset(backup/tablepath.name)['entries']
  assert all(lookup[e['key']]==e for e in before if e['key'] not in expected),'Unrelated/historical translation was modified'
  with (backup/csvpath.name).open(encoding='utf-8-sig',newline='') as f:oldrows=list(csv.reader(f))
  assert all(csvlookup[r[0]]==r for r in oldrows[1:] if r[0] not in expected),'Unrelated/pending CSV translation was modified'
 # All expected Thai characters are present in the configured TMP font's character table.
 fontguid=table['font']['guid'];font=next(p for p in (ROOT/'Assets/Fonts').rglob('*.asset') if guid(p)==fontguid)
 fontdata=asset(font);available={int(c['m_Unicode']) for c in fontdata['m_CharacterTable']}
 required={ord(c) for _,th in expected.values() for c in th if '\u0e00'<=c<='\u0e7f'}
 assert not required-available, 'Missing Thai glyphs: '+str(required-available)
 report['font']=str(font.relative_to(ROOT));report['thai_glyphs_checked']=len(required)
 (temp/'audit-after.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
 print('PASS',len(choices),'Production choices;',len(expected),'active keys;',report['reaction_pages'],'authored reaction pages; EN/TH/CSV; Thai glyphs',len(required))
 print('Historical unused keys retained:',len(orphans),'(not duplicate keys). No runtime assets/playback modes changed.')
 # Use existing API doubles, but compile the actual localization and dialogue implementations.
 old=(ROOT/'Tests/ProductionConfidence/RuntimeHarness.cs').read_text(encoding='utf-8')
 support='using System; using System.Collections; using System.Collections.Generic; using UnityEngine;\n'+old[old.index('namespace UnityEngine'):]
 support=support.replace('    public static class LocalizationManager { public static string Get(string key, string english) => english; }','')
 support+='''
namespace UnityEngine {
 public enum RuntimeInitializeLoadType {SubsystemRegistration}
 public class RuntimeInitializeOnLoadMethodAttribute : Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) {}}
 public static class PlayerPrefs {public static int GetInt(string k,int d)=>d; public static void SetInt(string k,int v){} public static void Save(){}}
 public static class Resources {public static T Load<T>(string path) where T:class => ResumeFixture.Table as T;}
}
namespace TMPro {public class TMP_FontAsset {}}
namespace PhEngine.ThaiTextCare {public enum ThaiGlyphCorrection {None}}
'''
 (temp/'Support.cs').write_text(support,encoding='utf-8')
 def cs(value):return json.dumps(value or '',ensure_ascii=True)
 lines=['using FirstJobber; using System.Collections.Generic; static class ResumeFixture { public static LocalizationTable Table;',
        'public static GameDatabase Create() { var db=new GameDatabase {genders=null,genderPrompt=""}; Table=new LocalizationTable();']
 for key,(en,th) in expected.items():lines.append('Table.entries.Add(new LocalizationTable.Entry {key='+cs(key)+',english='+cs(en)+',thai='+cs(th)+'});')
 for key,th in [('test/empty',''),('test/whitespace','   '),('test/legacy/text','Legacy Thai')]:lines.append('Table.entries.Add(new LocalizationTable.Entry {key='+cs(key)+',thai='+cs(th)+'});')
 for cat in categories:lines.append('db.categories.Add(new ResumeCategoryData {id='+cs(cat['id'])+',displayName='+cs(cat['displayName'])+',emptyMessage='+cs(cat['emptyMessage'])+'});')
 for choice in choices:
  blocks=[]
  for block in choice['playerReaction']:
   blocks.append('new DialogueLine {speaker='+cs(block['speaker'])+',text='+cs(block.get('text'))+',dialogueLines=new List<string> {'+','.join(cs(t) for t in block.get('dialogueLines',[]))+'}}')
  lines.append('db.choices.Add(new ResumeChoiceData {id='+cs(choice['id'])+',displayText='+cs(choice['displayText'])+',playerReaction=new DialogueLine[] {'+','.join(blocks)+'}});')
 lines.append('return db;} }');(temp/'Fixture.cs').write_text('\n'.join(lines),encoding='utf-8')
 data=Path('C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data');mono=data/'MonoBleedingEdge/bin/mono.exe';exe=temp/'RuntimeHarness.exe'
 scripts=['Data/GameDatabase.cs','Data/GenderData.cs','Data/DialogueData.cs','Data/ConfidenceDialogueDatabase.cs','Dialogue/DialogueManager.cs','Localization/LocalizationKeys.cs','Localization/LocalizationManager.cs','Localization/LocalizationTable.cs']
 subprocess.run([str(mono),str(data/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/langversion:9','/nowarn:0649,0067','/out:'+str(exe),str(temp/'Support.cs'),str(temp/'Fixture.cs'),str(Path(__file__).with_name('RuntimeHarness.cs')),*[str(ROOT/'Assets/Scripts'/p) for p in scripts]],check=True)
 subprocess.run([str(mono),str(exe)],check=True)
