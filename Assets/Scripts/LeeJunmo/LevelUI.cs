using UnityEngine;
using TMPro; // TextMeshPro 사용
using UnityEngine.UI; // Image 사용

public class LevelUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("기차의 TrainLevelManager를 연결")]
    [SerializeField] private TrainLevelManager levelManager;

    [Header("UI 요소")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider xpBarSlider;

    void Start()
    {
        if (levelManager == null)
        {
            Debug.LogError("LevelManager가 연결되지 않았습니다!", this);
            this.enabled = false;
            return;
        }

        // 이벤트 구독: 경험치를 얻거나 레벨업할 때마다 UpdateUI 함수가 자동 호출됨
        levelManager.OnExperienceGained += UpdateUI;
        levelManager.OnLevelUp += UpdateUI;

        // 게임 시작 시 UI 초기화
        UpdateUI();
    }

    void OnDestroy()
    {
        // 오브젝트 파괴 시 이벤트 구독 해제 (메모리 누수 방지)
        if (levelManager != null)
        {
            levelManager.OnExperienceGained -= UpdateUI;
            levelManager.OnLevelUp -= UpdateUI;
        }
    }

    /// <summary>
    /// UI를 최신 정보로 업데이트합니다.
    /// </summary>
    private void UpdateUI()
    {
        // 레벨 텍스트 업데이트 (예: "LV 2")
        levelText.text = $"LV {levelManager.CurrentLevel}";

        // 경험치 바(Image Fill) 업데이트
        xpBarSlider.value = levelManager.CurrentLevelProgress;
    }
}