using UnityEngine;
using TMPro; // TextMeshPro 사용
using UnityEngine.UI; // Slider 사용
using DG.Tweening; // ✨ DOTween 사용

public class LevelUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("기차의 TrainLevelManager를 연결")]
    [SerializeField] private TrainLevelManager levelManager;

    [Header("UI 요소")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider xpBarSlider;

    [Header("애니메이션 설정")]
    [SerializeField] private float fillDuration = 0.5f;

    // 내부 변수
    private int displayedLevel;
    private Tween xpTween;

    void Start()
    {
        if (levelManager == null)
        {
            Debug.LogError("TrainLevelManager가 연결되지 않았습니다!", this);
            this.enabled = false;
            return;
        }

        // 초기화: 시작 시점의 레벨과 경험치를 바로 적용
        displayedLevel = levelManager.CurrentLevel;
        levelText.text = $"LV {displayedLevel}";
        xpBarSlider.value = levelManager.CurrentLevelProgress;

        // 사용자님의 구조 유지: UpdateUI 하나로 모든 이벤트 처리
        levelManager.OnExperienceGained += UpdateUI;
        levelManager.OnLevelUp += UpdateUI;
    }

    void OnDestroy()
    {
        if (levelManager != null)
        {
            levelManager.OnExperienceGained -= UpdateUI;
            levelManager.OnLevelUp -= UpdateUI;
        }
        xpTween?.Kill();
    }

    /// <summary>
    /// UI를 최신 정보로 업데이트합니다. (경험치 획득 및 레벨업 자동 분기)
    /// </summary>
    private void UpdateUI()
    {
        // 1. 레벨업 상황인지 체크 (현재 표시된 레벨 < 실제 레벨)
        if (levelManager.CurrentLevel > displayedLevel)
        {
            PlayLevelUpSequence();
        }
        // 2. 단순 경험치 획득 상황
        else
        {
            PlayExpGainAnimation();
        }
    }

    // 단순 경험치 획득 애니메이션
    private void PlayExpGainAnimation()
    {
        float targetValue = levelManager.CurrentLevelProgress;

        xpTween?.Kill();
        // 게임 정지(UI 팝업) 상태에서도 게이지가 차오르도록 SetUpdate(true) 설정
        xpTween = xpBarSlider.DOValue(targetValue, fillDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    // 레벨업 시퀀스 애니메이션
    private void PlayLevelUpSequence()
    {
        xpTween?.Kill();
        Sequence seq = DOTween.Sequence().SetUpdate(true);

        // [A] 남은 게이지를 꽉 채움 (1.0)
        float currentVal = xpBarSlider.value;
        float timeToFill = fillDuration * (1f - currentVal);
        seq.Append(xpBarSlider.DOValue(1f, timeToFill).SetEase(Ease.Linear));

        // [B] 꽉 찬 뒤 레벨 텍스트 갱신 및 게이지 리셋 (0.0)
        seq.AppendCallback(() => {
            displayedLevel = levelManager.CurrentLevel;
            levelText.text = $"LV {displayedLevel}";
            xpBarSlider.value = 0f;
        });

        // [C] 현재 경험치량(초과분)까지 다시 채움
        float newTarget = levelManager.CurrentLevelProgress;
        if (newTarget > 0)
        {
            seq.Append(xpBarSlider.DOValue(newTarget, fillDuration).SetEase(Ease.OutQuad));
        }

        xpTween = seq;
    }
}