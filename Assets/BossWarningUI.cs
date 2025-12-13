using UnityEngine;
using DG.Tweening;

public class BossWarningUI : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("경고 UI 전체 부모 패널 (평소엔 꺼둠)")]
    [SerializeField] private GameObject warningPanel;
        
    [Tooltip("위쪽 경고 띠")]
    [SerializeField] private RectTransform topBand;

    [Tooltip("아래쪽 경고 띠")]
    [SerializeField] private RectTransform bottomBand;

    [Header("Animation Settings")]
    [Tooltip("화면을 가로지르는 데 걸리는 시간")]
    [SerializeField] private float moveDuration = 4.0f;

    [Header("Top Band Positions")]
    [Tooltip("위쪽 띠 시작 X 좌표 (예: -2500)")]
    [SerializeField] private float topStartX = -2500f;
    [Tooltip("위쪽 띠 종료 X 좌표 (예: 2500)")]
    [SerializeField] private float topEndX = 2500f;

    [Header("Bottom Band Positions")]
    [Tooltip("아래쪽 띠 시작 X 좌표 (예: 2500)")]
    [SerializeField] private float bottomStartX = 2500f;
    [Tooltip("아래쪽 띠 종료 X 좌표 (예: -2500)")]
    [SerializeField] private float bottomEndX = -2500f;

    [Header("Sound")]
    [Tooltip("경고음 사운드 이름")]
    [SerializeField] private string warningSoundName = "Warning";

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        if (warningPanel != null) warningPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.Boss)
        {
            SoundEventBus.Publish(SoundID.UI_BossWarning);
            PlayWarningSequence();
        }
    }

    public void PlayWarningSequence()
    {
        if (warningPanel == null || topBand == null || bottomBand == null) return;

        warningPanel.SetActive(true);

        // 1. 초기 위치 설정 (각각 설정한 Start 값으로 이동)
        topBand.anchoredPosition = new Vector2(topStartX, topBand.anchoredPosition.y);
        bottomBand.anchoredPosition = new Vector2(bottomStartX, bottomBand.anchoredPosition.y);

        // 3. 애니메이션 실행
        Sequence seq = DOTween.Sequence();

        // 인게임 시간이 흐를 때 동작하도록 설정 (일시정지 시 멈춤)
        // 만약 일시정지 상태에서도 보여야 한다면 true로 변경하세요.
        seq.SetUpdate(false);

        // 위쪽: Start -> End로 이동
        seq.Join(topBand.DOAnchorPosX(topEndX, moveDuration).SetEase(Ease.Linear));

        // 아래쪽: Start -> End로 이동
        seq.Join(bottomBand.DOAnchorPosX(bottomEndX, moveDuration).SetEase(Ease.Linear));

        // 4. 종료 처리
        seq.OnComplete(() =>
        {
            warningPanel.SetActive(false);
        });
    }
}