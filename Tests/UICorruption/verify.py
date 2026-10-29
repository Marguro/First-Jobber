"""Production corruption references/safety + real policy, geometry and surface code using API doubles."""
from pathlib import Path
import re,yaml,subprocess,json
R=Path(__file__).resolve().parents[2]
def docs(p):
 s=p.read_text(encoding='utf-8-sig');ids=re.findall(r'^--- !u!\d+ &(-?\d+)',s,re.M);assert len(ids)==len(set(ids)),p
 s=re.sub(r'^%.*\n','',s,flags=re.M);s=re.sub(r'^--- !u!\d+ &-?\d+.*$','---',s,flags=re.M)
 return dict(zip(map(int,ids),yaml.safe_load_all(s)))
def guid(p):return re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1]
asset=R/'Assets/Data/UI/UICorruption_Production.asset';cfg=docs(asset)[11400000]['MonoBehaviour'];assert [p['intensity'] for p in cfg['phases']]==[0,.55,.95]
for p,name in zip(cfg['phases'],['Phase_01_Normal','Phase_02_Deterioration','Phase_03_Despair']):assert p['phase']['guid']==guid(R/f'Assets/Data/Atmosphere/{name}.asset')
sguid=guid(R/'Assets/Scripts/UI/UICorruptionSurface.cs');binding=guid(R/'Assets/Scripts/Atmosphere/AtmosphereSpriteBinding.cs');coverage=guid(R/'Assets/Scripts/Atmosphere/AtmosphereOverlayMask.cs')
rects=images=0
for p in [R/'Assets/Scenes/Game.unity',*(R/'Assets/Prefabs/UI').glob('*.prefab')]:
 d=docs(p);backup=R/'Temp/VisibleCorruptionBefore'/p.relative_to(R)
 if backup.exists():
  before=docs(backup)
  for i,old in before.items():
   assert i in d,'Existing serialized object removed'
   m=old.get('MonoBehaviour',{});g=m.get('m_Script',{}).get('guid')
   if 'RectTransform' in old:assert d[i]==old, f'Authored transform changed: {p}:{i}';rects+=1
   if g in ['fe87c0e1cc204ed48ad3b37840f39efc','f4688fdb7df04437aeb418b961361dc5',binding,coverage]:assert d[i]==old,f'Artwork/text/phase binding changed: {p}:{i}';images+=1
  # Entire existing HR room and ending media remain untouched (no new component on their objects).
  if p.suffix=='.unity':
   for rt in [796213110,762817073]:
    todo=[rt]
    while todo:
     rid=todo.pop();r=before[rid]['RectTransform'];go=r['m_GameObject']['fileID'];assert d[go]==before[go]
     for c in before[go]['GameObject']['m_Component']:assert d[c['component']['fileID']]==before[c['component']['fileID']]
     todo += [x['fileID'] for x in r['m_Children']]
 if p.suffix=='.prefab' and p.stem in ['ResumeCategoryButton','ResumeChoiceButton','CompanySelectionButton','InterviewAnswerButton','EventChoiceButton','EndingChoiceButton','EndingActionButton']:
  surfaces=[(i,v['MonoBehaviour']) for i,v in d.items() if v.get('MonoBehaviour',{}).get('m_Script',{}).get('guid')==sguid];assert len(surfaces)==1
  fid,c=surfaces[0];assert c['settings']['guid']==guid(asset) and c['role']==0
  view=next(v['MonoBehaviour'] for v in d.values() if v.get('MonoBehaviour',{}).get('m_Script',{}).get('guid')==guid(R/'Assets/Scripts/UI/ChoiceButtonUI.cs'));assert view['corruption']['fileID']==fid and c['textSource']==view['titleText']
scene=docs(R/'Assets/Scenes/Game.unity');assert len([v for v in scene.values() if v.get('MonoBehaviour',{}).get('m_Script',{}).get('guid')==sguid])==7
assert scene[1381945727]['MonoBehaviour']['corruptionSettings']['guid']==guid(asset)
# Verify serialized tear sources are existing UI artwork, including runtime prefab backgrounds.
tear_targets=0
for path in [R/'Assets/Scenes/Game.unity',*(R/'Assets/Prefabs/UI').glob('*.prefab')]:
 objects=docs(path)
 for value in objects.values():
  surface=value.get('MonoBehaviour',{})
  if surface.get('m_Script',{}).get('guid')!=sguid:continue
  source=surface.get('tearSource',{}).get('fileID',0)
  if not source:continue
  graphic=objects[source]['MonoBehaviour']
  assert graphic['m_Script']['guid']=='fe87c0e1cc204ed48ad3b37840f39efc'
  assert graphic['m_Sprite']['fileID']!=0
  linked=objects[surface['tearPhaseBinding']['fileID']]['MonoBehaviour']
  assert linked['m_Script']['guid']==binding and linked['image']['fileID']==source
  tear_targets+=1
