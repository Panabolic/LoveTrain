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
    [Tooltip("속도가 0일 때의 바늘 각도 (Z축)")]
    [SerializeField] private float minAngle = 0f;

    [Tooltip("속도가 최대(baseSpeed)일 때의 바늘 각도 (Z축)")]
    [SerializeField] private float maxAngle = -180f;

    [Header("아날로그 설정")]
    [Tooltip("바늘이 목표 각도까지 따라가는 속도. 높을수록 빠릅니다.")]
    [SerializeField] private float needleSmoothSpeed = 5f;

    // 바늘의 현재 각도를 저장할 변수
    private float currentAngleZ;

    void Start()
    {
        // 시작할 때 바늘을 최소 각도로 초기화
        currentAngleZ = minAngle;
        needleRectTransform.rotation = Quaternion.Euler(0, 0, currentAngleZ);
    }

    void Update()
    {
        if (train == null || needleRectTransform == null)
        {
            return; // 참조가 없으면 실행 중지
        }

        // 1. 현재 속도와 최대 속도를 가져옵니다.
        float currentSpeed = train.CurrentSpeed;
        float maxSpeed = train.BaseSpeedValue;

        // 2. 현재 속도의 비율(0.0 ~ 1.0)을 계산합니다.
        float speedRatio = 0f;
        if (maxSpeed > 0)
        {
            speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);
        }

        // 3. 비율에 따른 '목표 각도'를 계산합니다.
        float targetAngleZ = Mathf.Lerp(minAngle, maxAngle, speedRatio);

        // ✨ [핵심 수정]
        // 4. 현재 각도(currentAngleZ)에서 목표 각도(targetAngleZ)까지 부드럽게 이동시킵니다.
        // LerpAngle은 -180도 -> 0도 같이 큰 각도 차이도 최단 거리로 회전시켜 줍니다.
        currentAngleZ = Mathf.LerpAngle(
            currentAngleZ,
            targetAngleZ,
            Time.deltaTime * needleSmoothSpeed
        );

        // 5. 부드럽게 계산된 현재 각도를 바늘에 적용합니다.
        needleRectTransform.rotation = Quaternion.Euler(0, 0, currentAngleZ);
    }
}