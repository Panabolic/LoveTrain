using UnityEngine;

public class EnemySimple : MonoBehaviour
{
    [Header("대상 설정")]
    [Tooltip("속도 정보를 가져올 TrainController를 여기에 연결해주세요.")]
    [SerializeField] private TrainController trainController;

    [Tooltip("적이 추적할 대상 (보통 기차)")]
    [SerializeField] private Transform target;

    [Header("능력치 설정")]
    [Tooltip("이 적의 기본 이동 속도입니다.")]
    [SerializeField] private float baseMoveSpeed = 5f;

    [Header("속도 배율 설정")]
    [Tooltip("적용될 최저 속도 배율 (예: 0.5 = 50%)")]
    [SerializeField] private float minSpeedMultiplier = 0.5f;
    [Tooltip("적용될 최고 속도 배율 (예: 2.0 = 200%)")]
    [SerializeField] private float maxSpeedMultiplier = 2.0f;


    void Start()
    {
        if (trainController == null || target == null)
        {
            Debug.LogError("TrainController 또는 Target이 연결되지 않았습니다!", this.gameObject);
            this.enabled = false;
        }
    }

    void Update()
    {
        if (target == null || trainController == null) return;

        // 1. 기차의 현재 속력과 속력 비율(0.0 ~ 1.0)을 계산합니다.
        float currentTrainSpeed = trainController.CurrentSpeed;
        float speedPercentage = Mathf.InverseLerp(trainController.minSpeed, trainController.maxSpeed, currentTrainSpeed);

        float currentMultiplier;

        // ✨ [핵심 수정] 자신의 X좌표와 기차의 X좌표를 비교하여 앞/뒤를 판단합니다.
        if (transform.position.x > target.position.x)
        {
            // [상황] 내가 기차보다 앞에 있을 때
            // 기차 속도와 배율이 정비례 관계가 됩니다.
            // 기차가 빨라질수록(speedPercentage → 1.0), 배율도 최대치(maxMultiplier)에 가까워집니다.
            currentMultiplier = Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, speedPercentage);
        }
        else
        {
            // [상황] 내가 기차보다 뒤에 있을 때
            // 기차 속도와 배율이 반비례 관계가 됩니다.
            // Lerp의 min, max 순서를 바꿔서, 기차가 빨라질수록(speedPercentage → 1.0) 배율은 최소치(minMultiplier)에 가까워집니다.
            currentMultiplier = Mathf.Lerp(maxSpeedMultiplier, minSpeedMultiplier, speedPercentage);
        }

        // 최종 속력 = 적의 기본 속력 * 현재 계산된 배율
        float finalMoveSpeed = baseMoveSpeed * currentMultiplier;

        // 이동 로직
        if (finalMoveSpeed > 0)
        {
            transform.position = Vector2.MoveTowards(transform.position, target.position, finalMoveSpeed * Time.deltaTime);
        }
    }
}