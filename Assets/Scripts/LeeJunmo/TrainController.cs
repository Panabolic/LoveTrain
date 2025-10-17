using UnityEngine;
using UnityEngine.InputSystem;

public class TrainController : MonoBehaviour
{
    [Header("속도 값 설정")]
    [Tooltip("실제 이동에 사용되지 않고, UI 표시나 다른 계산에 사용될 기본 수치입니다.")]
    [SerializeField] private float baseSpeedValue = 200f;

    [Header("속력 변동 설정")]
    [Tooltip("A키로 감속할 수 있는 최대 퍼센티지입니다. (예: -20은 20% 감속)")]
    [SerializeField][Range(-100f, 0f)] private float minSpeedPercentage = -20f;

    [Tooltip("D키로 가속할 수 있는 최대 퍼센티지입니다. (예: 20은 20% 가속)")]
    [SerializeField][Range(0f, 100f)] private float maxSpeedPercentage = 20f;

    [Tooltip("가속/감속 키를 눌렀을 때 퍼센티지가 얼마나 빨리 변할지 결정합니다.")]
    [SerializeField] private float accelerationRate = 50f;

    [Header("위치 매핑 설정")]
    [Tooltip("속도가 최소일 때 기차가 위치할 X좌표입니다.")]
    [SerializeField] private float minXPosition = -8f;

    [Tooltip("속도가 최대일 때 기차가 위치할 X좌표입니다.")]
    [SerializeField] private float maxXPosition = 8f;

    // --- 외부 공개 속성 ---
    public float BaseSpeedValue => baseSpeedValue;
    public float CurrentSpeedPercentage { get; private set; }
    public float MinSpeedPercentage => minSpeedPercentage;
    public float MaxSpeedPercentage => maxSpeedPercentage;

    // ✨ [요청 사항] 퍼센트로 조정된 최종 속도 값을 다시 추가합니다.
    public float CurrentSpeed { get; private set; }

    [Header("디버그 정보")]
    [SerializeField] private float _currentPercentageForInspector;
    [SerializeField] private float _currentSpeedForInspector; // 디버그용 현재 속도

    void Start()
    {
        CurrentSpeedPercentage = 0f;
        UpdateCurrentSpeed(); // 시작할 때 한 번 호출
        UpdatePositionBySpeedPercentage();
    }

    void Update()
    {
        // 1. 플레이어 입력에 따라 '속도 퍼센티지'를 조절합니다.
        HandleSpeedControl();

        // 2. ✨ [요청 사항] 조절된 퍼센티지를 기반으로 최종 CurrentSpeed를 계산합니다.
        UpdateCurrentSpeed();

        // 3. 조절된 '속도 퍼센티지'에 따라 기차의 X 위치를 업데이트합니다.
        UpdatePositionBySpeedPercentage();

        // 디버그용 변수 업데이트
        _currentPercentageForInspector = CurrentSpeedPercentage;
        _currentSpeedForInspector = CurrentSpeed;
    }

    private void HandleSpeedControl()
    {
        float inputDirection = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed)
            {
                inputDirection = 1f;
            }
            else if (Keyboard.current.aKey.isPressed)
            {
                inputDirection = -1f;
            }
        }

        if (inputDirection != 0)
        {
            CurrentSpeedPercentage += inputDirection * accelerationRate * Time.deltaTime;
            CurrentSpeedPercentage = Mathf.Clamp(CurrentSpeedPercentage, minSpeedPercentage, maxSpeedPercentage);
        }
    }

    /// <summary>
    /// ✨ [요청 사항] 현재 속도 퍼센티지를 이용해 최종 CurrentSpeed를 계산하는 함수
    /// </summary>
    private void UpdateCurrentSpeed()
    {
        // 퍼센티지를 실제 속도에 적용할 배율로 변환합니다. (예: 20% -> 1.2배, -20% -> 0.8배)
        float speedModifier = 1.0f + (CurrentSpeedPercentage / 100.0f);
        CurrentSpeed = baseSpeedValue * speedModifier;
    }

    private void UpdatePositionBySpeedPercentage()
    {
        float percentageT = Mathf.InverseLerp(minSpeedPercentage, maxSpeedPercentage, CurrentSpeedPercentage);
        float targetXPosition = Mathf.Lerp(minXPosition, maxXPosition, percentageT);
        transform.position = new Vector3(targetXPosition, transform.position.y, transform.position.z);
    }
}