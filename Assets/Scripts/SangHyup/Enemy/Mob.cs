using System;
using System.Collections;
using UnityEngine;

public class Mob : Enemy
{
    // Components
    protected Rigidbody2D       rigid2D;
    protected ParticleSystem    hitEffect;

    [Tooltip("기본 속도")]
    [SerializeField] protected float moveSpeed = 4.0f;

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

    protected override void OnEnable()
    {
        base.OnEnable();
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