assert tear_targets==12

for p in ['Assets/Scripts/Core/ChoiceManager.cs','Assets/Scripts/Core/GameFlowManager.cs','Assets/Scripts/Atmosphere/AtmosphereManager.cs','Assets/Scripts/UI/ScrollingBackground.cs','Assets/Scripts/Core/DayBgmController.cs','Assets/Scripts/UI/UISfxPlayer.cs','Assets/Scripts/UI/UISfxSettings.cs','Assets/Scripts/Endings/EndingController.cs']:
 assert subprocess.run(['git','diff','--quiet','--',p],cwd=R).returncode==0,'Out-of-scope system changed: '+p
changed=subprocess.check_output(['git','diff','--name-only'],cwd=R,text=True).splitlines();assert not any(p.startswith(('Assets/Data/Production/','Assets/Sprites/','Assets/Resources/Localization/')) for p in changed)
source=(R/'Assets/Scripts/UI/UICorruptionSurface.cs').read_text();assert not re.search(r'textSource\.\w+\s*=(?!=)',source) and 'new Material' not in source
print(f'PASS seven button bindings + seven scoped scene surfaces; {rects} authored transforms and {images} original graphics/TMP/phase bindings preserved; HR/media/gameplay/RNG/art/localization/BGM/SFX unchanged')
T=R/'Temp/UICorruptionTests';T.mkdir(exist_ok=True)
# Reuse tested minimal UI/DOTween API doubles; do not ship fake APIs in Assets.
s=(R/'Tests/UIPolish/RuntimeHarness.cs').read_text(encoding='utf-8-sig').split('class Tests {')[0]
s=s.replace('public static float unscaledDeltaTime=.1f,unscaledTime;', 'public static float unscaledDeltaTime=.1f,unscaledTime;')
s=s.replace('public T[] GetComponentsInChildren<T>(bool includeInactive)=>new T[0];','public T[] GetComponentsInChildren<T>(bool includeInactive)=>new T[0];public T[] GetComponentsInParent<T>(bool inactive)where T:class {var list=new List<T>();for(var t=transform;t!=null;t=t.parent){var c=t.GetComponent<T>();if(c!=null)list.Add(c);}return list.ToArray();}')
s=s.replace('public Coroutine StartCoroutine(IEnumerator x)=>new Coroutine();','public Coroutine StartCoroutine(IEnumerator x)=>new Coroutine();public void StopCoroutine(Coroutine c){}public static void Destroy(UnityEngine.Object o){}')
s=s.replace('public bool activeSelf=true;', 'public static int created;public int layer;public bool activeSelf=true;')
s=s.replace('public bool activeInHierarchy=>activeSelf;', 'public bool activeInHierarchy=>activeSelf&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);')
s=s.replace('public GameObject(){transform=', 'public GameObject(){created++;transform=')
s=s.replace('public T Add<T>(T c)', 'public GameObject(string n,params Type[] types):this(){name=n;}public T AddComponent<T>()where T:Component,new()=>Add(new T());public T Add<T>(T c)')
s=s.replace('public Transform parent;', 'public Transform parent;public void SetParent(Transform p,bool world){parent=p;}')
s=s.replace('public Vector2 anchoredPosition;', 'public Vector2 anchoredPosition,anchorMin,anchorMax,offsetMin,offsetMax;public Rect rect=new Rect(-200,-40,400,80);')
s=s.replace('public static Vector2 one=>', 'public static Vector2 zero=>new Vector2(0,0);public static Vector2 one=>')
s=s.replace('float a,float b,float c){x=a;y=b;z=c;}', 'float a,float b,float c=0){x=a;y=b;z=c;}')
s=s.replace('public static class Mathf {', 'public static class Mathf {public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v));public static float Lerp(float a,float b,float t)=>a+(b-a)*t;')
s=s.replace('public class WaitForSecondsRealtime {public WaitForSecondsRealtime(float f){}}', 'public class WaitForSecondsRealtime {public float waitTime;public void Reset(){}public WaitForSecondsRealtime(float f){waitTime=f;}}')
s=s.replace('namespace UnityEngine.Audio {', 'namespace UnityEngine { public static class Application{public static bool isPlaying=true;}public class CanvasRenderer:Component{}public class ContextMenu:Attribute{public ContextMenu(string n){}}public struct Color{public float r,g,b,a;public Color(float red,float green,float blue,float alpha=1){r=red;g=green;b=blue;a=alpha;}}public struct Rect{public float x,y,width,height;public float xMin=>x;public float yMin=>y;public Rect(float a,float b,float w,float h){x=a;y=b;width=w;height=h;}} }\nnamespace UnityEngine.Audio {')
s=s.replace('public class LayoutGroup:MonoBehaviour {}', 'public class LayoutGroup:MonoBehaviour {}public class LayoutElement:MonoBehaviour{public bool ignoreLayout;}public class MaskableGraphic:MonoBehaviour{public bool raycastTarget,maskable;public Color color;public RectTransform rectTransform=>(RectTransform)transform;public void SetVerticesDirty(){}protected virtual void OnPopulateMesh(VertexHelper v){}}public class VertexHelper{public List<Vector3> vertices=new List<Vector3>();public int currentVertCount=>vertices.Count;public void Clear(){vertices.Clear();}public void AddVert(Vector3 v,Color c,Vector2 uv){vertices.Add(v);}public void AddTriangle(int a,int b,int c){}}')
s=s.replace('public class TMP_Text:MonoBehaviour{', 'public class TextMeshProUGUI:TMP_Text{}public class TMP_Text:MonoBehaviour{public RectTransform rectTransform=>(RectTransform)transform;public object font,fontSharedMaterial,spriteAsset;public float fontSize=24,fontSizeMin=12,fontSizeMax=30,characterSpacing,wordSpacing,lineSpacing,paragraphSpacing;public bool raycastTarget,maskable,enableAutoSizing,richText,isRightToLeftText;public int fontStyle,alignment,textWrappingMode,overflowMode;public object margin;public Color color;')
s+='\nnamespace DG.Tweening {public static class DOVirtual{public static Tween DelayedCall(float d,Action a,bool unscaled)=>DOTween.New(a);}}\n'
s+='\nnamespace TMPro {public class FastAction<T>{event Action<T> listeners;public void Add(Action<T> a){listeners+=a;}public void Remove(Action<T> a){listeners-=a;}public void Call(T v){listeners?.Invoke(v);}}public static class TMPro_EventManager{public static FastAction<UnityEngine.Object> TEXT_CHANGED_EVENT=new FastAction<UnityEngine.Object>();}}\n'
s=s.replace('public AtmospherePhaseData AppliedPhase;', 'public AtmospherePhaseData AppliedPhase;public static event Action<AtmospherePhaseData> PhaseChanged;public static void Raise(AtmospherePhaseData p){PhaseChanged?.Invoke(p);}')
s=s.replace('public Sequence Append(Tween t)', 'public void First(){children[0].Complete();}public Sequence AppendCallback(Action a)=>Append(DOTween.New(a));public Sequence AppendInterval(float f)=>Append(DOTween.New(()=>{}));public Sequence Append(Tween t)')
s+='''
namespace FirstJobber {
 public static class ThaiTextApplier{public static void Register(TMPro.TMP_Text t){}}
 public enum ChoiceState{Available,Selected,Consumed,Disabled}
 public class ResumeCategoryData:ScriptableObject{public string id;}
 public class ResumeChoiceData:ScriptableObject{public string id;public ResumeCategoryData category;public float Confidence=80;}
 public class CompanyData:ScriptableObject{public string id;}
 public class GameDatabase:ScriptableObject{public List<ResumeChoiceData> choices=new List<ResumeChoiceData>();public List<ResumeCategoryData> categories=new List<ResumeCategoryData>();public List<CompanyData> companies=new List<CompanyData>();public bool showConsumedChoices;public ResumeChoiceData FindChoice(string id){foreach(var c in choices)if(c.id==id)return c;return null;}}
 public class Selection{public string categoryId,choiceId;}
 public class HistoryEntry{public int day;public string companyId,result;public float confidence;public List<string> choiceIds=new List<string>();}
 public class GameSession{public List<string> consumedChoiceIds=new List<string>(),consumedCompanyIds=new List<string>(),interviewQuestionIds=new List<string>();public List<Selection> selections=new List<Selection>();public List<HistoryEntry> history=new List<HistoryEntry>();public int day=1,interviewIndex;public string selectedCompanyId;public float confirmedConfidence;public string GetSelectedChoiceId(string id){foreach(var x in selections)if(x.categoryId==id)return x.choiceId;return null;}public void SetSelection(string cat,string choice){selections.RemoveAll(s=>s.categoryId==cat);selections.Add(new Selection{categoryId=cat,choiceId=choice});}}
}
'''

