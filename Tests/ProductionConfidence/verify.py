"""Read-only Production content/connection audit plus standalone C# regression tests.

Requires Python + PyYAML and Unity's bundled Mono/C# compiler. Run from any directory:
  python Tests/ProductionConfidence/verify.py --unity-data "C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data"
The runtime harness uses Unity test doubles; this does not replace Unity Play Mode testing.
Only ignored Temp/ProductionConfidenceTests receives generated build files.
"""
import argparse
import collections
import csv
import json
from pathlib import Path
import re
import subprocess

import yaml

ROOT = Path(__file__).resolve().parents[2]
PRODUCTION = ROOT / "Assets/Data/Production"
COUNTS = [(3, 5), (1, 2), (2, 1), (2, 2), (3, 3)]


def load_documents(path):
    text = path.read_text(encoding="utf-8-sig")
    text = re.sub(r"^%.*\n", "", text, flags=re.M)
    text = re.sub(r"^--- !u!\d+ &-?\d+.*$", "---", text, flags=re.M)
    return list(yaml.safe_load_all(text))


def asset(path):
    return load_documents(path)[0]["MonoBehaviour"]


def guid(path):
    return re.search(r"^guid: (\w+)$", Path(str(path) + ".meta").read_text(), re.M)[1]


def verify():
    reference = ROOT / "DAY_CONFIDENCE_DIALOGUE_TH.md"
    authored = {}
    for day, branch, count, body in re.findall(
        r"## Day (\d+) . (High|Low) \((\d+)\)\s*(.*?)(?=\n## |\Z)",
        reference.read_text(encoding="utf-8"), re.S,
    ):
        lines = re.findall(r"^- Candidate (\d+): (.*)$", body, re.M)
        assert [int(i) for i, _ in lines] == list(range(int(count)))
        authored[int(day), branch.lower()] = [text for _, text in lines]
    assert len(authored) == 10, "Expected ten authored Day/High/Low pools"

    config_path = PRODUCTION / "ConfidenceDialogue_Production.asset"
    config = asset(config_path)
    db_path = PRODUCTION / "GameDatabase_Production.asset"
    db = asset(db_path)
    assert config["m_Script"]["guid"] == guid(ROOT / "Assets/Scripts/Data/ConfidenceDialogueDatabase.cs"), \
        "Production confidence asset has a missing/wrong MonoScript GUID"
    assert config["m_EditorClassIdentifier"] == "Assembly-CSharp::FirstJobber.ConfidenceDialogueDatabase"
    assert db["m_Script"]["guid"] == guid(ROOT / "Assets/Scripts/Data/GameDatabase.cs")
    assert db["requireDayBasedConfidence"] == 1, "Production must not fall back to legacy dialogue when its configuration is missing"
    assert db["confidenceDialogue"]["guid"] == guid(config_path)
    assert config["threshold"] == 50
    assert [d["day"] for d in config["days"]] == [1, 2, 3, 4, 5]

    table = asset(ROOT / "Assets/Resources/Localization/LocalizationTable_TH.asset")
    counts = collections.Counter(e["key"] for e in table["entries"])
    assert all(n == 1 for n in counts.values()), "Duplicate localization key"
    entries = {e["key"]: e for e in table["entries"]}
    with (ROOT / "Assets/Localization/translation_template.csv").open(encoding="utf-8-sig", newline="") as f:
        rows = list(csv.DictReader(f))
    csv_counts = collections.Counter(r["key"] for r in rows)
    csv_entries = {r["key"]: r for r in rows}
    seen_english, seen_thai, keys = set(), set(), set()
    for d in config["days"]:
        for branch, count in zip(("high", "low"), COUNTS[d["day"] - 1]):
            assert len(d[branch]) == count
            ids = [c["id"] for c in d[branch]]
            assert len(set(ids)) == count, "Duplicate candidate ID within pool"
            for i, c in enumerate(d[branch]):
                line = c["dialogue"]
                # Existing authored candidates are single-page legacy-text blocks.
                assert line["speaker"] == "Player"
                assert not line["dialogueLines"] and not line["genderIds"] and not line["variantGroup"]
                en = line["text"]
                assert en and not re.search(r"[\u0e00-\u0e7f]", en), "Missing English source"
                key = f"confidence/apply/day_{d['day']:02d}/{branch}/{c['id']}/text"
                entry = entries[key]
                assert entry["english"] == en, key + " English source mismatch"
                assert entry["thai"] == authored[d["day"], branch][i], key + " Thai differs from reference"
                assert en not in seen_english and entry["thai"] not in seen_thai, "Duplicate candidate text"
                assert csv_counts[key] == 1 and csv_entries[key]["english"] == en
                assert csv_entries[key]["thai"] == entry["thai"], key + " CSV mismatch"
                seen_english.add(en)
                seen_thai.add(entry["thai"])
                keys.add(key)
                print("PASS", key)
    assert len(keys) == 24

    scene_path = ROOT / "Assets/Scenes/Game.unity"
    scene_text = scene_path.read_text(encoding="utf-8")
    documents = load_documents(scene_path)
    ids = re.findall(r"^--- !u!\d+ &(-?\d+)", scene_text, re.M)
    objects = {int(i): doc for i, doc in zip(ids, documents)}

    def component(script):
        script_guid = guid(ROOT / script)
        found = [(i, doc["MonoBehaviour"]) for i, doc in objects.items()
                 if doc and doc.get("MonoBehaviour", {}).get("m_Script", {}).get("guid") == script_guid]
        assert len(found) == 1, "Expected one scene component: " + script
        return found[0]

    _, flow = component("Assets/Scripts/Core/GameFlowManager.cs")
    assert flow["m_Enabled"] == 1 and flow["database"]["guid"] == guid(db_path)
    for field, script in [("dialogue", "Dialogue/DialogueManager"), ("companySelection", "Companies/CompanySelectionController"),
                          ("resumeEditor", "Resume/ResumeEditorController"), ("interview", "Interview/InterviewController"),
                          ("dailyEvents", "Events/DailyEventManager")]:
        object_id, comp = component("Assets/Scripts/" + script + ".cs")
        assert flow[field]["fileID"] == object_id, "Disconnected flow component: " + field
        if field == "dailyEvents":
            assert comp["database"]["guid"] == guid(db_path)
            assert comp["dialogue"]["fileID"] == flow["dialogue"]["fileID"]
        if field == "companySelection":
            for name in ["applyButton", "companyList", "backButton"]:
                assert comp[name]["fileID"] in objects, "Missing company UI: " + name
        if field == "dialogue":
            assert comp["ui"]["fileID"] in objects, "Missing DialogueUI"
    for ref in db["companies"]:
        matches = [p for p in (PRODUCTION / "Companies").glob("*.asset") if guid(p) == ref["guid"]]
        assert len(matches) == 1
        company = asset(matches[0])
        hrs = [p for p in (PRODUCTION / "HR").glob("*.asset") if guid(p) == company["hr"]["guid"]]
        assert len(hrs) == 1 and asset(hrs[0])["introduction"], "Missing selected HR introduction"
    build = (ROOT / "ProjectSettings/EditorBuildSettings.asset").read_text()
    assert re.search(r"- enabled: 1\s+path: Assets/Scenes/Game.unity", build)
    print("PASS active Production scene -> GameDatabase -> confidence config; Apply/UI/dialogue/interview/HR references")
    return config


