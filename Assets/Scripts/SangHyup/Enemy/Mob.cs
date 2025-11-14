using System;
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

    protected Vector2 moveDirection = Vector2.zero;

    // Target Components
    protected TrainController trainController;

    public Action<Mob> OnDied;

    protected override void Awake()
    {
        base.Awake();

        // Get components
        rigid2D = GetComponent<Rigidbody2D>();

        // Get target components
        trainController = GameObject.FindWithTag("Player").GetComponent<TrainController>();
    }

    protected override void Start()
    {
        base.Start();

        // TrainController 연결 확인
        if (trainController == null)
        {
            Debug.LogError("Mob: TrainController를 찾을 수 없습니다!", this.gameObject);
            this.enabled = false;
        }

        // Die 애니메이션 추가 전까진 임시로
        deathToDeactive = 3.0f;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // init Default value
        moveSpeed = 2.0f;
    }

    private void FixedUpdate()
    {
        if (trainController == null) return; // TrainController가 없으면 실행 중지

        // Move Direction Setting
        SetMoveDirection(targetRigid.position);

        // 기차의 'X위치 퍼센티지'를 기준으로 0~1 사이의 비율을 계산합니다.
        float speedRate = Mathf.InverseLerp(
            trainController.MinXPosition,      // 기차의 최소 X위치
            trainController.MaxXPosition,      // 기차의 최대 X위치
            trainController.transform.position.x // 기차의 현재 X좌표
        );

        float currentMultiplier;

        // 자신의 X좌표와 기차의 X좌표를 비교 (이 로직은 그대로 유지)
        if (transform.position.x > targetRigid.position.x)
        {
            // [상황] 내가 기차보다 앞에 있을 때
            // 기차가 오른쪽으로 갈수록(speedRate → 1.0), 나도 최대 배율(maxMultiplier)로 빨라집니다.
            currentMultiplier = Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, speedRate);
        }
        else
        {
            // [상황] 내가 기차보다 뒤에 있을 때
            // 기차가 오른쪽으로 갈수록(speedRate → 1.0), 나는 최소 배율(minMultiplier)로 느려집니다.
            currentMultiplier = Mathf.Lerp(maxSpeedMultiplier, minSpeedMultiplier, speedRate);
        }

        // 죽으면 왼쪽으로 확 날아가게
        if (!isAlive)
        {
            moveDirection           = Vector2.left;
            float deathMoveSpeed    = 20.0f * currentMultiplier;

            rigid2D.linearVelocity = new Vector2(deathMoveSpeed * moveDirection.x, rigid2D.linearVelocity.y);

            return;
        }

        // 최종 속력 = 적의 기본 속력 * 현재 계산된 배율
        float finalMoveSpeed = moveSpeed * currentMultiplier;

        // 이동 로직 (기존과 동일)
        if (finalMoveSpeed > 0 && isAlive == true)
        {
            rigid2D.linearVelocity = new Vector2(finalMoveSpeed * moveDirection.x, rigid2D.linearVelocity.y);
        }
    }

    /// <summary>
    /// 1D move direction setting for GroundMob
    /// </summary>
    protected virtual void SetMoveDirection(Vector2 targetPos)
    {
        float x = targetPos.x - transform.position.x;

        if (x > 0f)
        {
            moveDirection = Vector2.right;
            sprite.flipX = (moveDirection.x > 0f);
            return;
        }
        if (x < 0f)
        {
            moveDirection = Vector2.left;
            sprite.flipX = (moveDirection.x > 0f);
            return;
        }

        moveDirection = Vector2.zero;

        // Set sprite to move direction
        sprite.flipX = (moveDirection.x > 0f);

        return;
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Train"))
        {
            Train train = collision.transform.GetComponentInParent<Train>();

            if (train != null)
            {
                train.TakeDamage(damage); // Train 스크립트에 맞게 수정 필요
                if (CameraShakeManager.Instance != null)
                {
                    CameraShakeManager.Instance.ShakeCamera(); // 기본 설정으로 흔들기
                                                               // 또는 원하는 값으로 흔들기: CameraShakeManager.Instance.ShakeCamera(0.3f, 1f, 15, 90f);
                }
            }

            StartCoroutine(Die());
        }
    }

    public void Knockback(Vector2 direction, float power)
    {
        Vector2 force = direction * power;

        rigid2D.AddForce(force, ForceMode2D.Impulse);
    }

    protected override IEnumerator Die()
    {
        yield return base.Die(); // base.Die()가 isAlive = false 처리

        yield return new WaitForSeconds(1.0f); // 1초 대기

        sprite.enabled = false;
        gameObject.SetActive(false);

        if (OnDied != null) OnDied(this);
    }
}