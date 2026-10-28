"""Production Interview UI wiring/art/layout audit; native visual/raycast checks require Unity."""
from pathlib import Path
import re,yaml,subprocess
R=Path(__file__).resolve().parents[2]
def docs(path):
 s=path.read_text(encoding='utf-8-sig');ids=re.findall(r'^--- !u!\d+ &(-?\d+)',s,re.M);assert len(ids)==len(set(ids)),path
 s=re.sub(r'^%.*\n','',s,flags=re.M);s=re.sub(r'^--- !u!\d+ &-?\d+.*$','---',s,flags=re.M);return dict(zip(map(int,ids),yaml.safe_load_all(s)))
def guid(p):return yaml.safe_load(Path(str(p)+'.meta').read_text())['guid']
def sprite(p):
 m=yaml.safe_load(Path(str(R/p)+'.meta').read_text());z=max(m['TextureImporter']['spriteSheet']['sprites'],key=lambda z:z['rect']['width']*z['rect']['height']);return dict(fileID=z['internalID'],guid=m['guid'],type=3)
s=docs(R/'Assets/Scenes/Game.unity');p=docs(R/'Assets/Prefabs/UI/InterviewAnswerButton.prefab');b=R/'.utmp/interview-layout-before'
for fid,path in [(1633187481,'Assets/Sprites/UI/Revise My Resume.png'),(796213113,'Assets/Sprites/UI/Background/HR_BG 1.png')]:
 image=s[fid]['MonoBehaviour'];assert image['m_Sprite']==sprite(path) and image['m_PreserveAspect']==1 and image['m_RaycastTarget']==0 and image['m_Type']==0
assert s[1385924648]['MonoBehaviour']['itemPrefab']['guid']==guid(R/'Assets/Prefabs/UI/InterviewAnswerButton.prefab')
assert p[5205809760930852328]['MonoBehaviour']['m_Type']==1
view=p[2598443313786929471]['MonoBehaviour'];assert view['iconImage']['fileID']==0 and view['useAuthoredBackgroundColor']==1 and view['tintSelectedBackground']==0
assert p[5557010667304815370]['MonoBehaviour']['m_HorizontalAlignment']==2
assert not any('LayoutGroup' in (d.get('MonoBehaviour',{}).get('m_EditorClassIdentifier') or '') for d in p.values())
star=next(d['GameObject'] for d in p.values() if d.get('GameObject',{}).get('m_Name')=='DecorativeStar');assert star['m_IsActive']==1
for fid in [1396686514,1633187480,1385924647,796213110,1921029422,2140000031,2140000001]:assert s[fid]['RectTransform']['m_LocalScale']==dict(x=1,y=1,z=1)
hr=s[796213111]['MonoBehaviour'];assert hr['portrait']['fileID']==1921029423 and hr['swayTarget']['fileID']==2140000031
assert s[1921029422]['RectTransform']['m_Father']['fileID']==2140000031
assert s[1921029423]['MonoBehaviour']['m_Sprite']['fileID']==0 and s[1921029423]['MonoBehaviour']['m_PreserveAspect']==1
c=s[1396686516]['MonoBehaviour'];assert c['questionScroll']['fileID']==2140000005 and c['questionContent']['fileID']==2140000011
for fid in [1385924649,2140000005]:
 scroll=s[fid]['MonoBehaviour'];assert scroll['m_Vertical']==1 and scroll['m_Horizontal']==0
assert s[466925469]['RectTransform']['m_Father']==c['questionContent']
assert s[217966940]['MonoBehaviour']['interviewDialogueLayout']['fileID']==2140000102
profile=s[2140000102]['MonoBehaviour'];assert profile['paperSprite']==sprite('Assets/Sprites/UI/Dialog Box/Dialog Box_Line.png') and profile['dialogueRoot']['fileID']==977193194
assert s[2140000100]['GameObject']['m_IsActive']==0
assert profile['speakerMargins']==dict(x=0,y=0,z=0,w=0) and profile['speakerMinimumFontSize']==26
nameArea=s[2140000111]['RectTransform'];nameWidth=1840*(nameArea['m_AnchorMax']['x']-nameArea['m_AnchorMin']['x'])
assert nameWidth>530,'Speaker area still collapsed by the old 486px margin'
assert s[2140000200]['MonoBehaviour']['tokenizeThai']==0 and s[2140000200]['MonoBehaviour']['useProjectFont']==1
assert sum((d.get('MonoBehaviour',{}).get('m_EditorClassIdentifier') or '').endswith('DialogueUI') for d in s.values())==1
# The shared box is unchanged in serialized form; only the scoped helper applies its interview preset.
if (b/'Game.unity').exists():
 old=docs(b/'Game.unity')
 for fid in [977193194,977193195,977193197,2108387066,853240512,1450052839]:assert s[fid]==old[fid],'Shared non-interview dialogue layout changed'
changed=subprocess.check_output(['git','diff','--name-only'],cwd=R,text=True).splitlines();assert not any(x.startswith('Assets/Data/Production/') or x.startswith('Assets/Sprites/') for x in changed)
print('PASS actual Production Interview prefab/art, unit roots, masked independent question/answer scrolling, current-HR portrait binding, manual decorative star, one shared DialogueUI and scoped/restored interview preset; Production data/art untouched')
data=Path('C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data');out=R/'Temp/InterviewLayoutTests';out.mkdir(parents=True,exist_ok=True);exe=out/'RuntimeHarness.exe';mono=data/'MonoBleedingEdge/bin/mono.exe'
subprocess.run([str(mono),str(data/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/langversion:9','/nowarn:0649','/out:'+str(exe),str(Path(__file__).with_name('RuntimeHarness.cs')),str(R/'Assets/Scripts/Dialogue/InterviewDialogueLayout.cs')],check=True)
subprocess.run([str(mono),str(exe)],check=True)
print('UNVERIFIED: Unity Game View resemblance to InterviewMock, font wrapping/scroll hit-testing, portrait clipping/sway and shared dialogue restoration during real cross-fades at multiple resolutions.')