def runtime(config, unity_data):
    temp = ROOT / "Temp/ProductionConfidenceTests"
    temp.mkdir(parents=True, exist_ok=True)
    # Feed the real serialized candidate data into the real C# resolver, without loading Unity native APIs.
    lines = ["using FirstJobber;", "static class ProductionFixture { public static ConfidenceDialogueDatabase Create() {",
             f"var config = new ConfidenceDialogueDatabase {{ threshold = {config['threshold']}f }};"]
    for d in config["days"]:
        lines.append(f"var day{d['day']} = new ConfidenceDayConfig {{ day = {d['day']} }};")
        for branch in ("high", "low"):
            for c in d[branch]:
                text = json.dumps(c["dialogue"]["text"], ensure_ascii=True)
                lines.append(f'day{d["day"]}.{branch}.Add(new ConfidenceCandidate {{ id = "{c["id"]}", '
                             f'dialogue = new DialogueLine(Speakers.Player, {text}) }});')
        lines.append(f"config.days.Add(day{d['day']});")
    lines.append("return config; } }")
    fixture = temp / "ProductionFixture.cs"
    fixture.write_text("\n".join(lines), encoding="utf-8")
    mono = unity_data / "MonoBleedingEdge/bin/mono.exe"
    csc = unity_data / "MonoBleedingEdge/lib/mono/4.5/csc.exe"
    output = temp / "RuntimeHarness.exe"
    sources = [ROOT / "Assets/Scripts" / p for p in ["Data/GameDatabase.cs", "Data/GenderData.cs",
               "Data/DialogueData.cs", "Data/ConfidenceDialogueDatabase.cs", "Dialogue/DialogueManager.cs",
               "Localization/LocalizationKeys.cs"]]
    subprocess.run([str(mono), str(csc), "/nologo", "/langversion:9", "/nowarn:0649,0067",
                    "/out:" + str(output), str(Path(__file__).with_name("RuntimeHarness.cs")), str(fixture),
                    *map(str, sources)], check=True, cwd=ROOT)
    subprocess.run([str(mono), str(output)], check=True, cwd=ROOT)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity-data", type=Path, help="Unity Editor/Data directory; enables standalone C# checks")
    args = parser.parse_args()
    config = verify()
    if args.unity_data:
        runtime(config, args.unity_data)
    else:
        print("C# runtime checks NOT RUN (supply --unity-data).")
