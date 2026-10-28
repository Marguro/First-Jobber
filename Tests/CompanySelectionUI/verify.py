"""Production scene/art/localization audit + actual controller logic; not Unity Play Mode."""
from pathlib import Path
import re, yaml, subprocess
R=Path(__file__).resolve().parents[2]
def docs(path):
 s=path.read_text(encoding='utf-8-sig');ids=re.findall(r'^--- !u!\d+ &(-?\d+)',s,re.M)
 assert len(ids)==len(set(ids)),str(path)+' duplicate IDs'
 s=re.sub(r'^%.*\n','',s,flags=re.M);s=re.sub(r'^--- !u!\d+ &-?\d+.*$','---',s,flags=re.M)
 return dict(zip(map(int,ids),yaml.safe_load_all(s)))
def sprite(name):
 p=R/'Assets/Sprites/UI/Work Bord'/(name+'.png.meta');m=yaml.safe_load(p.read_text());s=m['TextureImporter']['spriteSheet']['sprites'][0]
 return dict(fileID=s['internalID'],guid=m['guid'],type=3)
s=docs(R/'Assets/Scenes/Game.unity');p=docs(R/'Assets/Prefabs/UI/CompanySelectionButton.prefab')
guid=yaml.safe_load((R/'Assets/Prefabs/UI/CompanySelectionButton.prefab.meta').read_text())['guid']
assert guid=='ecd0aa1f72824da584dd73a778b27adc','Existing company prefab GUID changed'
assert s[103586512]['MonoBehaviour']['itemPrefab']['guid']==guid
c=s[445826512]['MonoBehaviour'];assert c['backgroundVariants']==[sprite(n) for n in ['Job Bog','Job bog2','Job Bog3','Job Bog4','Job Bog5']]
for fid,name in [(2120000003,'Job_Bord'),(2120000013,'Woek Head'),(1617775026,'Job Description'),(1863792828,'Button_Back'),(1673392689,'Button_Apply')]:
 image=s[fid]['MonoBehaviour'];assert image['m_Sprite']==sprite(name) and image['m_Type']==0 and image['m_PreserveAspect']==1
 if name not in ['Button_Back','Button_Apply']:assert image['m_RaycastTarget']==0
assert c['companyNameText']['fileID']==2120000033
for rid in [445826510,2120000001,2120000011,103586511,390823690,1464922609,1617775025,2119000101,1863792826,1673392687]:assert s[rid]['RectTransform']['m_LocalScale']==dict(x=1,y=1,z=1)
for sid in [103586513,2119000107]:
 scroll=s[sid]['MonoBehaviour'];assert scroll['m_Vertical']==1 and scroll['m_Horizontal']==0 and scroll['m_Content']['fileID'] and scroll['m_Viewport']['fileID']
assert s[2120000023]['MonoBehaviour']['m_RaycastTarget']==1,'Description cannot receive wheel/drag events'
view=p[2598443313786929471]['MonoBehaviour'];assert view['subtitleText']['fileID']==0 and view['iconImage']['fileID']==0 and view['confidenceStar']['fileID']==0
texts=[d['MonoBehaviour'] for d in p.values() if 'TextMeshProUGUI' in (d.get('MonoBehaviour',{}).get('m_EditorClassIdentifier') or '')]
assert len(texts)==2 and p[5557010667304815370]['MonoBehaviour']['m_HorizontalAlignment']==1 and p[3556140069722006281]['MonoBehaviour']['m_HorizontalAlignment']==4
assert view['showBadge']==1 and view['useAuthoredBackgroundColor']==1 and view['tintSelectedBackground']==0 and view['selectionIndicator']['fileID'] in p
assert p[5205809760930852328]['MonoBehaviour']['m_PreserveAspect']==1 and p[910300001]['MonoBehaviour']['growForWrappedText']==0
assert not any('LayoutGroup' in (d.get('MonoBehaviour',{}).get('m_EditorClassIdentifier') or '') for d in p.values())
# All local prefab references must resolve after removal of unused icon/star/subtitle.
def refs(node):
 if isinstance(node,dict):
  if node.get('fileID') and not node.get('guid'):assert node['fileID'] in p,node
  for v in node.values():refs(v)
 elif isinstance(node,list):
  for v in node:refs(v)
refs(p)
table=docs(R/'Assets/Resources/Localization/LocalizationTable_TH.asset')[11400000]['MonoBehaviour']['entries'];entries={e['key']:e for e in table}
for key in ['ui/company/choose','ui/company/salary_not_specified','ui/label/GameCanvas/CompanyPanel/DetailsCard/BackButton/Label','ui/label/GameCanvas/CompanyPanel/DetailsCard/ApplyButton/Label']:
 assert sum(e['key']==key for e in table)==1 and entries[key]['english'] and entries[key]['thai']
assert entries['ui/company/salary_not_specified']['thai']=='ไม่ระบุเงินเดือน'
# Production CompanyData and original artwork/import settings are not part of this UI migration.
changed=subprocess.check_output(['git','diff','--name-only'],cwd=R,text=True).splitlines()
assert not any(x.startswith('Assets/Data/Production/') or x.startswith('Assets/Sprites/') for x in changed),changed
print('PASS active Production prefab GUID, all 10 real artwork references, unit UI scales, two independent scroll views, two-label manual card, subtle selection, valid references, EN/TH keys, untouched Production data/source art')
data=Path('C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data');temp=R/'Temp/CompanyUITests';temp.mkdir(parents=True,exist_ok=True);exe=temp/'RuntimeHarness.exe';mono=data/'MonoBleedingEdge/bin/mono.exe'
subprocess.run([str(mono),str(data/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/langversion:9','/nowarn:0649','/out:'+str(exe),str(Path(__file__).with_name('RuntimeHarness.cs')),str(R/'Assets/Scripts/Companies/CompanySelectionController.cs'),str(R/'Assets/Scripts/Localization/UiText.cs')],check=True)
subprocess.run([str(mono),str(exe)],check=True)
print('UNVERIFIED: native Unity appearance vs mockup, pointer/scroll routing, resolution changes, Apply dialogue → confidence → interview in Play Mode.')
