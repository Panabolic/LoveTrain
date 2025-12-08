using System.ComponentModel;
using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위해 필수!

public class ProgressUIController : MonoBehaviour
{
    [Header("시간 설정")]
    [Tooltip("목표 시간(초 단위). (기본값: 3분 = 180초)")]
    [SerializeField] private float totalTimeInSeconds = 900f;

    [Header("UI 연결")]
    // [수정] Slider 대신 TextMeshProUGUI를 연결
    [Tooltip("시간을 표시할 TextMeshProUGUI 컴포넌트")]
    [SerializeField] private TextMeshProUGUI timeText;

    // 타이머가 진행 중인지 확인하는 변수
    private bool isTimerRunning = false;

    void Start()
    {
        isTimerRunning = false;

        // 시작 시 텍스트를 "03:00" (초기 시간)으로 초기화
        UpdateTimeText(totalTimeInSeconds);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChange;
            // 현재 상태로 초기화
            HandleGameStateChange(GameManager.Instance.CurrentState);
        }
    }

    void Update()
    {
        // 타이머가 멈췄거나 텍스트가 연결되지 않았으면 실행하지 않음
        if (!isTimerRunning || timeText == null)
        {
            return;
        }

        // '남은 시간'을 계산합니다.
        float remainingTime = totalTimeInSeconds - GameManager.Instance.gameTime;

        // 남은 시간이 0보다 작아지면 0으로 고정합니다.
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isTimerRunning = false; // 타이머 멈춤
            OnTimeFinished();
        }

        // 계산된 남은 시간을 텍스트(00:00 형식)로 변환하여 적용합니다.
        UpdateTimeText(remainingTime);
    }

    /// <summary>
    /// 초 단위 시간을 00:00 형식의 텍스트로 변환하여 UI에 표시합니다.
    /// </summary>
    private void UpdateTimeText(float timeInSeconds)
    {
        if (timeText == null) return;

        // 시간을 분과 초로 변환
        float minutes = Mathf.FloorToInt(timeInSeconds / 60);
        float seconds = Mathf.FloorToInt(timeInSeconds % 60);

        // string.Format을 사용해 "00:00" 형식으로 만듭니다.
        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    /// <summary>
    /// 시간이 모두 경과했을 때 호출되는 함수
    /// </summary>
    private void OnTimeFinished()
    {
        Debug.Log("시간 종료!");
        // (선택) 여기에 스테이지 실패 또는 다음 페이즈로 넘어가는 로직을 추가할 수 있습니다.
    }

    private void HandleGameStateChange(GameState newState)
    {
        isTimerRunning = (newState == GameState.Playing);

        // (선택적) 게임이 시작될 때마다 타이머를 리셋할 수도 있습니다.
        // if (isTimerRunning)
        // {
        //     currentTime = 0f;
        //     UpdateTimeText(totalTimeInSeconds);
        // }
    }
}