"""Read-only serialized prefab audit. Does not import assets or execute Unity."""
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
PREFAB = ROOT / "Assets/Resources/PermanentUpgradeMenu.prefab"
text = PREFAB.read_text(encoding="utf-8-sig")
checks = 0


def check(condition, message):
    global checks
    checks += 1
    if not condition:
        raise AssertionError(message)


def scalar(body, name):
    match = re.search(r"^  " + re.escape(name) + r": (.*)$", body, re.M)
    check(match is not None, "Missing serialized field: " + name)
    return match.group(1).strip()


def ref(body, name):
    return int(re.search(r"fileID: (-?\d+)", scalar(body, name)).group(1))


def refs(body, name):
    match = re.search(r"^  " + re.escape(name) + r":\s*\n((?:  -[^\n]*\n)*)", body, re.M)
    check(match is not None, "Missing serialized reference array: " + name)
    return [int(value) for value in re.findall(r"fileID: (-?\d+)", match.group(1))]


def guid(path):
    return re.search(r"^guid: ([0-9a-f]{32})$", Path(path).read_text(encoding="utf-8-sig"), re.M).group(1)


headers = list(re.finditer(r"^--- !u!(\d+) &(-?\d+)\s*$", text, re.M))
objects = {}
for index, header in enumerate(headers):
    identifier = int(header.group(2))
    check(identifier not in objects, "Duplicate local object ID: " + str(identifier))
    end = headers[index + 1].start() if index + 1 < len(headers) else len(text)
    objects[identifier] = (int(header.group(1)), text[header.end():end])
check(bool(objects), "Prefab contains no serialized objects")
for identifier, (kind, body) in objects.items():
    for found in re.finditer(r"\{fileID: (-?\d+)([^}]*)\}", body):
        target = int(found.group(1))
        if target != 0 and "guid:" not in found.group(2):
            check(target in objects, f"Dangling local reference from {identifier} to {target}")

game_objects = {identifier: body for identifier, (kind, body) in objects.items() if kind == 1}
transforms = {identifier: body for identifier, (kind, body) in objects.items() if kind in (4, 224)}
transform_for_go = {ref(body, "m_GameObject"): identifier for identifier, body in transforms.items()}
for identifier, body in game_objects.items():
    check(identifier in transform_for_go, "GameObject lacks a Transform: " + str(identifier))
    for component in re.findall(r"component: \{fileID: (\d+)\}", body):
        check(ref(objects[int(component)][1], "m_GameObject") == identifier, "Component ownership mismatch")
for identifier, body in transforms.items():
    father = ref(body, "m_Father")
    if father:
        check(father in transforms, "Transform parent has wrong type")
        check(identifier in refs(transforms[father], "m_Children"), "Parent omits declared child")
    children_match = re.search(r"^  m_Children:\s*\n((?:  -[^\n]*\n)*)", body, re.M)
    children = [] if children_match is None else [int(value) for value in re.findall(r"fileID: (\d+)", children_match.group(1))]
    for child in children:
        check(ref(transforms[child], "m_Father") == identifier, "Child declares different parent")

meta_sources = subprocess.run(["rg", "-n", "-g", "*.meta", "^guid: [0-9a-f]{32}", "Assets", "Packages", "Library/PackageCache"], cwd=ROOT, text=True, capture_output=True, encoding="utf-8").stdout
meta_registry = {}
for line in meta_sources.splitlines():
    match = re.match(r"(.+):\d+:guid: ([0-9a-f]{32})$", line)
    if match:
        meta_registry.setdefault(match.group(2), []).append(match.group(1))
external_guids = set(re.findall(r"guid: ([0-9a-f]{32})", text))
for external in external_guids:
    if not external.startswith("0000000000000000"):
        check(external in meta_registry, "External asset/script GUID not found: " + external)
feature_metas = list((ROOT / "Assets/Scripts/LeeJunmo/PermanentUpgrades").glob("*.meta")) + [Path(str(PREFAB) + ".meta"), ROOT / "Assets/Scripts/LeeJunmo/PermanentUpgrades.meta"]
for meta in feature_metas:
    check(len(meta_registry.get(guid(meta), [])) == 1, "Feature GUID collision: " + str(meta))

menu_guid = guid(ROOT / "Assets/Scripts/LeeJunmo/PermanentUpgrades/PermanentUpgradeMenu.cs.meta")
card_guid = guid(ROOT / "Assets/Scripts/LeeJunmo/PermanentUpgrades/PermanentUpgradeCard.cs.meta")
menus = [(identifier, body) for identifier, (kind, body) in objects.items() if "guid: " + menu_guid in body]
cards = [(identifier, body) for identifier, (kind, body) in objects.items() if "guid: " + card_guid in body]
check(len(menus) == 1 and len(cards) == 10, "Prefab must contain one menu and ten card components")
menu = menus[0][1]
def component_name(identifier):
    found = re.search(r"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32})", objects[identifier][1])
    check(found is not None, "Required component lacks a script reference")
    return Path(meta_registry[found.group(1)][0]).name.removesuffix(".cs.meta")

for field in ("panel", "title", "souls", "message", "startLabel", "startButton", "tooltip", "tooltipTitle", "tooltipDescription", "tooltipStatus"):
    target = ref(menu, field)
    check(target != 0 and target in objects, "Required menu reference missing: " + field)
    if field in ("panel", "tooltip"):
        check(objects[target][0] == 224, "Menu RectTransform reference has wrong type: " + field)
    else:
        check(component_name(target) == ("Button" if field == "startButton" else "TextMeshProUGUI"), "Menu component reference has wrong type: " + field)
