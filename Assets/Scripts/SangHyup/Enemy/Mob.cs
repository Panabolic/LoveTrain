using System.Collections;
using UnityEngine;

public class Mob : Enemy
{
    // Components
    protected Rigidbody2D rigid2D;

    [Tooltip("기본 속도")]
    [SerializeField] protected float moveSpeed = 2.0f;
    [Tooltip("적용될 최저 속도 배율 (예: 0.5 = 50%)")]
    [SerializeField] protected float minSpeedMultiplier = 0.5f;
    [Tooltip("적용될 최고 속도 배율 (예: 2.0 = 200%)")]
    [SerializeField] protected float maxSpeedMultiplier = 2.0f;

    // Target Components
    protected TrainController trainController;

    protected override void Awake()
    {
        base.Awake();
        rigid2D = GetComponent<Rigidbody2D>();
        // "Player" 태그를 가진 오브젝트에서 TrainController를 찾아옵니다.
        trainController = GameObject.FindWithTag("Player").GetComponent<TrainController>();
    }

    protected virtual void Start()
    {
        // targetRigid는 Enemy 부모 클래스에 있다고 가정합니다.
        if (targetRigid == null)
        {
            Debug.LogError("Target이 연결되지 않았습니다!", this.gameObject);
            this.enabled = false;
        }
        if (trainController == null)
        {
            Debug.LogError("TrainController를 찾을 수 없습니다! 'Player' 태그를 확인해주세요.", this.gameObject);
            this.enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (!isAlive) return;

        // ✨ [핵심 수정] 기차의 절대 속도 대신, '속도 퍼센티지'를 기준으로 0~1 사이의 비율을 계산합니다.
        float speedRate = Mathf.InverseLerp(
            trainController.MinSpeedPercentage,
            trainController.MaxSpeedPercentage,
            trainController.CurrentSpeedPercentage
        );

        float currentMultiplier;

        // 자신의 X좌표와 기차의 X좌표를 비교하여 앞/뒤를 판단 (이 로직은 그대로 유지)
        if (transform.position.x > targetRigid.position.x)
        {
            // [상황] 내가 기차보다 앞에 있을 때
            // 기차가 빨라질수록(speedRate → 1.0), 나도 최대 배율(maxMultiplier)로 빨라집니다.
            currentMultiplier = Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, speedRate);
        }
        else
        {
            // [상황] 내가 기차보다 뒤에 있을 때
            // 기차가 빨라질수록(speedRate → 1.0), 나는 최소 배율(minMultiplier)로 느려집니다. (따라잡기 어려워짐)
            currentMultiplier = Mathf.Lerp(maxSpeedMultiplier, minSpeedMultiplier, speedRate);
        }

        // 최종 속력 = 적의 기본 속력 * 현재 계산된 배율
        float finalMoveSpeed = moveSpeed * currentMultiplier;

        // 이동 로직 (기존과 동일)
        if (finalMoveSpeed > 0)
        {
            // 물리 기반 이동이 아니라면 transform.position을 직접 조작하는 것이 더 간단할 수 있습니다.
            transform.position = Vector2.MoveTowards(transform.position, targetRigid.position, finalMoveSpeed * Time.fixedDeltaTime);
        }
    }

    protected override IEnumerator Die()
    {
        yield return base.Die();
        isAlive = false;
        enabled = false;
    }
}