# Additional API doubles exercise actual tear clipping and current source-sprite copies.
s=s.replace('public class CanvasRenderer:Component{}','public class CanvasRenderer:Component{public bool cull;}')
s=s.replace('public Vector2 anchoredPosition,anchorMin,anchorMax,offsetMin,offsetMax;', 'public Vector2 anchoredPosition,anchorMin,anchorMax,offsetMin,offsetMax,pivot;')
s=s.replace('public static class Mathf {','public static class Mathf {public static float Sqrt(float v)=>(float)Math.Sqrt(v);public static float Abs(float v)=>Math.Abs(v);public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));')
s=s.replace('public struct Color{','public class HideInInspector:Attribute{}public struct Color{public static Color operator*(Color a,Color b)=>new Color(a.r*b.r,a.g*b.g,a.b*b.b,a.a*b.a);')
s=s.replace('public static class Debug {','public static class Debug {public static void Log(string s,UnityEngine.Object o=null){}')
s=s.replace('public class TMP_Text:MonoBehaviour{','public class TMP_Text:MonoBehaviour{public CanvasRenderer canvasRenderer=new CanvasRenderer();')
s=s.replace('public class Image:MonoBehaviour {public Sprite sprite;}','''public class Image:MaskableGraphic {public Sprite sprite,overrideSprite;public int type,fillMethod,fillOrigin;public bool preserveAspect,fillCenter,fillClockwise,useSpriteMesh;public float fillAmount,pixelsPerUnitMultiplier;public object material;public CanvasRenderer canvasRenderer=new CanvasRenderer();protected override void OnPopulateMesh(VertexHelper v){v.Clear();var a=new UIVertex{position=new Vector3(-200,-40,0),uv0=new Vector4(0,0,0,0),color=new Color32(255,255,255,255)};var b=a;b.position=new Vector3(200,-40,0);b.uv0=new Vector4(1,0,0,0);var c=a;c.position=new Vector3(200,40,0);c.uv0=new Vector4(1,1,0,0);var d=a;d.position=new Vector3(-200,40,0);d.uv0=new Vector4(0,1,0,0);v.AddVert(a);v.AddVert(b);v.AddVert(c);v.AddVert(d);v.AddTriangle(0,1,2);v.AddTriangle(0,2,3);}}''')
s=s.replace('public class VertexHelper{public List<Vector3> vertices=new List<Vector3>();public int currentVertCount=>vertices.Count;public void Clear(){vertices.Clear();}public void AddVert(Vector3 v,Color c,Vector2 uv){vertices.Add(v);}public void AddTriangle(int a,int b,int c){}}','''public class VertexHelper{public List<Vector3> vertices=new List<Vector3>();public List<UIVertex> data=new List<UIVertex>();public List<int> indices=new List<int>();public int currentVertCount=>vertices.Count;public void Clear(){vertices.Clear();data.Clear();indices.Clear();}public void AddVert(Vector3 v,Color c,Vector2 uv){vertices.Add(v);data.Add(new UIVertex{position=v});}public void AddVert(UIVertex v){vertices.Add(v.position);data.Add(v);}public void AddTriangle(int a,int b,int c){indices.Add(a);indices.Add(b);indices.Add(c);}public void GetUIVertexStream(List<UIVertex> stream){stream.Clear();foreach(int i in indices)stream.Add(data[i]);}}''')
s=s.replace('public static Vector3 one=>','public static Vector3 LerpUnclamped(Vector3 a,Vector3 b,float t)=>new Vector3(a.x+(b.x-a.x)*t,a.y+(b.y-a.y)*t,a.z+(b.z-a.z)*t);public static Vector3 one=>')
s+='''
namespace UnityEngine {public struct Vector4{public float x,y,z,w;public Vector4(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}public static Vector4 LerpUnclamped(Vector4 a,Vector4 b,float t)=>new Vector4(a.x+(b.x-a.x)*t,a.y+(b.y-a.y)*t,a.z+(b.z-a.z)*t,a.w+(b.w-a.w)*t);}public struct Color32{public byte r,g,b,a;public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color32 Lerp(Color32 a,Color32 b,float t)=>a;}public struct UIVertex{public Vector3 position;public Vector4 uv0,uv1;public Color32 color;}}
namespace FirstJobber {public class AtmosphereSpriteBinding:UnityEngine.MonoBehaviour{}public class AtmosphereOverlayMask:UnityEngine.MonoBehaviour{public AtmosphereSpriteBinding Binding;public void Configure(AtmosphereSpriteBinding b){Binding=b;}}}
'''

