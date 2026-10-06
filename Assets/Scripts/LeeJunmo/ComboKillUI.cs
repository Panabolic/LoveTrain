using TMPro;
using UnityEngine;

// Displays scene-authored references; all combo state belongs to GameManager.
[DisallowMultipleComponent]
public sealed class ComboKillUI : MonoBehaviour
{
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text godMessage;
    [SerializeField] private UnityEngine.UI.Image timeFill;
    [SerializeField] private RectTransform timeTrack;
    [SerializeField] private CanvasGroup visibility;

    private const string GodMessage = "고대신이 당신을 눈 여겨 봅니다.";
    private int displayedCount = -1;
    private float displayedFraction = -1f;
    private bool isVisible;

    private void OnEnable()
    {
        displayedCount = -1;
        displayedFraction = -1f;
        if (godMessage != null) godMessage.text = GodMessage;
        if (visibility != null)
        {
            visibility.interactable = false;
            visibility.blocksRaycasts = false;
        }
        Refresh();
    }

    private void LateUpdate() => Refresh();

    private void OnDisable()
    {
        SetVisible(false);
        if (godMessage != null) godMessage.gameObject.SetActive(false);
        displayedCount = -1;
        displayedFraction = -1f;
    }

    private void Refresh()
    {
        GameManager manager = GameManager.Instance;
        int count = manager != null ? manager.ComboKillCount : 0;
        float fraction = manager != null ? manager.ComboTimeFraction : 0f;
        SetVisible(count > 0);

        if (count != displayedCount)
        {
            displayedCount = count;
            if (countText != null)
            {
                countText.text = ComboKillState.FormatLabel(count);
                if (timeTrack != null)
                    timeTrack.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                        countText.GetPreferredValues(countText.text).x);
            }
            if (godMessage != null) godMessage.gameObject.SetActive(count >= 10);
        }

        if (fraction == displayedFraction) return;
        displayedFraction = fraction;
        if (timeFill == null) return;
        timeFill.fillAmount = fraction;
        // The authored, sprite-free Simple Image uses its rectangle as the underline fill.
        RectTransform fillRect = timeFill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(fraction, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }

    private void SetVisible(bool visible)
    {
        // Keep the presenter enabled while hidden so the next kill can reveal the authored HUD.
        float alpha = visible ? 1f : 0f;
        if (visibility != null && visibility.alpha != alpha) visibility.alpha = alpha;
        if (isVisible == visible) return;
        isVisible = visible;
        if (countText != null) countText.enabled = visible;
        if (timeFill != null) timeFill.enabled = visible;
    }
}
