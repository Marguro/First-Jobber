"""Section prefab ownership/integration audit and real C# pooling/font/height tests; not Unity Play Mode."""
from pathlib import Path
import collections,re,subprocess,yaml
R=Path(__file__).resolve().parents[2]
def docs(path):
 s=path.read_text(encoding='utf-8-sig');ids=re.findall(r'^--- !u!\d+ &(-?\d+)',s,re.M)
 assert len(ids)==len(set(ids)),str(path)+' duplicate file IDs'
 s=re.sub(r'^%.*\n','',s,flags=re.M);s=re.sub(r'^--- !u!\d+ &-?\d+.*$','---',s,flags=re.M)
 return dict(zip(map(int,ids),yaml.safe_load_all(s)))
def guid(p):return re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1]
roles={'ResumeCategoryButton':1192156551,'ResumeChoiceButton':2107588837,'CompanySelectionButton':103586512,'InterviewAnswerButton':1385924648,'EventChoiceButton':2106000002,'EndingChoiceButton':411229344,'EndingActionButton':None}
guids={};source=R/'Assets/Prefabs/ChoiceButton.prefab';base=docs(source)
for role,listid in roles.items():
 p=R/'Assets/Prefabs/UI'/(role+'.prefab');d=docs(p);guids[role]=guid(p)
 assert all('PrefabInstance' not in x for x in d.values()),'Unexpected base-prefab coupling'
 assert d[6690180372980020888]['GameObject']['m_Name']==role
 assert d[833558695849796019]['RectTransform']['m_LocalScale']==dict(x=1,y=1,z=1)
 assert all(not (x.get('MonoBehaviour',{}).get('m_EditorClassIdentifier') or '').endswith(('HorizontalLayoutGroup','VerticalLayoutGroup','ContentSizeFitter')) for x in d.values()),role+' internal auto layout'
 assert sum((x.get('MonoBehaviour',{}).get('m_EditorClassIdentifier') or '').endswith('LayoutElement') for x in d.values())==1
 view=d[2598443313786929471]['MonoBehaviour'];assert view['button']['fileID']==450245967780818654 and view['useProjectLocalizationFont']==0
 assert d[5205809760930852328]['MonoBehaviour']['m_RaycastTarget']==0 and d[910200004]['MonoBehaviour']['m_RaycastTarget']==1
 assert d[358968367170934732]['MonoBehaviour']['m_LayoutPriority']==0
 assert d[910300001]['MonoBehaviour']['m_Script']['guid']==guid(R/'Assets/Scripts/UI/ChoiceButtonContentHeight.cs')
 assert d[7783337574798386231]['MonoBehaviour']['targetId']==role
 if role!='CompanySelectionButton':assert d[910100004]['MonoBehaviour']['m_PreserveAspect']==1
 assert view['confidenceStarSettings']==base[2598443313786929471]['MonoBehaviour']['confidenceStarSettings']
 for obj in d.values():
  text=obj.get('MonoBehaviour',{})
  if text.get('m_fontAsset',{}).get('guid')=='c00c48ceb5c5e64488bfa9ff66cf2288':
   assert text['m_sharedMaterial']['fileID']==-5161746986844279274
 # Local file references must stay inside this standalone prefab; script/font/sprite refs carry GUIDs.
 def check(node):
  if isinstance(node,dict):
   if 'fileID'in node and not node.get('guid') and node['fileID']:assert node['fileID'] in d,(role,node)
   for v in node.values():check(v)
  elif isinstance(node,list):
   for v in node:check(v)
 check(d)
assert len(set(guids.values()))==7 and guid(source) not in guids.values()
scene=docs(R/'Assets/Scenes/Game.unity')
category=docs(R/'Assets/Prefabs/UI/ResumeCategoryButton.prefab')
categoryView=category[2598443313786929471]['MonoBehaviour']
assert categoryView['useAuthoredBackgroundColor']==1 and categoryView['tintSelectedBackground']==0
assert category[910300001]['MonoBehaviour']['growForWrappedText']==0
assert category[5297326378183541114]['RectTransform']['m_AnchorMin']['y']==0
assert category[5557010667304815370]['MonoBehaviour']['m_VerticalAlignment']==512
# Scene geometry is artist-authored. Company-only changes must not rewrite later Resume layout edits.
for role,listid in roles.items():
 if listid:assert scene[listid]['MonoBehaviour']['itemPrefab']==dict(fileID=2598443313786929471,guid=guids[role],type=3)
assert scene[1172304822]['MonoBehaviour']['actionButtonPrefab']['guid']==guids['EndingActionButton']
assert guid(source) not in (R/'Assets/Scenes/Game.unity').read_text(encoding='utf-8'),'Legacy prefab still used by Production scene'
company=scene[445826512]['MonoBehaviour'];scroll=scene[company['detailsScroll']['fileID']]['MonoBehaviour']
assert scroll['m_Content']==company['detailsContent'] and scroll['m_Viewport']['fileID'] in scene
assert scene[1817045654]['RectTransform']['m_Father']==company['detailsContent']
company_code=(R/'Assets/Scripts/Companies/CompanySelectionController.cs').read_text()
assert all(word not in company_code for word in ['new GameObject(','.anchorMin =','.anchorMax =','.offsetMin =','.offsetMax =','.sizeDelta ='])
preview=docs(R/'Assets/Editor/UI/SectionButtonsPreview.unity')
instances=[x['PrefabInstance'] for x in preview.values() if 'PrefabInstance'in x]
assert collections.Counter(x['m_SourcePrefab']['guid'] for x in instances)==collections.Counter({g:3 for g in guids.values()})
assert preview[627569010]==scene[627569010],'CanvasScaler differs from Production'
assert len(preview[330000002]['MonoBehaviour']['sectionContainers'])==7 and len(preview[330000002]['MonoBehaviour']['samples'])==21
assert not any(any(k in (x.get('MonoBehaviour',{}).get('m_EditorClassIdentifier') or '') for k in ['GameFlowManager','DialogueManager','DailyEventManager','InterviewController']) for x in preview.values())
assert 'SectionButtonsPreview' not in (R/'ProjectSettings/EditorBuildSettings.asset').read_text()
backup=R/'.utmp/section-buttons-before'
if backup.exists():assert source.read_bytes()==(backup/source.name).read_bytes(),'Legacy shared prefab modified'
print('PASS seven standalone visual prefabs, manual internals/unit scales, six Production list refs + independent ending actions, existing star mapping, authored description scroll, 21 linked preview instances, exact CanvasScaler and no preview gameplay/build entry')
temp=R/'Temp/SectionButtonTests';temp.mkdir(parents=True,exist_ok=True)
data=Path('C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data');mono=data/'MonoBleedingEdge/bin/mono.exe';out=temp/'RuntimeHarness.exe'
scripts=['UI/ChoiceListUI.cs','UI/ChoiceButtonContentHeight.cs','Localization/ThaiTextApplier.cs','Localization/UIFontPolicy.cs']
subprocess.run([str(mono),str(data/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/langversion:9','/nowarn:0649','/out:'+str(out),str(Path(__file__).with_name('RuntimeHarness.cs')),*[str(R/'Assets/Scripts'/p) for p in scripts]],check=True)
subprocess.run([str(mono),str(out)],check=True)
print('UNVERIFIED: native Unity responsive layout/visuals, mouse raycasts, all Production flows and Play Mode. Use Tools > First Jobber > UI > Validate ChoiceButton Layout and the section preview scene.')
