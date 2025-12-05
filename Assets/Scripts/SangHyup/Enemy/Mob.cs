using System;
using System.Collections;
using UnityEngine;

public class Mob : Enemy
{
    // Components
    protected Rigidbody2D       rigid2D;
    protected ParticleSystem    hitEffect;

    [Tooltip("Mob Specification")]
    [SerializeField] protected float    moveSpeed   = 2.0f;
    [SerializeField] protected bool     isEliteMob  = false;

    protected Vector2 moveDirection = Vector2.zero;

    protected bool isStunned = false;
    private float stunDuration = 0.5f;

    public Action<Mob> OnDied;

    protected override void Awake()
    {
        base.Awake();

        // Get components
        rigid2D = GetComponent<Rigidbody2D>();
        hitEffect = GetComponent<ParticleSystem>();
    }

    protected override void Start()
    {
        base.Start();

        // 초기화
        deathToDeactive = 3.0f; // Die 애니메이션 추가 전까진 임시로
    }

    private void FixedUpdate()
    {
        // Right after death
        if (!isAlive && !isStunned)
        {
            moveDirection           = Vector2.left;
            float deathMoveSpeed    = 30.0f;

            rigid2D.linearVelocity  = new Vector2(moveDirection.x * deathMoveSpeed, rigid2D.linearVelocity.y);

            return;
        }

        // Movement Logic
        if (isAlive && !isStunned)
        {
            SetMoveDirection(targetRigid.position);

            rigid2D.linearVelocity = new Vector2(moveDirection.x * moveSpeed, rigid2D.linearVelocity.y);
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

    private void OnTriggerEnter2D(Collider2D collision)
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

/*    protected virtual void OnCollisionEnter2D(Collision2D collision)
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
    }*/

    protected override float CalculateCalibratedHP()
    {
        // 체력 보정 공식
        // (기본체력 x (1 + (게임시간(분) x 체력증가율%)) x 이벤트 디버프) x 엘리트 몹 보정

        // 게임 시간(분) 및 체력 증가율 계산
        float gameTimeMin   = GameManager.Instance.gameTime / 60.0f;
        float hpIncrease    = 1.0f + (PoolManager.instance.hpIncrease / 100.0f);

        // 체력 보정값 및 이벤트 디버프 계산
        float calibratedValue   = 1.0f + (gameTimeMin * hpIncrease);
        float eventDebuff       = 1.0f + (PoolManager.instance.eventDebuff / 100.0f);

        // 엘리트 몹 보정
        float eliteMultiplier = isEliteMob ? 1.5f : 1.0f;

        // 최종 보정 체력 계산
        calibratedHP = hp * calibratedValue * eventDebuff * eliteMultiplier;

        return calibratedHP;
    }

    public override void TakeDamage(float damageAmount)
    {
        base.TakeDamage(damageAmount);

        hitEffect.Play();
    }

    public void Knockback(Vector2 direction, float power)
    {
        Vector2 force = direction.normalized * power;

        StartCoroutine(Stun());
        rigid2D.AddForce(force, ForceMode2D.Impulse);
    }

    protected IEnumerator Stun()
    {
        isStunned = true;

        yield return new WaitForSeconds(stunDuration);

        isStunned = false;
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