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

    //private float moveDirection;

    // Target Components
    protected TrainController trainController;


    private void Awake()
    {
        rigid2D = GetComponent<Rigidbody2D>();

        // Get Target Components
        trainController = GameObject.FindWithTag("Player").GetComponent<TrainController>();
    }

    void Start()
    {
        if (trainController == null || targetRigid == null)
        {
            Debug.LogError("TrainController 또는 Target이 연결되지 않았습니다!", this.gameObject);
            this.enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (!isAlive) return;

        // 이동 로직 구버전
        //moveDirection = Mathf.Sign(targetRigid.position.x - rigid2D.position.x);
        //Vector2 nextXPosition = new Vector2(moveDirection * moveSpeed, rigid2D.position.y) * Time.fixedDeltaTime;
        //rigid2D.MovePosition(rigid2D.position + nextXPosition);
        //rigid2D.linearVelocity = Vector2.zero;

        // 기차의 현재 속력과 속력 비율(0.0 ~ 1.0)을 계산
        float currentTrainSpeed = trainController.CurrentSpeed;
        float speedRate = Mathf.InverseLerp(trainController.minSpeed, trainController.maxSpeed, currentTrainSpeed);

        float currentMultiplier;

        // 자신의 X좌표와 기차의 X좌표를 비교하여 앞/뒤를 판단
        if (transform.position.x > targetRigid.position.x)
        {
            // [상황] 내가 기차보다 앞에 있을 때
            // 기차 속도와 배율이 정비례 관계가 됩니다.
            // 기차가 빨라질수록(speedPercentage → 1.0), 배율도 최대치(maxMultiplier)에 가까워집니다.
            currentMultiplier = Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, speedRate);
        }
        else
        {
            // [상황] 내가 기차보다 뒤에 있을 때
            // 기차 속도와 배율이 반비례 관계가 됩니다.
            // Lerp의 min, max 순서를 바꿔서, 기차가 빨라질수록(speedPercentage → 1.0) 배율은 최소치(minMultiplier)에 가까워집니다.
            currentMultiplier = Mathf.Lerp(maxSpeedMultiplier, minSpeedMultiplier, speedRate);
        }

        // 최종 속력 = 적의 기본 속력 * 현재 계산된 배율
        float finalMoveSpeed = moveSpeed * currentMultiplier;

        // 이동 로직
        if (finalMoveSpeed > 0)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetRigid.position, finalMoveSpeed * Time.deltaTime);
        }
    }

    protected override IEnumerator Die()
    {
        yield return base.Die();

        collision.enabled = true;
        isAlive = false;
        enabled = false;
    }
}
