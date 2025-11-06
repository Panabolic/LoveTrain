using UnityEngine;
using UnityEngine.UI;

public class SpeedMeterUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("기차의 Train 스크립트를 연결하세요.")]
    [SerializeField] private Train train;

    [Tooltip("회전시킬 바늘(Needle) 이미지의 RectTransform을 연결하세요.")]
    [SerializeField] private RectTransform needleRectTransform;

    [Header("회전 설정")]
    // ✨ [툴팁 수정]
    [Tooltip("속도가 최소(deathSpeedThreshold)일 때의 바늘 각도 (Z축)")]
    [SerializeField] private float minAngle = 0f;

    // ✨ [툴팁 수정]
    [Tooltip("속도가 최대(baseSpeedValue)일 때의 바늘 각도 (Z축)")]
    [SerializeField] private float maxAngle = -180f;

    [Header("아날로그 설정")]
    [Tooltip("바늘이 목표 각도까지 따라가는 속도. 높을수록 빠릅니다.")]
    [SerializeField] private float needleSmoothSpeed = 5f;

    private float currentAngleZ;

    void Start()
    {
        // 시작 시 바늘 초기화 (기존과 동일)
        currentAngleZ = minAngle;
        if (needleRectTransform != null)
        {
            needleRectTransform.rotation = Quaternion.Euler(0, 0, currentAngleZ);
        }
    }

    void Update()
    {
        if (train == null || needleRectTransform == null)
        {
            return; // 참조가 없으면 실행 중지
        }

        // --- ✨ [로직 핵심 수정] ---

        // 1. 현재 속도, 최소 속도(사망), 최대 속도를 가져옵니다.
        float currentSpeed = train.CurrentSpeed;
        float minSpeed = train.GetDeathSpeed(); // 0 대신 사망 속도 사용
        float maxSpeed = train.MaxSpeedValue;

        // 2. 실제 속도 범위를 계산합니다. (최대 속도 - 최소 속도)
        float speedRange = maxSpeed - minSpeed;

        // 3. 현재 속도가 최소 속도로부터 얼마나 떨어져 있는지 계산합니다.
        float speedOffset = currentSpeed - minSpeed;

        // 4. (최소 속도 ~ 최대 속도) 범위를 (0.0 ~ 1.0) 비율로 변환합니다.
        float speedRatio = 0f;
        if (speedRange > 0)
        {
            speedRatio = speedOffset / speedRange;
        }

        // 5. 비율이 0~1 범위를 벗어나지 않도록 고정합니다.
        // (현재 속도가 사망 속도보다 낮으면 0, 최대 속도보다 높으면 1이 됩니다)
        speedRatio = Mathf.Clamp01(speedRatio);

        // --- [수정 끝] ---

        // 6. 비율(0.0~1.0)에 따른 '목표 각도'를 계산합니다.
        float targetAngleZ = Mathf.Lerp(minAngle, maxAngle, speedRatio);

        // 7. 현재 각도에서 목표 각도까지 부드럽게 이동시킵니다.
        currentAngleZ = Mathf.LerpAngle(
            currentAngleZ,
            targetAngleZ,
            Time.deltaTime * needleSmoothSpeed
        );

        // 8. 부드럽게 계산된 현재 각도를 바늘에 적용합니다.
        needleRectTransform.rotation = Quaternion.Euler(0, 0, currentAngleZ);
    }
}