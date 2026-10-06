using TMPro;
using UnityEngine;

// Reads the existing stage owner and updates only scene-authored HUD references.
[DisallowMultipleComponent]
public sealed class StageProgressUI : MonoBehaviour
{
    [SerializeField] private StageManager stageManager;
    [SerializeField] private UnityEngine.UI.Image progressFill;
    [SerializeField] private RectTransform currentPosition;
    [SerializeField] private UnityEngine.UI.Image[] eventMarkers;
    [SerializeField] private TMP_Text[] eventSymbols;

    private static readonly Color PendingColor = new Color32(131, 122, 143, 255);
    private static readonly Color PassedColor = new Color32(199, 172, 207, 255);
    private static readonly Color SymbolColor = new Color32(248, 237, 219, 255);
    private float displayedProgress = -1f;

    private void OnEnable()
    {
        displayedProgress = -1f;
        Refresh();
    }

    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        float progress = stageManager != null ? stageManager.NormalizedProgress : 0f;
        if (progress == displayedProgress) return;
        displayedProgress = progress;

        if (progressFill != null)
        {
            RectTransform line = progressFill.rectTransform;
            line.anchorMin = Vector2.zero;
            line.anchorMax = new Vector2(progress, 1f);
            line.offsetMin = Vector2.zero;
            line.offsetMax = Vector2.zero;
        }

        if (currentPosition != null)
        {
            currentPosition.anchorMin = new Vector2(progress, 0.5f);
            currentPosition.anchorMax = currentPosition.anchorMin;
            currentPosition.anchoredPosition = Vector2.zero;
        }

        for (int index = 0; eventMarkers != null && index < eventMarkers.Length; index++)
        {
            bool passed = progress >= (index + 1) * 0.25f;
            if (eventMarkers[index] != null)
                eventMarkers[index].color = passed ? PassedColor : PendingColor;
            if (eventSymbols != null && index < eventSymbols.Length && eventSymbols[index] != null)
                eventSymbols[index].color = passed ? PassedColor : SymbolColor;
        }
    }
}
