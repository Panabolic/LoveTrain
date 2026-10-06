using UnityEngine;
using TMPro;
using DG.Tweening;

public class LevelUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("기차의 TrainLevelManager를 연결")]
    [SerializeField] private TrainLevelManager levelManager;

    [Header("UI 요소")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private UnityEngine.UI.Slider xpBarSlider;
    [SerializeField] private UnityEngine.UI.Image xpFill;
    [SerializeField, Min(0f)] private float fillDuration = 0.5f;

    private Tween fillTween;
    private int displayedLevel;

    private void OnEnable()
    {
        if (levelManager == null) return;
        levelManager.OnExperienceGained += UpdateUI;
        levelManager.OnLevelUp += UpdateUI;
        RefreshImmediately();
    }

    private void Start() => RefreshImmediately();

    private void OnDisable()
    {
        if (levelManager != null)
        {
            levelManager.OnExperienceGained -= UpdateUI;
            levelManager.OnLevelUp -= UpdateUI;
        }
        fillTween?.Kill();
        fillTween = null;
    }

    private void RefreshImmediately()
    {
        if (levelManager == null) return;
        displayedLevel = levelManager.CurrentLevel;
        if (levelText != null) levelText.text = $"Lv. {displayedLevel}";
        SetFill(levelManager.CurrentLevelProgress);
    }

    private void UpdateUI()
    {
        if (levelManager == null) return;
        fillTween?.Kill();
        if (displayedLevel != levelManager.CurrentLevel)
        {
            displayedLevel = levelManager.CurrentLevel;
            if (levelText != null) levelText.text = $"Lv. {displayedLevel}";
            SetFill(0f);
        }
        float fill = xpFill != null ? xpFill.fillAmount : xpBarSlider != null ? xpBarSlider.value : 0f;
        fillTween = DOTween.To(() => fill, value => { fill = value; SetFill(value); },
            levelManager.CurrentLevelProgress, fillDuration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    private void SetFill(float value)
    {
        if (xpFill != null) xpFill.fillAmount = value;
        if (xpBarSlider != null) xpBarSlider.value = value;
    }
}
