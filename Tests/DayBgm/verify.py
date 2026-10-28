from pathlib import Path
import subprocess,re,yaml
r=Path(__file__).resolve().parents[2]
s=(r/'Assets/Scenes/Game.unity').read_text(encoding='utf-8-sig')
ids=re.findall(r'^--- !u!\d+ &(-?\d+)',s,re.M)
assert len(ids)==len(set(ids))
s=re.sub(r'^%.*\n','',s,flags=re.M);s=re.sub(r'^--- !u!\d+ &-?\d+.*$','---',s,flags=re.M)
d=dict(zip(map(int,ids),yaml.safe_load_all(s)))
assert d[217966938]['MonoBehaviour']['dayMusic']['fileID']==2189000001
assert d[762817071]['MonoBehaviour']['dayMusic']['fileID']==2189000001
assert d[2189000001]['MonoBehaviour']['sourceA']['fileID']==2189000002
for i in [2189000002,2189000003]:assert d[i]['AudioSource']['m_PlayOnAwake']==0 and d[i]['AudioSource']['m_audioClip']['fileID']==0
print('PASS Production flow/ending/controller/source references, unique scene IDs, no placeholder clips')
data=Path('C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data');mono=data/'MonoBleedingEdge/bin/mono.exe';out=r/'Temp/DayBgmTests.exe'
subprocess.run([str(mono),str(data/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/nowarn:0649','/out:'+str(out),str(Path(__file__).with_name('RuntimeHarness.cs')),str(r/'Assets/Scripts/Data/DayBgmConfiguration.cs'),str(r/'Assets/Scripts/Core/DayBgmController.cs')],check=True)
subprocess.run([str(mono),str(out)],check=True)
