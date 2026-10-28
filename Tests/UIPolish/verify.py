"""Production UI safety audit + actual presentation code with API doubles; not Play Mode."""
from pathlib import Path
import re,yaml,subprocess
R=Path(__file__).resolve().parents[2]
def docs(p):
 s=p.read_text(encoding='utf-8-sig');ids=re.findall(r'^--- !u!\d+ &(-?\d+)',s,re.M)
 assert len(ids)==len(set(ids)),f'duplicate Unity fileID: {p}'
 s=re.sub(r'^%.*\n','',s,flags=re.M);s=re.sub(r'^--- !u!\d+ &-?\d+.*$','---',s,flags=re.M)
 return dict(zip(map(int,ids),yaml.safe_load_all(s)))
def guid(p):return re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1]
def data(p):return docs(p)[11400000]['MonoBehaviour']
scene=docs(R/'Assets/Scenes/Game.unity');anim=R/'Assets/Data/UI/UIAnimation_Production.asset';sfx=R/'Assets/Data/UI/UISfx_Production.asset'
assert scene[217966940]['MonoBehaviour']['animationSettings']['guid']==guid(anim)
sguid=guid(R/'Assets/Scripts/UI/UIButtonFeedback.cs');mguid=guid(R/'Assets/Scripts/UI/UIVisualMotion.cs');pguid=guid(R/'Assets/Scripts/UI/UISfxPlayer.cs')
players=[v['MonoBehaviour'] for v in scene.values() if v.get('MonoBehaviour',{}).get('m_Script',{}).get('guid')==pguid];assert len(players)==1
source=scene[players[0]['source']['fileID']]['AudioSource'];assert source['Loop']==0 and source['m_PlayOnAwake']==0
assert source['m_GameObject']['fileID']==217966937
for group,prefix in [('click','Click'),('transition','Transition'),('finishTask','FinishTask')]:
 assert [x['guid'] for x in data(sfx)[group]['clips']]==[guid(R/f'Assets/Casual Game Sounds U6/CasualGameSounds/{prefix}{i}.wav') for i in [1,2]]
assert [e['amount'] for e in data(anim)['phaseMotion']]==[1,.65,.35]
image_guid='fe87c0e1cc204ed48ad3b37840f39efc'
checkedrects=checkedimages=0
for p in [R/'Assets/Scenes/Game.unity',*(R/'Assets/Prefabs/UI').glob('*.prefab')]:
 current=docs(p);backup=R/'Temp/UIPolishBefore'/p.relative_to(R)
 if backup.exists():
  old=docs(backup)
  for i,before in old.items():
   assert i in current,'Existing serialized object removed'
   if 'RectTransform' in before:
    a=before['RectTransform'].copy();b=current[i]['RectTransform'].copy()
    if p.suffix=='.prefab':
     for key in ['m_Father','m_Children']:a.pop(key,None);b.pop(key,None)
    assert a==b,f'Authored RectTransform changed: {p}:{i}';checkedrects+=1
   m=before.get('MonoBehaviour',{})
   if m.get('m_Script',{}).get('guid')==image_guid:
    assert current[i]==before,f'Image art/color/material changed: {p}:{i}';checkedimages+=1
  # Scene overlay and wallpaper subtrees include no added components or property changes.
  if p.suffix=='.unity':
   goids={v['GameObject']['m_Name']:i for i,v in old.items() if 'GameObject' in v}
   roots=[i for n,i in goids.items() if n in ['AtmosphereOverlay','TransitionOverlay','WallpaperCanvas','Background']]
   todo=list(roots);protected=set()
   while todo:
    go=todo.pop()
    if go in protected:continue
    protected.add(go)
    for comp in old[go]['GameObject']['m_Component']:
     cid=comp['component']['fileID'];protected.add(cid)
     for k in ['RectTransform','Transform']:
      if k in old[cid]:
       for child in old[cid][k].get('m_Children',[]):todo.append(next(iter(old[child['fileID']].values()))['m_GameObject']['fileID'])
   for i in protected:assert current[i]==old[i],f'Protected background/overlay object changed: {i}'
  # Existing phase bindings and coverage masks remain exactly the same serialized objects.
  for i,before in old.items():
   m=before.get('MonoBehaviour',{});g=m.get('m_Script',{}).get('guid')
   for name in ['AtmosphereSpriteBinding','AtmosphereOverlayMask']:
    if g==guid(R/f'Assets/Scripts/Atmosphere/{name}.cs'):assert current[i]==before
 if p.suffix=='.prefab' and p.stem in ['ResumeCategoryButton','ResumeChoiceButton','CompanySelectionButton','InterviewAnswerButton','EventChoiceButton','EndingChoiceButton','EndingActionButton']:
  feedback=[v['MonoBehaviour'] for v in current.values() if v.get('MonoBehaviour',{}).get('m_Script',{}).get('guid')==sguid];assert len(feedback)==1
  v=current[feedback[0]['visual']['fileID']]['RectTransform'];assert v['m_SizeDelta']=={'x':0,'y':0} and v['m_AnchorMin']=={'x':0,'y':0} and v['m_AnchorMax']=={'x':1,'y':1}
  assert current[v['m_GameObject']['fileID']]['GameObject']['m_Name']=='VisualRoot'
  assert current[feedback[0]['entranceGroup']['fileID']]['CanvasGroup']['m_IgnoreParentGroups']==0
flow=scene[217966938]['MonoBehaviour'];music=scene[flow['dayMusic']['fileID']]['MonoBehaviour'];assert music['configuration']['guid']==guid(R/'Assets/Data/Production/DayBgm_Production.asset')
for path in ['Assets/Scripts/Core/GameFlowManager.cs','Assets/Scripts/Core/DayBgmController.cs','Assets/Scripts/Atmosphere/AtmosphereManager.cs','Assets/Scripts/UI/ScrollingBackground.cs']:
 assert subprocess.run(['git','diff','--quiet','--',path],cwd=R).returncode==0,f'Out-of-scope logic modified: {path}'
ui=(R/'Assets/Scripts/UI/UIManager.cs').read_text(encoding='utf-8-sig');original=subprocess.check_output(['git','show','HEAD:Assets/Scripts/UI/UIManager.cs'],cwd=R).decode('utf-8-sig')
assert ui[ui.index('        public IEnumerator Blackout'):]==original[original.index('        public IEnumerator Blackout'):], 'Transition overlay implementation changed'
print(f'PASS Production bindings, 7 independent prefab VisualRoots, six exact SFX references, {checkedrects} original RectTransform poses, {checkedimages} original Images; overlay/wallpaper/phase bindings and BGM preserved')
D=Path('C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data');mono=D/'MonoBleedingEdge/bin/mono.exe';out=R/'Temp/UIPolishTests.exe'
files=['UIAnimationSettings','UISfxSettings','UISfxPlayer','UIVisualMotion','UIButtonFeedback','UIPresentationAudio','UIManager']
subprocess.run([str(mono),str(D/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/langversion:9','/nowarn:0649','/out:'+str(out),str(Path(__file__).with_name('RuntimeHarness.cs')),*[str(R/f'Assets/Scripts/UI/{n}.cs') for n in files],str(R/'Assets/Scripts/Dialogue/DialogueUI.cs')],check=True)
subprocess.run([str(mono),str(out)],check=True)
