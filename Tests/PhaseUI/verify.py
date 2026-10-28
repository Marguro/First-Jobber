"""Serialized Production phase-art integration and real C# policy tests. Native stencil rendering is NOT simulated."""
from pathlib import Path
import re,yaml,subprocess,collections
R=Path(__file__).resolve().parents[2]
def docs(path):
 raw=path.read_text(encoding='utf-8-sig');ids=re.findall(r'^--- !u!\d+ &(-?\d+)',raw,re.M);assert len(ids)==len(set(ids)),path
 raw=re.sub(r'^%.*\n','',raw,flags=re.M);raw=re.sub(r'^--- !u!\d+ &-?\d+.*$','---',raw,flags=re.M);return dict(zip(map(int,ids),yaml.safe_load_all(raw)))
def guid(p):return yaml.safe_load(Path(str(p)+'.meta').read_text())['guid']
libpath=R/'Assets/Data/Atmosphere/PhaseUISprites.asset';lib=docs(libpath)[11400000]['MonoBehaviour'];entries=lib['entries'];assert len(entries)==19
assert len({(e['original']['guid'],e['original']['fileID']) for e in entries})==19
assert lib['phase2']['guid']==guid(R/'Assets/Data/Atmosphere/Phase_02_Deterioration.asset') and lib['phase3']['guid']==guid(R/'Assets/Data/Atmosphere/Phase_03_Despair.asset')
byguid={guid(p):p for p in (R/'Assets/Sprites/UI').rglob('*.png')}
for e in entries:
 for key in ['original','phase2Sprite','phase3Sprite']:
  z=e[key]
  if not z['fileID']:continue
  path=byguid[z['guid']];sprites=yaml.safe_load(Path(str(path)+'.meta').read_text())['TextureImporter']['spriteSheet']['sprites'];assert z['fileID'] in [s['internalID'] for s in sprites]
 p=byguid[e['original']['guid']]
 if p.parent.name=='Answer Box':assert byguid[e['phase3Sprite']['guid']].name=='Anw Bog_Phase_3.png'
 if p.parent.name=='Work Bord' and p.stem in ['Job Bog','Job bog2','Job Bog3','Job Bog4','Job Bog5']:assert byguid[e['phase3Sprite']['guid']].name=='Job Bog_Phase_3.png'
scene=docs(R/'Assets/Scenes/Game.unity');bindguid=guid(R/'Assets/Scripts/Atmosphere/AtmosphereSpriteBinding.cs');maskguid=guid(R/'Assets/Scripts/Atmosphere/AtmosphereOverlayMask.cs')
paths=[R/'Assets/Scenes/Game.unity']+list((R/'Assets/Prefabs/UI').glob('*.prefab'));count=0;coverage=0;animated=0
for path in paths:
 d=docs(path);backup=R/'.utmp/phase-ui-before'/path.name
 if backup.exists():
  before=docs(backup)
  for fid,old in before.items():
   if 'RectTransform'in old or old.get('MonoBehaviour',{}).get('m_EditorClassIdentifier','') in ['UnityEngine.UI::UnityEngine.UI.Image','UnityEngine.UI::UnityEngine.UI.RawImage']:
    assert d[fid]==old,'Phase setup changed layout/art/authored Image settings: '+str(path)+':'+str(fid)
 targets=set();maskgos=set()
 for fid,z in d.items():
  c=z.get('MonoBehaviour',{});g=c.get('m_Script',{}).get('guid')
  if g==bindguid and c.get('uiSprites',{}).get('guid')==guid(libpath):
   assert c['image']['fileID'] not in targets,'Duplicate bindings';targets.add(c['image']['fileID']);count+=1
   assert c['baseSprite']['fileID'] and c['m_Enabled']==1
   if c['overlayMask']['fileID']:assert d[c['overlayMask']['fileID']]['MonoBehaviour']['binding']['fileID']==fid
  if g==maskguid:
   assert c['m_GameObject']['fileID'] not in maskgos,'Duplicate coverage components';maskgos.add(c['m_GameObject']['fileID']);coverage+=1;animated+=c['syncAnimatedMaterial']
 assert len(d)==len(set(d))
assert count==23 and coverage==180 and animated==4,(count,coverage,animated)
assert scene[796213111]['MonoBehaviour']['portrait']['fileID'] not in {c.get('MonoBehaviour',{}).get('image',{}).get('fileID') for c in scene.values() if c.get('MonoBehaviour',{}).get('uiSprites')},'HR portrait got UI mapping'
manager=(R/'Assets/Scripts/Atmosphere/AtmosphereManager.cs').read_text();assert 'CompareFunction.NotEqual' in manager and 'AtmosphereOverlayMask.ProtectionBit' in manager
assert (R/'Assets/Shaders/UI/MotifWallpaper.shader').read_text().count('UNITY_UI_ALPHACLIP')>=2
changed=subprocess.check_output(['git','diff','--name-only'],cwd=R,text=True).splitlines();assert not any(p.startswith(('Assets/Sprites/','Assets/Data/Production/','Assets/Resources/Localization/')) for p in changed)
print('PASS 19 exact original identities; all imported references; 23 Production mappings, 180 scoped render-coverage graphics, 4 animated wallpaper syncs; unified Answer/Job rules; no layout/art/portrait/gameplay/localization changes or duplicate bindings')
data=Path('C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data');temp=R/'Temp/PhaseUITests';temp.mkdir(parents=True,exist_ok=True);exe=temp/'RuntimeHarness.exe';mono=data/'MonoBleedingEdge/bin/mono.exe'
subprocess.run([str(mono),str(data/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/langversion:9','/nowarn:0649','/out:'+str(exe),str(Path(__file__).with_name('RuntimeHarness.cs')),*[str(R/'Assets/Scripts/Atmosphere'/p) for p in ['PhaseUISpriteLibrary.cs','AtmosphereSpriteBinding.cs','AtmosphereOverlayMask.cs']]],check=True)
subprocess.run([str(mono),str(exe)],check=True)
print('UNVERIFIED: native Unity stencil/depth/alpha clipping, TMP submesh/fallback fonts, Canvas ordering, phase transitions/hover/scroll and real gameplay in Play Mode. Use AtmosphereManager.Preview with the existing phase assets.')
