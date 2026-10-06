"""Transfer approved Editor-authored documents while preserving unrelated data."""
from pathlib import Path
import hashlib
import json
import re
import shutil

destination = Path(__file__).resolve().parent
workspace = destination.parents[1]
preview = destination.parent / "drive-boss-validation/project"

def read(path):
    return path.read_bytes().decode("utf-8-sig").replace("\r\n", "\n")

def documents(text):
    matches = list(re.finditer(r"^--- !u!\d+ &(-?\d+)(?: stripped)?\n", text, re.MULTILINE))
    result = {}
    for index, match in enumerate(matches):
        identifier = int(match.group(1))
        if identifier in result:
            raise RuntimeError(f"Duplicate object ID {identifier}")
        result[identifier] = text[match.start(): matches[index + 1].start() if index + 1 < len(matches) else len(text)]
    return result

receipt = json.loads((destination / "authoring-receipt.json").read_text(encoding="utf-8-sig"))
prepared = []
reports = []
for asset in receipt["assets"]:
    path = asset["path"]
    before, after, live = map(read, (destination / "before" / path, destination / "after" / path, workspace / path))
    old, authored, current = map(documents, (before, after, live))
    approved = set(asset["modifiedIds"]) & set(old)
    added = set(authored) - set(old)
    if set(old) - set(authored):
        raise RuntimeError(f"Existing objects removed in {path}")
    if added & set(current):
        raise RuntimeError(f"New object ID conflict in {path}")
    for identifier in approved:
        if current.get(identifier) != old[identifier]:
            raise RuntimeError(f"Concurrent edit: {path}:{identifier}")
        if old[identifier] != authored[identifier]:
            live = live.replace(old[identifier], authored[identifier], 1)
    changed = [i for i in approved if old[i] != authored[i]]
    live = live.rstrip("\n") + "\n" + "".join(authored[i] for i in authored if i in added)
    final = documents(live)
    for identifier, block in current.items():
        if identifier not in approved and final.get(identifier) != block:
            raise RuntimeError(f"Unrelated object changed: {path}:{identifier}")
    if set(final) != set(current) | added:
        raise RuntimeError(f"Incorrect final object set: {path}")
    # Every new component must remain attached to its authored GameObject.
    for identifier in added:
        block = final[identifier]
        game_object = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", block)
        if game_object:
            owner = int(game_object.group(1))
            if owner not in final or f"component: {{fileID: {identifier}}}" not in final[owner]:
                raise RuntimeError(f"New component lost its owner: {path}:{identifier}")
    prepared.append((workspace / path, live))
    reports.append({"path": path, "modified": sorted(changed), "addedDocuments": len(added),
                    "preservedDocuments": len(current) - len(changed),
                    "ignoredNormalization": sum(old[i] != authored[i] for i in old if i not in approved)})

new_files = []
for relative in ("Assets/Sprites/UI/Driving/PickupOutline.mat", "Assets/Sprites/UI/Driving/PickupOutline.mat.meta"):
    source = preview / relative
    target = workspace / relative
    if not source.exists(): raise RuntimeError(f"New outline material is missing: {relative}")
    if target.exists() and target.read_bytes() != source.read_bytes(): raise RuntimeError(f"Concurrent new asset conflict: {relative}")
    new_files.append((source,target))

for target, text in prepared:
    target.write_bytes(text.encode("utf-8"))
for source, target in new_files:
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, target)

(destination / "merge-result.json").write_text(json.dumps(reports, indent=2), encoding="utf-8")
print(json.dumps({"assets": len(reports), "modifiedDocuments": sum(len(r["modified"]) for r in reports),
                  "addedDocuments": sum(r["addedDocuments"] for r in reports), "newAssetFiles": len(new_files)}))
