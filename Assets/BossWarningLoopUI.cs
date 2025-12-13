using UnityEngine;
using DG.Tweening;

public class BossWarningLoopUI : MonoBehaviour
{
    public static BossWarningLoopUI Instance; // 싱글톤 추가 (Spawner에서 부르기 위함)

    [Header("UI Components")]
    [SerializeField] private GameObject warningPanel;

    [Header("Top Band Settings")]
    [SerializeField] private RectTransform[] topBands;
    [SerializeField] private float topSpeed = 500f;

    [Header("Bottom Band Settings")]
    [SerializeField] private RectTransform[] bottomBands;
    [SerializeField] private float bottomSpeed = 500f;

    [Header("Timing")]
    [SerializeField] private float loopDuration = 3.0f;
    [SerializeField] private float exitDelay = 3.0f;

    // --- 내부 변수 ---
    private float topBandWidth;
    private float bottomBandWidth;
    private Vector2[] topInitialPos;
    private Vector2[] bottomInitialPos;

    private Sequence mainSequence;
    private Tween movementTween;

    private bool isRunning = false;
    private bool isExiting = false;
    private bool isPaused = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (topBands.Length > 0) topBandWidth = topBands[0].rect.width;
        if (bottomBands.Length > 0) bottomBandWidth = bottomBands[0].rect.width;

        SaveInitialPositions();
    }

    private void Start()
    {
        // ✨ [삭제] GameState 이벤트 리스너 제거!
        // 이제 Spawner가 직접 ShowWarning()을 호출합니다.

        if (warningPanel != null) warningPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        KillAllTweens();
    }

    // ✨ Spawner에서 호출할 공개 함수
    public void ShowWarning()
    {
        if (isRunning) return; // 이미 실행 중이면 무시

        SoundEventBus.Publish(SoundID.UI_BossWarning);
        StartSequence();
    }

    // -----------------------------------------------------------
    // 제어 함수 (Start, Pause, Resume, Stop)
    // -----------------------------------------------------------
    private void StartSequence()
    {
        if (warningPanel == null) return;

        KillAllTweens();
        ResetPositions();
        warningPanel.SetActive(true);

        isRunning = true;
        isExiting = false;
        isPaused = false;

        mainSequence = DOTween.Sequence();
        mainSequence.SetUpdate(true); // TimeScale 0이어도 작동

        mainSequence.AppendInterval(loopDuration);
        mainSequence.AppendCallback(() => { isExiting = true; });
        mainSequence.AppendInterval(exitDelay);
        mainSequence.OnComplete(StopSequence);

        StartBandMovement();
    }

    // ✨ GameManager가 상태 변경 시 호출하여 일시정지/재개 처리
    public void SetPauseState(bool pause)
    {
        if (!isRunning) return;

        if (pause)
        {
            if (!isPaused)
            {
                if (mainSequence != null) mainSequence.Pause();
                if (movementTween != null) movementTween.Pause();
                isPaused = true;
            }
        }
        else
        {
            if (isPaused)
            {
                if (mainSequence != null) mainSequence.Play();
                if (movementTween != null) movementTween.Play();
                isPaused = false;
            }
        }
    }

    private void StopSequence()
    {
        KillAllTweens();
        if (warningPanel != null) warningPanel.SetActive(false);
        isRunning = false;
        isExiting = false;
        isPaused = false;
    }

    private void StartBandMovement()
    {
        movementTween = DOVirtual.Float(0, 1, float.MaxValue, (val) =>
        {
            float dt = Time.unscaledDeltaTime;
            MoveBandsLogic(topBands, Vector2.right, topSpeed, topBandWidth, true, dt);
            MoveBandsLogic(bottomBands, Vector2.left, bottomSpeed, bottomBandWidth, false, dt);
        })
        .SetUpdate(true)
        .SetEase(Ease.Linear);
    }

    private void MoveBandsLogic(RectTransform[] bands, Vector2 direction, float speed, float width, bool isMovingRight, float dt)
    {
        Vector2 delta = direction * speed * dt;
        for (int i = 0; i < bands.Length; i++)
        {
            bands[i].anchoredPosition += delta;
            if (!isExiting)
            {
                if (isMovingRight)
                {
                    if (bands[i].anchoredPosition.x >= width) bands[i].anchoredPosition -= new Vector2(width * bands.Length, 0);
                }
                else
                {
                    if (bands[i].anchoredPosition.x <= -width) bands[i].anchoredPosition += new Vector2(width * bands.Length, 0);
                }
            }
        }
    }

    private void KillAllTweens()
    {
        if (mainSequence != null) mainSequence.Kill();
        if (movementTween != null) movementTween.Kill();
    }

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