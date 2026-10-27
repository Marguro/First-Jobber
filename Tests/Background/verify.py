"""Compact GPU wallpaper: scene/material wiring and real controller tests with Unity API test doubles.
Requires Python/PyYAML and the Unity 6000.6.7f1 bundled Mono compiler. No Play Mode claim.
"""
from pathlib import Path
import re
import subprocess
import yaml

ROOT = Path(__file__).resolve().parents[2]


def documents(text):
    ids = re.findall(r"^--- !u!\d+ &(-?\d+)", text, re.M)
    text = re.sub(r"^%.*\n", "", text, flags=re.M)
    text = re.sub(r"^--- !u!\d+ &-?\d+.*$", "---", text, flags=re.M)
    return dict(zip(map(int, ids), yaml.safe_load_all(text)))


def sprite(file):
    meta = yaml.safe_load((ROOT / "Assets/Sprites/UI/Background" / (file + ".meta")).read_text())
    part = max(meta["TextureImporter"]["spriteSheet"]["sprites"], key=lambda s: s["rect"]["width"] * s["rect"]["height"])
    return dict(fileID=part["internalID"], guid=meta["guid"], type=3)


scene_path = ROOT / "Assets/Scenes/Game.unity"
scene = documents(scene_path.read_text(encoding="utf-8"))
old_scene = documents(subprocess.check_output(["git", "show", "HEAD:Assets/Scenes/Game.unity"], cwd=ROOT).decode("utf-8"))
for object_id in old_scene:
    if object_id not in [1773278458, 1773278459, 1773278461, 1773278462]:
        assert scene[object_id] == old_scene[object_id], "Unrelated scene object changed: " + str(object_id)
assert scene[627569008]["RectTransform"]["m_Children"][0]["fileID"] == 1773278458
base = scene[1773278459]["MonoBehaviour"]
assert base["m_Sprite"] == sprite("BG_Base.png") and base["m_RaycastTarget"] == 0
assert base["m_Color"] == dict(r=1, g=1, b=1, a=1)
assert scene[1773278462]["MonoBehaviour"]["m_Padding"] == dict(x=0,y=0,z=0,w=0)
controller = scene[1773278461]["MonoBehaviour"]
script = yaml.safe_load((ROOT / "Assets/Scripts/UI/ScrollingBackground.cs.meta").read_text())["guid"]
assert controller["m_Script"]["guid"] == script and controller["mode"] == 3
assert len(controller["motifSlots"]) == 4
assert scene[1773278458]["RectTransform"]["m_Children"] == [dict(fileID=910400002)]
assert len(scene[910400002]["RectTransform"]["m_Children"]) == 4
assert scene[910400003]["Canvas"]["m_OverrideSorting"] == 0
assert scene[910400003]["Canvas"]["m_ReceivesEvents"] == 0
shader_path = ROOT / "Assets/Shaders/UI/MotifWallpaper.shader"
shader_guid = yaml.safe_load(Path(str(shader_path)+".meta").read_text())["guid"]
assert controller["wallpaperShader"]["guid"] == shader_guid
assert "frac(pixel / _CellSize.xy + 0.5)" in shader_path.read_text()
assert isinstance(controller["speed"], (int, float))
assert set(controller["diagonalDirection"]) == {"x", "y"}
assert controller["enableScrolling"] == 1 and controller["enableProgressionIntegration"] == 1
assert controller["patternTileSize"] == dict(x=960,y=640)
for i, (slot, number) in enumerate(zip(controller["motifSlots"], [4, 1, 2, 3])):
    assert slot["motif"] == i and slot["isBlurred"] == 0
    assert slot["normalSprite"] == sprite(f"ICON_{number}.png")
    assert slot["blurredSprite"] == sprite(f"ICON_F_{number}.png")
    image = scene[slot["image"]["fileID"]]["MonoBehaviour"]
    assert image["m_Texture"]["guid"] == slot["normalSprite"]["guid"]
    assert image["m_EditorClassIdentifier"].endswith("::UnityEngine.UI.RawImage")
    raw_meta = next((ROOT/"Library/PackageCache").glob("com.unity.ugui*/Runtime/UGUI/UI/Core/RawImage.cs.meta"))
    assert image["m_Script"]["guid"] == yaml.safe_load(raw_meta.read_text())["guid"]
    assert image["m_RaycastTarget"] == 0
    material_path = ROOT / "Assets/Materials/Background" / (["Heart","Flower","Star","Cloud"][i]+"Wallpaper.mat")
    material = documents(material_path.read_text())[2100000]["Material"]
    assert material["m_Shader"]["guid"] == shader_guid
    assert image["m_Material"]["guid"] == yaml.safe_load(Path(str(material_path)+".meta").read_text())["guid"]
    rt = scene[910300002+i*10]["RectTransform"]
    assert rt["m_Father"]["fileID"] == 910400002
    assert rt["m_AnchorMin"] == dict(x=0,y=0) and rt["m_AnchorMax"] == dict(x=1,y=1)
    assert rt["m_SizeDelta"] == dict(x=0,y=0)
    assert image["m_Color"] == dict(r=1, g=1, b=1, a=1)
    assert slot["elementSize"]["x"] > 0 and slot["elementSize"]["y"] > 0