card_refs = refs(menu, "cards")
check(len(card_refs) == 10 and set(card_refs) == {identifier for identifier, _ in cards}, "Menu card array omits or duplicates a card")
expected_nodes = {"fuel": 5, "damage": 5, "weapon.laser": 1, "weapon.buckshot": 1, "weapon.machinegun": 1, "boss.train": 1, "boss.tentacle": 1, "ultimate": 3, "dash.duration": 3, "dash.speed": 3}
seen_nodes = []
segments_total = 0
for identifier, card in cards:
    node_id = scalar(card, "nodeId")
    seen_nodes.append(node_id)
    check(node_id in expected_nodes, "Unknown serialized node ID: " + node_id)
    for field in ("purchaseButton", "nameLabel", "costLabel", "statusLabel", "accent"):
        check(ref(card, field) in objects, "Card reference missing: " + field)
        expected_type = "Button" if field == "purchaseButton" else "Image" if field == "accent" else "TextMeshProUGUI"
        check(component_name(ref(card, field)) == expected_type, "Card component reference has wrong type: " + field)
    segments = refs(card, "progressSegments")
    check(len(segments) == expected_nodes[node_id], node_id + " segment count differs from stages")
    segments_total += len(segments)
    for segment in segments:
        check(component_name(segment) == "Image", "Progress segment reference is not an Image")
        check(scalar(objects[segment][1], "m_RaycastTarget") == "0", "Progress segment intercepts pointer input")
    button = objects[ref(card, "purchaseButton")][1]
    target_graphic = ref(button, "m_TargetGraphic")
    check(target_graphic in objects and "m_RaycastTarget:" in objects[target_graphic][1], "Purchase button lacks its transition graphic")
    button_go = ref(button, "m_GameObject")
    hit_graphics = [body for _, (_, body) in objects.items() if "m_RaycastTarget:" in body and ref(body, "m_GameObject") == button_go and scalar(body, "m_RaycastTarget") == "1"]
    check(bool(hit_graphics), "Purchase button GameObject lacks an active raycast graphic")
    check(ref(button, "m_GameObject") == ref(card, "m_GameObject"), "Card pointer handlers are separated from button hit target")
check(len(set(seen_nodes)) == 10 and set(seen_nodes) == set(expected_nodes) and segments_total == 24, "Card mapping does not cover the ten nodes and twenty-four segments")

root_go = ref(menu, "m_GameObject")
check(scalar(game_objects[root_go], "m_IsActive") == "0", "Prefab root must be inactive until binding is complete")
named = {}
for identifier, body in game_objects.items():
    named.setdefault(scalar(body, "m_Name"), []).append(identifier)
check("EventSystem" not in named, "Prefab creates a second EventSystem")
for name in ("AspectSafeAreaRoot", "AspectContentRoot", "LetterboxBars", "UpgradeGrid", "LeftBar", "RightBar", "BottomBar", "TopBar"):
    check(len(named.get(name, [])) == 1, "Missing or duplicate fixed-aspect/grid root: " + name)
safe_transform = transform_for_go[named["AspectSafeAreaRoot"][0]]
content_transform = transform_for_go[named["AspectContentRoot"][0]]
check(ref(transforms[safe_transform], "m_Father") == transform_for_go[root_go], "Safe-area root is not a direct Canvas child")
check(ref(transforms[content_transform], "m_Father") == safe_transform, "Content root is not inside safe area")
check(scalar(transforms[content_transform], "m_SizeDelta") == "{x: 800, y: 450}", "Content does not match 16:9 logical dimensions")

tooltip_transform = ref(menu, "tooltip")
tooltip_descendants = {tooltip_transform}
while True:
    added = {identifier for identifier, body in transforms.items() if ref(body, "m_Father") in tooltip_descendants} - tooltip_descendants
    if not added:
        break
    tooltip_descendants |= added
tooltip_gos = {ref(transforms[identifier], "m_GameObject") for identifier in tooltip_descendants}
tooltip_graphics = [body for identifier, (kind, body) in objects.items() if "m_RaycastTarget:" in body and ref(body, "m_GameObject") in tooltip_gos]
check(len(tooltip_graphics) >= 4, "Tooltip lacks its background or text graphics")
for graphic in tooltip_graphics:
    check(scalar(graphic, "m_RaycastTarget") == "0", "Tooltip intercepts card hover")
grid = [body for identifier, (kind, body) in objects.items() if "UnityEngine.UI.GridLayoutGroup" in body]
check(len(grid) == 1 and scalar(grid[0], "m_ConstraintCount") == "5", "Grid must have five authored columns")
grid_go = ref(grid[0], "m_GameObject")
check(len(refs(transforms[transform_for_go[grid_go]], "m_Children")) == 10, "Grid must contain exactly ten card roots")
source = (ROOT / "Assets/Scripts/LeeJunmo/PermanentUpgrades/PermanentUpgradeMenu.cs").read_text(encoding="utf-8-sig")
check("new GameObject" not in source and ".AddComponent" not in source, "Menu source still generates UI hierarchy")
check("Resources.Load<GameObject>" in source and "GetComponent<PermanentUpgradeMenu>" in source, "Prefab loader must explicitly resolve the menu component")
print(f"PASS: {checks} static serialized prefab integrity checks; {len(game_objects)} GameObjects, {len(objects)} objects, 10 nodes, 24 passive segments. Unity import and visual interaction not executed.")