(T/'Support.cs').write_text(s,encoding='utf-8')
# Compile the actual production remnant-filter method, with real ChoiceManager/depletion calculations.
resume=(R/'Assets/Scripts/Resume/ResumeEditorController.cs').read_text();start=resume.index('        void ApplyRemnantPresentation()');end=resume.index('\n        /// <summary>',start);method=resume[start:end]
probe='''using System;using System.Collections.Generic;using FirstJobber;class RemnantsProbe {class Context{public ChoiceManager Choices;public GameDatabase Database;}Context ctx;UICorruptionSettings corruptionSettings;ResumeCategoryData current;List<ResumeChoiceData> choiceBuffer=new List<ResumeChoiceData>();\n'''+method+'''
public static void Test(UICorruptionSettings settings){var db=new GameDatabase();var cat=new ResumeCategoryData{id="cat"};db.categories.Add(cat);db.companies.Add(new CompanyData{id="company"});var a=new ResumeChoiceData{id="a",category=cat};var b=new ResumeChoiceData{id="b",category=cat};db.choices.Add(a);db.choices.Add(b);var session=new GameSession();session.consumedChoiceIds.Add("a");var manager=new ChoiceManager(db,session);var p=new RemnantsProbe{ctx=new Context{Choices=manager,Database=db},current=cat,corruptionSettings=settings};manager.GetVisibleChoices(cat,p.choiceBuffer);settings.showConsumedRemnants=true;p.ApplyRemnantPresentation();if(p.choiceBuffer.Count!=2||manager.CountAvailable(cat)!=1||manager.SelectChoice(a))throw new Exception("remnants altered availability");p.ApplyRemnantPresentation();if(p.choiceBuffer.Count!=2)throw new Exception("duplicate remnant");if(!manager.SelectChoice(b)||!manager.CanConfirm())throw new Exception("available choice/confirm broken");settings.showConsumedRemnants=false;p.ApplyRemnantPresentation();if(p.choiceBuffer.Count!=1||p.choiceBuffer[0]!=b||session.consumedChoiceIds.Count!=1)throw new Exception("hide changed selectable order/depletion");session.consumedChoiceIds.Add("b");manager.GetVisibleChoices(cat,p.choiceBuffer);settings.showConsumedRemnants=true;p.ApplyRemnantPresentation();if(p.choiceBuffer.Count!=2||manager.CanConfirm()||manager.HasValidApplication()||manager.CountAvailable(cat)!=0)throw new Exception("all-consumed visuals prevent correct ending eligibility");}}
'''
(T/'RemnantsProbe.cs').write_text(probe,encoding='utf-8')
fixture='using FirstJobber;using UnityEngine;static class AuthoredCorruption {public static UICorruptionSettings Create(){var s=new UICorruptionSettings();'
for k in ['masterIntensity','maximumGlitchFrequency','availableMultiplier','selectedMultiplier','consumedMultiplier','disabledMultiplier','consumedRemnantOpacity']:fixture+=f's.{k}={float(cfg[k])}f;'
for p in cfg['phases']:
 fixture+='s.phases.Add(new UICorruptionSettings.PhaseStyle{phase=new AtmospherePhaseData(),'+','.join(k+'='+str(float(p[k]))+'f' for k in ['intensity','burstDuration','textOffset','ghostOpacity','jitterPixels','redactionStrength','horizontalTearStrength','horizontalTearFrequency'])+f",ghostLayers={p['ghostLayers']},burstComplexity={p['burstComplexity']},burstDurationRange=new Vector2({p['burstDurationRange']['x']}f,{p['burstDurationRange']['y']}f),burstInterval=new Vector2({p['burstInterval']['x']}f,{p['burstInterval']['y']}f)"+'});'
fixture+='return s;}}';(T/'AuthoredCorruption.cs').write_text(fixture)
D=Path('C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data');mono=D/'MonoBleedingEdge/bin/mono.exe';out=T/'RuntimeHarness.exe'
files=['UI/UICorruptionSettings.cs','UI/UICorruptionSurface.cs','UI/UIInkCorruptionGraphic.cs','UI/UIHorizontalTearGraphic.cs','UI/UIVisualMotion.cs','UI/UIAnimationSettings.cs','Core/ChoiceManager.cs','Core/ResumeConfidenceCalculator.cs']
subprocess.run([str(mono),str(D/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/langversion:9','/nowarn:0649,0067','/out:'+str(out),str(T/'Support.cs'),str(T/'RemnantsProbe.cs'),str(T/'AuthoredCorruption.cs'),str(Path(__file__).with_name('RuntimeHarness.cs')),*[str(R/'Assets/Scripts'/p) for p in files]],check=True)
subprocess.run([str(mono),str(out)],check=True)