for file, count in [("Phase_01_Normal", 0), ("Phase_02_Deterioration", 2), ("Phase_03_Despair", 4)]:
    path = ROOT / "Assets/Data/Atmosphere" / (file + ".asset")
    phase = documents(path.read_text(encoding="utf-8"))[11400000]["MonoBehaviour"]
    assert phase["blurredBackgroundElements"] == count
    assert phase["m_Script"]["guid"] == yaml.safe_load((ROOT / "Assets/Scripts/Atmosphere/AtmospherePhaseData.cs.meta").read_text())["guid"]
    before = documents(subprocess.check_output(["git", "show", "HEAD:" + path.relative_to(ROOT).as_posix()], cwd=ROOT).decode("utf-8"))[11400000]["MonoBehaviour"]
    assert {k: v for k, v in phase.items() if k != "blurredBackgroundElements"} == {k: v for k, v in before.items() if k != "blurredBackgroundElements"}
config_path = ROOT / "Assets/Data/Atmosphere/AtmosphereConfiguration.asset"
assert scene[217966938]["MonoBehaviour"]["atmosphere"]["fileID"] == 217966943
assert scene[217966943]["MonoBehaviour"]["configuration"]["guid"] == yaml.safe_load(Path(str(config_path) + ".meta").read_text())["guid"]
config = documents(config_path.read_text(encoding="utf-8"))[11400000]["MonoBehaviour"]
expected_phases = {yaml.safe_load((ROOT / "Assets/Data/Atmosphere" / (name + ".asset.meta")).read_text())["guid"]
                   for name in ["Phase_01_Normal", "Phase_02_Deterioration", "Phase_03_Despair"]}
assert {phase["guid"] for phase in config["phases"]} == expected_phases
print("PASS foreground/IDs preserved; six background objects, four fixed RawImages on an isolated Canvas, shader/material/texture references, no raycast targets; existing phase integration.", flush=True)

temp = ROOT / "Temp/BackgroundTests"
temp.mkdir(parents=True, exist_ok=True)
fixture = ['using FirstJobber; using UnityEngine; using UnityEngine.UI;',
           'static class SlotFixture { public static BackgroundMotifSlot[] Create() { var slots = new BackgroundMotifSlot[4];']
for i, (slot, number) in enumerate(zip(controller["motifSlots"], [4,1,2,3])):
    width, height = slot["elementSize"]["x"], slot["elementSize"]["y"]
    fixture.append(f'var obj{i} = new GameObject("motif{i}",typeof(RectTransform),typeof(RawImage));')
    fixture.append(f'obj{i}.GetComponent<RectTransform>().sizeDelta = new Vector2(1920,1080);')
    expressions = []
    for file in [f"ICON_{number}.png",f"ICON_F_{number}.png"]:
        meta = yaml.safe_load((ROOT/"Assets/Sprites/UI/Background"/(file+".meta")).read_text())
        part = max(meta["TextureImporter"]["spriteSheet"]["sprites"],key=lambda x:x["rect"]["width"]*x["rect"]["height"])
        r = part["rect"]
        expressions.append(f'new Sprite {{ name = "{meta["guid"]}", texture = new Texture2D(400,400), textureRect = new Rect({r["x"]},{r["y"]},{r["width"]},{r["height"]}) }}')
    fixture.append(f'slots[{i}] = new BackgroundMotifSlot {{ motif = (BackgroundMotif){i}, image = obj{i}.GetComponent<RawImage>(), '
                   f'normalSprite = {expressions[0]}, blurredSprite = {expressions[1]}, '
                   f'elementSize = new Vector2({width}f,{height}f), '
                   f'startOffset = new Vector2({slot["startOffset"]["x"]}f,{slot["startOffset"]["y"]}f) }};')
    fixture.append(f'slots[{i}].image.texture = slots[{i}].normalSprite.texture; slots[{i}].image.material = new Material(new Shader());')
fixture.append('return slots; } }')
fixture_path = temp / "SlotFixture.cs"
fixture_path.write_text("\n".join(fixture), encoding="utf-8")
data = Path("C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data")
mono = data / "MonoBleedingEdge/bin/mono.exe"
csc = data / "MonoBleedingEdge/lib/mono/4.5/csc.exe"
exe = temp / "RuntimeHarness.exe"
subprocess.run([str(mono), str(csc), "/nologo", "/langversion:9", "/nowarn:0649", "/out:" + str(exe),
                str(Path(__file__).with_name("RuntimeHarness.cs")), str(fixture_path),
                str(ROOT / "Assets/Scripts/UI/ScrollingBackground.cs")], cwd=ROOT, check=True)
subprocess.run([str(mono), str(exe)], cwd=ROOT, check=True)
