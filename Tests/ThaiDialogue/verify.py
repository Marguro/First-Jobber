from pathlib import Path
import re,subprocess,yaml
R=Path(__file__).resolve().parents[2];D=Path('C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data');T=R/'Temp/ThaiDialogueTests';T.mkdir(parents=True,exist_ok=True)
def asset(p):
 s=p.read_text(encoding='utf-8-sig');s=re.sub(r'^%.*\n','',s,flags=re.M);s=re.sub(r'^--- !u!\d+ &-?\d+.*$','---',s,flags=re.M);return next(yaml.safe_load_all(s))['MonoBehaviour']
table=asset(R/'Assets/Resources/Localization/LocalizationTable_TH.asset')['entries']
for event,segment in [('event_day01_opening','opening'),('event_day01_resume_intro','resume_intro')]:
 key=f'event/{event}/segments/{segment}/lines/0/lines/0';entries=[e for e in table if e['key']==key];assert len(entries)==1 and entries[0]['thai']=='เช้าวันแรกของการเป็นเฟิร์สจ๊อบแจ๊บ!'
 e=asset(R/f'Assets/Data/Production/DailyEvents/{event}.asset');assert e['id']==event and e['segments'][0]['id']==segment
for path in ['Assets/Scenes/Game.unity','Assets/Resources/Localization/LocalizationTable_TH.asset','Assets/Data/Production/DailyEvents/event_day01_opening.asset','Assets/Data/Production/DailyEvents/event_day01_resume_intro.asset']:
 assert subprocess.run(['git','diff','--quiet','--',path],cwd=R).returncode==0,'Task must preserve authored data/layout: '+path
package=next((R/'Library/PackageCache').glob('com.phengine.thaitextcare@*'))/'Runtime/ThaiTextNurse'
files=[R/'Assets/Scripts'/f for f in ['Data/DialogueData.cs','Data/GenderData.cs','Dialogue/DialogueUI.cs','Dialogue/DialogueManager.cs']]+[package/'PhTokenizer.cs',package/'ThaiFontAdjuster/ThaiFontAdjuster.cs']
mono=D/'MonoBleedingEdge/bin/mono.exe';out=T/'ThaiDialogue.exe'
subprocess.run([str(mono),str(D/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'/nologo','/langversion:9','/define:DEVELOPMENT_BUILD','/nowarn:0649','/out:'+str(out),str(Path(__file__).with_name('RuntimeHarness.cs')),*map(str,files)],check=True)
subprocess.run([str(mono),str(out)],check=True)
print('PASS distinct event IDs/keys, exact full Thai value in both authored events, and no scene/font/localization changes. Native Game View, Nurse component lifecycle, glyph meshes/wrapping and Play Mode remain UNVERIFIED.')
