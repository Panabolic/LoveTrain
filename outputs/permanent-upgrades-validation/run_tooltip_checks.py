"""Compile unchanged production method bodies against small coordinate/input doubles.

This is an isolated regression check, not a Unity input/camera/Play Mode test.
"""
import hashlib
from pathlib import Path
import subprocess
import sys

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent


def method(path, signature):
    source = path.read_text(encoding="utf-8-sig")
    start = source.index(signature)
    brace = source.index("{", start)
    depth = 1
    end = brace + 1
    while depth:
        char = source[end]
        depth += (char == "{") - (char == "}")
        end += 1
    result = source[start:end]
    print(f"Production method {signature}: SHA256 {hashlib.sha256(result.encode()).hexdigest()}", flush=True)
    return result


place = method(ROOT / "Assets/Scripts/LeeJunmo/PermanentUpgrades/PermanentUpgradeMenu.cs",
               "private void PlaceTooltip(Vector2 point)")
select = method(ROOT / "Assets/Scripts/LeeJunmo/PermanentUpgrades/PermanentUpgradeCard.cs",
                "public void OnSelect(BaseEventData eventData)")
show = method(ROOT / "Assets/Scripts/LeeJunmo/PermanentUpgrades/PermanentUpgradeMenu.cs",
              "public void ShowTooltip(PermanentUpgradeNode node, RectTransform card)")
enter = method(ROOT / "Assets/Scripts/LeeJunmo/PermanentUpgrades/PermanentUpgradeCard.cs",
               "public void OnPointerEnter(PointerEventData pointer)")
card_source = (ROOT / "Assets/Scripts/LeeJunmo/PermanentUpgrades/PermanentUpgradeCard.cs").read_text(encoding="utf-8-sig")
menu_source = (ROOT / "Assets/Scripts/LeeJunmo/PermanentUpgrades/PermanentUpgradeMenu.cs").read_text(encoding="utf-8-sig")
assert "IPointerMoveHandler" not in card_source and "OnPointerMove(" not in card_source, "Card must not follow mouse motion"
assert "MoveTooltip(" not in menu_source and "ShowTooltip(PermanentUpgradeNode node, PointerEventData" not in menu_source, "Menu must use card anchor only"
generated = """using UnityEngine;
using UnityEngine.EventSystems;
public sealed partial class PermanentUpgradeMenu {
METHOD_PLACE
METHOD_SHOW
}
public sealed partial class PermanentUpgradeCard {
METHOD_SELECT
METHOD_ENTER
}
""".replace("METHOD_PLACE", place).replace("METHOD_SELECT", select).replace("METHOD_SHOW", show).replace("METHOD_ENTER", enter)
(HERE / "TooltipProductionMethods.generated.cs").write_text(generated, encoding="utf-8")
result = subprocess.run(["dotnet", "run", "--project", str(HERE / "TooltipRegressionValidation.csproj"),
                         "--property:BaseIntermediateOutputPath=obj/tooltip/", "--verbosity", "quiet"], cwd=HERE)
sys.exit(result.returncode)
