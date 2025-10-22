using UnityEngine;
using UnityEngine.UI; // Slider를 사용하기 위해 필수!

public class ProgressUIController : MonoBehaviour
{
    [Header("시간 설정")]
    [Tooltip("목표 시간(초 단위). (기본값: 3분 = 180초)")]
    [SerializeField] private float totalTimeInSeconds = 180f;

    [Header("UI 연결")]
    [Tooltip("진행도를 표시할 슬라이더(Slider) 컴포넌트")]
    [SerializeField] private Slider progressSlider;

    // 현재까지 흐른 시간을 저장할 변수
    private float currentTime = 0f;

    // 타이머가 진행 중인지 확인하는 변수
    private bool isTimerRunning = true;

    void Start()
    {
        // 시작 시 슬라이더 값 0으로 초기화
        if (progressSlider != null)
        {
            progressSlider.value = 0;
        }
        currentTime = 0f;
        isTimerRunning = true;
    }

    void Update()
    {
        // 타이머가 멈췄거나 슬라이더가 연결되지 않았으면 실행하지 않음
        if (!isTimerRunning || progressSlider == null)
        {
            return;
        }

        // 1. 매 프레임마다 시간(Time.deltaTime)을 더해줍니다.
        currentTime += Time.deltaTime;

        // 2. 현재 진행도 계산 (현재 시간 / 총 시간)
        //    Mathf.Clamp01을 사용해 값이 0.0 ~ 1.0 범위를 벗어나지 않게 합니다.
        float progress = Mathf.Clamp01(currentTime / totalTimeInSeconds);

        // 3. 계산된 진행도를 슬라이더의 value 값에 적용합니다.
        progressSlider.value = progress;

        // 4. 진행도가 1.0 (100%)에 도달하면 타이머를 멈춥니다.
        if (progress >= 1.0f)
        {
            isTimerRunning = false;
            OnTimeFinished();
        }
    }

    /// <summary>
    /// 시간이 모두 경과했을 때 호출되는 함수
    /// </summary>
    private void OnTimeFinished()
    {
        Debug.Log("시간 종료!");
        // (선택) 여기에 스테이지 실패 또는 다음 페이즈로 넘어가는 로직을 추가할 수 있습니다.
    }
}