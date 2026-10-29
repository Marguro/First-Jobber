from pathlib import Path
import re,yaml,subprocess,json
R=Path(__file__).resolve().parents[2]
def docs(p):
 s=p.read_text(encoding='utf-8-sig');ids=re.findall(r'^--- !u!\d+ &(-?\d+)',s,re.M);assert len(ids)==len(set(ids));s=re.sub(r'^%.*\n','',s,flags=re.M);s=re.sub(r'^--- !u!\d+ &-?\d+.*$','---',s,flags=re.M);return dict(zip(map(int,ids),yaml.safe_load_all(s)))
def asset(p):return docs(p)[11400000]['MonoBehaviour']
def guid(p):return re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1]
scene=docs(R/'Assets/Scenes/Game.unity');ui=scene[1939456819]['MonoBehaviour'];image=scene[ui['background']['fileID']]['MonoBehaviour']
assert ui['presentationGroup']['fileID']==2189300001 and ui['panels']['fileID']==217966940
assert image['m_RaycastTarget']==0 and image['m_PreserveAspect']==1 and image['m_Material']['fileID']==0
assert scene[1877831937]['RectTransform']['m_Father']['fileID']==627569008
order=scene[627569008]['RectTransform']['m_Children'];idx=lambda fid:order.index({'fileID':fid})
assert idx(1396686514)<idx(1877831937)<idx(1939456818)<idx(1172304820)<idx(1529752985)<idx(977193194)<idx(901422122)
assert scene[1636116355]['RectTransform']['m_Father']['fileID']==1877831937
# Native sprite subasset IDs, not merely a matching image filename.
expected={'event_day01_opening':'MY ROOM D1.png','event_day02_before_resume':'MY ROOM D2.png','event_day04_before_resume':'MY ROOM D3.png','event_day06_final_story':'MY ROOM D4.png'}
database=asset(R/'Assets/Data/Production/GameDatabase_Production.asset')
for event,art in expected.items():
 p=R/f'Assets/Data/Production/DailyEvents/{event}.asset';e=asset(p);sprite=R/'Assets/Sprites/UI/Background'/art
 assert e['id']==event and e['background']['guid']==guid(sprite)
 assert str(e['background']['fileID']) in Path(str(sprite)+'.meta').read_text(),event+' subasset ID'
 assert {'fileID':11400000,'guid':guid(p),'type':2} in database['events']
# Only one requested translation changed; all other entries stay intact when a current-task backup exists.
key='event/event_day01_resume_intro/segments/resume_intro/lines/0/lines/0'
entries=asset(R/'Assets/Resources/Localization/LocalizationTable_TH.asset')['entries'];matches=[e for e in entries if e['key']==key];assert len(matches)==1
assert matches[0]['thai']=='เช้าวันแรกของการเป็นเฟิร์สจ๊อบแจ๊บ!' and matches[0]['english']=='My first morning as a fresh graduate entering the workforce!'
backup=R/'Temp/EventBackgroundBefore/Assets/Resources/Localization/LocalizationTable_TH.asset'
if backup.exists():
 before=asset(backup)['entries'];assert [x for x in before if x['key']!=key]==[x for x in entries if x['key']!=key]
for event in expected:
 path=R/f'Assets/Data/Production/DailyEvents/{event}.asset';b=R/'Temp/EventBackgroundBefore'/path.relative_to(R)
 if b.exists():assert path.read_bytes()==b.read_bytes()
