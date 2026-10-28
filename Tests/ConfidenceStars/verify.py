"""Static Unity asset checks + real C# view/mapping regression tests (Python/PyYAML and Unity Mono required)."""
from pathlib import Path
import re
import subprocess
import yaml

ROOT = Path(__file__).resolve().parents[2]


def documents(path):
    text = path.read_text(encoding="utf-8-sig")
    ids = re.findall(r"^--- !u!\d+ &(-?\d+)", text, re.M)
    text = re.sub(r"^%.*\n", "", text, flags=re.M)
    text = re.sub(r"^--- !u!\d+ &-?\d+.*$", "---", text, flags=re.M)
    return {int(i): d for i, d in zip(ids, yaml.safe_load_all(text))}


def guid(path):
    return yaml.safe_load(Path(str(path) + ".meta").read_text())["guid"]


settings_path = ROOT / "Assets/Data/UI/ConfidenceStarSettings.asset"
config = documents(settings_path)[11400000]["MonoBehaviour"]
assert config["m_Script"]["guid"] == guid(ROOT / "Assets/Scripts/UI/ConfidenceStarSettings.cs")
assert [config[f"level{i}Max"] for i in [5,4,3,2]] == [20,40,60,80]
for field, file in [(f"starLevel{i}", f"Star point_{i}.png") for i in range(1,6)] + [("defaultStar", "Confident_Star.png")]:
    path = ROOT / "Assets/Sprites/UI/Confident_Bar" / file
    assert path.exists()
    meta = yaml.safe_load(Path(str(path) + ".meta").read_text())
    main = max(meta["TextureImporter"]["spriteSheet"]["sprites"], key=lambda s: s["rect"]["width"] * s["rect"]["height"])
    assert config[field]["guid"] == meta["guid"] and config[field]["fileID"] == main["internalID"], file + " references a fragment/wrong sprite"
prefab_path = ROOT / "Assets/Prefabs/ChoiceButton.prefab"
prefab = documents(prefab_path)
stars = [(i,d["GameObject"]) for i,d in prefab.items() if d.get("GameObject",{}).get("m_Name") == "ConfidenceStar"]
assert len(stars) == 1
star_id, star = stars[0]
image = next(d["MonoBehaviour"] for d in prefab.values() if d.get("MonoBehaviour",{}).get("m_GameObject",{}).get("fileID") == star_id and d["MonoBehaviour"].get("m_EditorClassIdentifier", "").endswith("::UnityEngine.UI.Image"))
assert image["m_RaycastTarget"] == 0 and image["m_PreserveAspect"] == 1 and image["m_Color"] == dict(r=1,g=1,b=1,a=1)
layout = prefab[910100005]["MonoBehaviour"]
assert layout["m_MinWidth"] == layout["m_PreferredWidth"] == 48 and layout["m_FlexibleWidth"] == 0
assert prefab[833558695849796019]["RectTransform"]["m_Children"][-1]["fileID"] == 910100002
assert prefab[4329057189700137429]["MonoBehaviour"]["m_MinWidth"] == 0
assert 910100006 not in prefab, "Text mask would clip long wrapped labels; reserved columns provide separation"
view = prefab[2598443313786929471]["MonoBehaviour"]
assert view["confidenceStar"]["fileID"] == 910100004 and view["confidenceStarSettings"]["guid"] == guid(settings_path)
scene = (ROOT / "Assets/Scenes/Game.unity").read_text()
assert "guid: " + guid(ROOT / "Assets/Prefabs/UI/ResumeChoiceButton.prefab") in scene
controller = (ROOT / "Assets/Scripts/Resume/ResumeEditorController.cs").read_text()
assert "showConfidenceStar = true" in controller and "confidence = c.Confidence" in controller
print("PASS script/asset GUIDs, all six main sprite subassets, single star, reserved columns/wrapping, alpha/aspect/raycast and Production prefab binding", flush=True)

temp = ROOT / "Temp/ConfidenceStarTests"
temp.mkdir(parents=True, exist_ok=True)
choices = []
for path in (ROOT / "Assets/Data/Production/Resume/Choices").glob("*.asset"):
    choice = documents(path)[11400000]["MonoBehaviour"]
    choices.append(f'new ResumeChoiceData {{ id = "{choice["id"]}", confidencePercentage = {choice["confidencePercentage"]}f }}')
assert len(choices) == 15
fixture = temp / "AuthoredChoices.cs"
fixture.write_text("using FirstJobber; static class AuthoredChoices { public static ResumeChoiceData[] Create() => new[] {" + ",".join(choices) + "}; }", encoding="utf-8")
data = Path("C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data")
mono = data / "MonoBleedingEdge/bin/mono.exe"
csc = data / "MonoBleedingEdge/lib/mono/4.5/csc.exe"
sources = [ROOT / "Assets/Scripts" / p for p in ["UI/ConfidenceStarSettings.cs", "UI/ChoiceButtonUI.cs", "Core/ResumeConfidenceCalculator.cs", "Data/ResumeChoiceData.cs", "Data/DialogueData.cs"]]
exe = temp / "RuntimeHarness.exe"
subprocess.run([str(mono),str(csc),"/nologo","/langversion:9","/nowarn:0649", "/out:"+str(exe), str(Path(__file__).with_name("RuntimeHarness.cs")),str(fixture),*map(str,sources)], check=True,cwd=ROOT)
subprocess.run([str(mono),str(exe)],check=True,cwd=ROOT)
