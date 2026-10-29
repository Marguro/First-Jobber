"""Audit real Production references and MP4 copies; optionally verify a completed Web build.
Usage: python Tests/EndingPresentation/verify_web_packaging.py [--build PATH]
Does not run Unity or claim browser decoding/playback verification.
"""
from pathlib import Path
import argparse
import hashlib
import re
import yaml

ROOT = Path(__file__).resolve().parents[2]

def asset(path):
    text = path.read_text(encoding="utf-8-sig")
    text = re.sub(r"^%.*\n", "", text, flags=re.M)
    text = re.sub(r"^--- !u!\d+ &-?\d+.*$", "---", text, flags=re.M)
    return yaml.safe_load(text)["MonoBehaviour"]

def digest(path):
    return hashlib.sha256(path.read_bytes()).digest()

parser = argparse.ArgumentParser()
parser.add_argument("--build", type=Path)
args = parser.parse_args()
db_path = ROOT / "Assets/Data/Production/GameDatabase_Production.asset"
db = asset(db_path)
db_guid = re.search(r"^guid: (\w+)", Path(str(db_path)+".meta").read_text(), re.M)[1]
assert "guid: " + db_guid in (ROOT / "Assets/Scenes/Game.unity").read_text(encoding="utf-8-sig")
lookup = {}
for folder in ["Assets/Data/Production/Endings", "Assets/Video"]:
    for meta in (ROOT / folder).rglob("*.meta"):
        match = re.search(r"^guid: (\w+)", meta.read_text(encoding="utf-8-sig"), re.M)
        if match:
            lookup[match[1]] = Path(str(meta)[:-5])
count = total = 0
for reference in db["endings"]:
    ending = asset(lookup[reference["guid"]])
    if ending["presentationMode"] != 1:
        print("NO VIDEO REQUIRED:", ending["id"], "mode=", ending["presentationMode"])
        continue
    source = lookup[ending["videoClip"]["guid"]]
    filename = ending["webVideoFileName"]
    packaged = ROOT / "Assets/StreamingAssets/EndingVideos" / filename
    assert filename == Path(filename).name and filename.lower().endswith(".mp4")
    assert packaged.is_file() and digest(source) == digest(packaged), ending["id"]
    data = packaged.read_bytes()
    assert b"avc1" in data and b"mp4a" in data, "Expected H.264/AAC MP4 sample entries"
    if args.build:
        folder = args.build / "StreamingAssets/EndingVideos"
        assert folder.is_dir() and filename in [p.name for p in folder.iterdir()], "Missing in Web build: " + filename
        assert digest(folder / filename) == digest(packaged), "Build MP4 is stale: " + filename
    count += 1
    total += len(data)
    print("PASS:", ending["id"], source.name, len(data), "bytes; exact SHA256 match")
print("PASS:", count, "Production videos packaged;", total, "bytes total.")
print("Build files verified." if args.build else "NOT VERIFIED: newly generated Web build, HTTP delivery, codecs in a browser, video/audio playback.")
