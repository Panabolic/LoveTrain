using System;
using UnityEngine;
using UnityEngine.EventSystems;

internal static class TooltipRegressionChecks
{
    private static int assertions;
    private static void Check(bool condition, string explanation)
    { assertions++; if (!condition) throw new Exception(explanation); }
    private static void Near(float actual, float expected, string explanation)
    { Check(Math.Abs(actual - expected) < .001f, explanation + ": actual=" + actual + ", expected=" + expected); }

    private static void Case(Vector2 anchor, bool left, bool above, Vector2 initialPivot, string name)
    {
        RectTransform panel = new RectTransform { rect = new Rect(-400, -225, 800, 450) };
        RectTransform tooltip = new RectTransform { rect = new Rect(0, 0, 260, 170), pivot = initialPivot };
        PermanentUpgradeMenu menu = new PermanentUpgradeMenu(panel, tooltip);
        menu.Position(anchor);
        Near(tooltip.pivot.x, left ? 1 : 0, name + " selected horizontal corner");
        Near(tooltip.pivot.y, above ? 0 : 1, name + " selected vertical corner");
        Near(tooltip.anchoredPosition.x - anchor.x, left ? -12 : 12, name + " anchor horizontal gap");
        Near(tooltip.anchoredPosition.y - anchor.y, above ? 12 : -12, name + " anchor vertical gap");
        float xmin = tooltip.anchoredPosition.x - tooltip.pivot.x * tooltip.rect.size.x;
        float ymin = tooltip.anchoredPosition.y - tooltip.pivot.y * tooltip.rect.size.y;
        Check(xmin >= panel.rect.xMin + 8 - .001f && xmin + 260 <= panel.rect.xMax - 8 + .001f, name + " horizontal bounds");
        Check(ymin >= panel.rect.yMin + 8 - .001f && ymin + 170 <= panel.rect.yMax - 8 + .001f, name + " vertical bounds");
        Check(tooltip.lastSiblingCalls == 1, name + " tooltip stays on top");
    }

    private static void CardAnchoring(bool scaled)
    {
        RectTransform panel = new RectTransform { rect = new Rect(-400, -225, 800, 450),
            position = scaled ? new Vector3(200, 400, 0) : new Vector3(0, 0, 0),
            worldScale = scaled ? new Vector2(2, 2) : new Vector2(1, 1) };
        RectTransform tooltip = new RectTransform { rect = new Rect(0, 0, 260, 170), pivot = new Vector2(0, 1) };
        // Matches a top-center button pivot: local center is 36 units below its origin.
        RectTransform transform = new RectTransform { rect = new Rect(-70, -72, 140, 72), pivot = new Vector2(.5f, 1),
            position = scaled ? new Vector3(400, 600, 0) : new Vector3(0, 0, 0),
            worldScale = scaled ? new Vector2(2, 2) : new Vector2(1, 1) };
        PermanentUpgradeMenu menu = new PermanentUpgradeMenu(panel, tooltip);
        PermanentUpgradeNode node = new PermanentUpgradeNode();
        PermanentUpgradeCard card = new PermanentUpgradeCard(menu, node, transform);
        // Center y=-36 would cross the bottom margin by one unit, so the unscaled case opens above.
        Vector2 expected = scaled ? new Vector2(112, 52) : new Vector2(12, -24);
        foreach (Vector2 cursor in new[] { new Vector2(345, 210), new Vector2(-345, -210), new Vector2(0, 0) })
        {
            PointerEventData pointer = new PointerEventData { position = cursor };
            card.OnPointerEnter(pointer);
            Check(card.PointerInside, "Pointer enter keeps hover ownership");
            Check(ReferenceEquals(menu.Hovered, node), "Pointer enter keeps node");
            Near(tooltip.anchoredPosition.x, expected.x, "Hover uses button center regardless of mouse x");
            Near(tooltip.anchoredPosition.y, expected.y, "Hover uses button center regardless of mouse y");
            Check(tooltip.gameObject.activeSelf, "Tooltip becomes visible on hover");
            Near(tooltip.pivot.y, scaled ? 1 : 0, "Center determines vertical opening direction");
            card.OnSelect(pointer);
            Near(tooltip.anchoredPosition.x, expected.x, "Mouse click shares button center x");
            Near(tooltip.anchoredPosition.y, expected.y, "Mouse click shares button center y");
        }
        card.OnSelect(new BaseEventData());
        Near(tooltip.anchoredPosition.x, expected.x, "Keyboard selection shares button center x");
        Near(tooltip.anchoredPosition.y, expected.y, "Keyboard selection shares button center y");
        int refreshes = menu.RefreshCalls;
        PointerEventData pointerForGuard = new PointerEventData();
        new PermanentUpgradeCard(null, node, transform).OnSelect(pointerForGuard);
        new PermanentUpgradeCard(menu, null, transform).OnSelect(pointerForGuard);
        new PermanentUpgradeCard(null, node, transform).OnPointerEnter(pointerForGuard);
        new PermanentUpgradeCard(menu, null, transform).OnPointerEnter(pointerForGuard);
        Check(menu.RefreshCalls == refreshes, "Unbound cards must ignore hover/selection");
    }

    public static int Main()
    {
        try
        {
            foreach (Vector2 initialPivot in new[] { new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(1, 0) })
            {
                Case(new Vector2(0, 0), false, false, initialPivot, "Central anchor");
                Case(new Vector2(250, 50), true, false, initialPivot, "Right border flips left");
                Case(new Vector2(-100, -150), false, true, initialPivot, "Bottom border flips above");
                Case(new Vector2(350, -200), true, true, initialPivot, "Bottom-right corner flips both");
                Case(new Vector2(390, 215), true, false, initialPivot, "Top-right corner");
                Case(new Vector2(-390, 215), false, false, initialPivot, "Top-left corner");
                Case(new Vector2(-390, -215), false, true, initialPivot, "Bottom-left corner");
                Case(new Vector2(120, -35), false, false, initialPivot, "Exact margin fits without flip");
                Case(new Vector2(120.01f, -35.01f), true, true, initialPivot, "One fraction beyond margin flips");
            }
            for (int x = -390; x <= 390; x += 30)
                for (int y = -215; y <= 215; y += 10)
                    Case(new Vector2(x, y), x + 12 + 260 > 392, y - 12 - 170 < -217,
                         new Vector2(0, 1), "Screen grid " + x + "," + y);
            CardAnchoring(false);
            CardAnchoring(true);
            Console.WriteLine("PASS: " + assertions + " assertions; actual production PlaceTooltip, ShowTooltip, OnPointerEnter and OnSelect bodies.");
            Console.WriteLine("Coverage: button-center anchoring, differing pointer positions ignored, scaled/translated transforms, horizontal/vertical flips, edge margins, changing pivots.");
            Console.WriteLine("Isolated coordinate/input doubles only; actual Unity transforms/events, Editor import and Play Mode not executed.");
            return 0;
        }
        catch (Exception error) { Console.WriteLine("FAIL after " + assertions + " assertions: " + error.Message); return 1; }
    }
}