T=R/'Temp/EventBackgroundTests';T.mkdir(parents=True,exist_ok=True)
s=(R/'Tests/DailyEvents/RuntimeHarness.cs').read_text(encoding='utf-8-sig');s=s[:s.index('class Harness')]+s[s.index('namespace UnityEngine'):]
s=s.replace('public class Object {}','public class Object {public string name;}')
s=s.replace('public class MonoBehaviour : Object {}','public class MonoBehaviour : Object {public bool enabled=true;public GameObject gameObject=new GameObject{active=true};public bool isActiveAndEnabled=>enabled&&gameObject.activeInHierarchy;}')
s=s.replace('public class SerializeField : Attribute {}', 'public class SerializeField : Attribute {}public class HeaderAttribute:Attribute{public HeaderAttribute(string s){}}public class ContextMenu:Attribute{public ContextMenu(string s){}}public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1,1);}public class Sprite:Object{}public class CanvasGroup:MonoBehaviour{public float alpha=1;public bool interactable=true,blocksRaycasts=true,ignoreParentGroups;}')
s=s.replace('public class DailyEventData {public string id,', 'public class DailyEventData {public string name,id,').replace('public object background;','public UnityEngine.Sprite background;')
s=re.sub(r'    public class DailyEventPanelUI \{.*\n','',s)
s=s.replace('public class AtmosphereManager {public static AtmosphereManager Instance;', 'public class AtmospherePhaseData:UnityEngine.Object{}public class AtmosphereManager {public AtmospherePhaseData AppliedPhase;public static AtmosphereManager Instance;')
s=s.replace('public class DialogueManager { public Action Next;', 'public class DialogueManager { public static Action BeforePlay;public Action Next;').replace('{ Next=done; }','{BeforePlay?.Invoke();Next=done; }')
s+='''
namespace UnityEngine.UI {public class Image:UnityEngine.MonoBehaviour{public UnityEngine.Sprite sprite;public UnityEngine.Color color;public bool raycastTarget;public T[] GetComponentsInParent<T>(bool v)=>new T[0];}}
namespace FirstJobber {public enum GamePhase{DailyEvent}public class UIManager{public UnityEngine.CanvasGroup CurrentPanel,DailyPanel=new UnityEngine.CanvasGroup();public UnityEngine.CanvasGroup PanelFor(GamePhase p)=>DailyPanel;}}
'''
(T/'Support.cs').write_text(s,encoding='utf-8')
cs=lambda value:json.dumps(value,ensure_ascii=True)
fixture='using FirstJobber;class Fixture{public static GameDatabase Create(){var db=new GameDatabase();'
for ref in database['events']:
 path=next(path for path in (R/'Assets/Data/Production/DailyEvents').glob('*.asset') if guid(path)==ref['guid'])
 event=asset(path)
 fixture+='db.events.Add(new DailyEventData{id='+cs(event['id'])+',exactDay='+str(event['exactDay'])+',trigger=(EventTrigger)'+str(event['trigger'])+',segments=new DailyEventSegment[]{'
 for seg in event.get('segments') or []:
  fixture+='new DailyEventSegment{id='+cs(seg['id'])+',lines=new DialogueLine[]{'+','.join('new DialogueLine{text='+cs('\n'.join(line.get('dialogueLines') or [line.get('text') or '']))+'}' for line in seg.get('lines') or [])+'},options=new DailyEventOption[]{'+','.join('new DailyEventOption{id='+cs(op['id'])+',label='+cs(op['label'])+'}' for op in seg.get('options') or [])+'}},'
 fixture+='}});'
fixture+='return db;}}'
(T/'Fixture.cs').write_text(fixture,encoding='utf-8')
D=Path('C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data');mono=D/'MonoBleedingEdge/bin/mono.exe';out=T/'Background.exe'
subprocess.run([str(mono),str(D/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/langversion:9','/nowarn:0649','/out:'+str(out),str(T/'Support.cs'),str(T/'Fixture.cs'),str(Path(__file__).with_name('RuntimeHarness.cs')),str(R/'Assets/Scripts/Events/DailyEventManager.cs'),str(R/'Assets/Scripts/UI/DailyEventPanelUI.cs')],check=True)
subprocess.run([str(mono),str(out)],check=True)
print('PASS serialized Production backgrounds/subasset IDs, independent existing Image layer/order, aspect/raycast/material, exact stable EN/TH key; no artwork replacement.')
print('UNVERIFIED: native Game View pixels, CanvasGroup fades, masks and aspect at resolutions, phase appearance, mouse interaction and WebGL build.')
