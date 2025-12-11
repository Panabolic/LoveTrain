using UnityEngine;
using System.Collections;

public class BossWarningLoopUI : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("경고 UI 전체 부모 패널")]
    [SerializeField] private GameObject warningPanel;

    [Header("Top Band Settings (Left -> Right)")]
    [Tooltip("상단 띠 이미지 2개 (서로 이어져 있어야 함)")]
    [SerializeField] private RectTransform[] topBands;
    [Tooltip("상단 이동 속도")]
    [SerializeField] private float topSpeed = 500f;

    [Header("Bottom Band Settings (Right -> Left)")]
    [Tooltip("하단 띠 이미지 2개 (서로 이어져 있어야 함)")]
    [SerializeField] private RectTransform[] bottomBands;
    [Tooltip("하단 이동 속도")]
    [SerializeField] private float bottomSpeed = 500f;

    [Header("Timing")]
    [Tooltip("무한 루프가 지속될 시간")]
    [SerializeField] private float loopDuration = 3.0f;
    [Tooltip("종료 시작 후(화면 밖으로 나갈 때까지) 기다렸다가 꺼질 시간")]
    [SerializeField] private float exitDelay = 3.0f;

    // --- 내부 상태 변수 ---
    private float timer = 0f;
    private bool isPlaying = false;
    private bool isExiting = false; // true면 무한 루프를 멈추고 화면 밖으로 나감

    // 이미지의 너비 (루프 계산용)
    private float topBandWidth;
    private float bottomBandWidth;

    // 초기 위치 저장
    private Vector2[] topInitialPos;
    private Vector2[] bottomInitialPos;

    private void Awake()
    {
        // 이미지 너비 계산 (모두 같은 크기라고 가정)
        if (topBands.Length > 0) topBandWidth = topBands[0].rect.width;
        if (bottomBands.Length > 0) bottomBandWidth = bottomBands[0].rect.width;

        // 초기 위치 저장
        SaveInitialPositions();
    }

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
            PlaySequence();
        }
    }

    // 테스트용
    [ContextMenu("Play Test")]
    public void PlaySequence()
    {
        if (warningPanel == null) return;

        // 1. 상태 초기화
        warningPanel.SetActive(true);
        isPlaying = true;
        isExiting = false;
        timer = 0f;

        // 2. 위치 리셋 (저장된 초기 위치로 복구)
        ResetPositions();
    }

    private void Update()
    {
        if (!isPlaying) return;

        float dt = Time.unscaledDeltaTime; // 일시정지(TimeScale 0) 중에도 경고가 보여야 한다면 unscaled 사용

        // --- 1. 타이머 체크 ---
        timer += dt;
        if (!isExiting && timer >= loopDuration)
        {
            isExiting = true; // 이제부터는 위치 리셋을 안 함 (화면 밖으로 나감)
            StartCoroutine(DisableRoutine());
        }

        // --- 2. 상단 띠 이동 (왼쪽 -> 오른쪽) ---
        MoveBands(topBands, Vector2.right * topSpeed * dt, topBandWidth, true);

        // --- 3. 하단 띠 이동 (오른쪽 -> 왼쪽) ---
        MoveBands(bottomBands, Vector2.left * bottomSpeed * dt, bottomBandWidth, false);
    }

    /// <summary>
    /// 띠를 이동시키고, 화면 밖으로 나가면 위치를 리셋(Loop)하는 함수
    /// </summary>
    private void MoveBands(RectTransform[] bands, Vector2 moveDelta, float width, bool moveRight)
    {
        for (int i = 0; i < bands.Length; i++)
        {
            // 1. 이동
            bands[i].anchoredPosition += moveDelta;

            // 2. 무한 루프 로직 (Exiting 상태가 아닐 때만)
            if (!isExiting)
            {
                if (moveRight) // 왼쪽 -> 오른쪽 이동 중
                {
                    // 이미지가 오른쪽으로 너무 가서(X > width), 화면 밖으로 나갔다면?
                    // -> 왼쪽 끝(현재 위치 - 너비 * 2)으로 이동시켜 뒤에 붙임
                    // (기준점은 앵커 설정에 따라 다를 수 있으나, 보통 X 좌표가 너비만큼 이동하면 리셋)
                    if (bands[i].anchoredPosition.x >= width)
                    {
                        bands[i].anchoredPosition -= new Vector2(width * 2, 0);
                    }
                }
                else // 오른쪽 -> 왼쪽 이동 중
                {
                    // 이미지가 왼쪽으로 너무 가서(X < -width), 화면 밖으로 나갔다면?
                    // -> 오른쪽 끝으로 이동
                    if (bands[i].anchoredPosition.x <= -width)
                    {
                        bands[i].anchoredPosition += new Vector2(width * 2, 0);
                    }
                }
            }
        }
    }

    private IEnumerator DisableRoutine()
    {
        // 화면 밖으로 완전히 나갈 때까지 대기
        yield return new WaitForSecondsRealtime(exitDelay);

        isPlaying = false;
        warningPanel.SetActive(false);
    }

    // --- 헬퍼 함수: 위치 저장 및 복구 ---
    private void SaveInitialPositions()
    {
        topInitialPos = new Vector2[topBands.Length];
        for (int i = 0; i < topBands.Length; i++) topInitialPos[i] = topBands[i].anchoredPosition;

        bottomInitialPos = new Vector2[bottomBands.Length];
        for (int i = 0; i < bottomBands.Length; i++) bottomInitialPos[i] = bottomBands[i].anchoredPosition;
    }

    private void ResetPositions()
    {
        for (int i = 0; i < topBands.Length; i++) topBands[i].anchoredPosition = topInitialPos[i];
        for (int i = 0; i < bottomBands.Length; i++) bottomBands[i].anchoredPosition = bottomInitialPos[i];
    }
}