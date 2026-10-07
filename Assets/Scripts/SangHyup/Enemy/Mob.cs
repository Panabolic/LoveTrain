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

    protected Train playerTrain;
    protected override EnemyRewardKind RewardKind => isEliteMob ? EnemyRewardKind.Elite : EnemyRewardKind.Normal;

    private readonly KnockbackState knockback = new KnockbackState();
    protected bool isStunned { get => knockback.IsStunned; set => knockback.IsStunned = value; }
    private float stunDuration  = 0.5f;

    public Action<Mob> OnDied;

    protected override void Awake()
    {
        base.Awake();
        if (targetRigid != null) playerTrain = targetRigid.GetComponent<Train>();

        // Get components
        rigid2D     = GetComponent<Rigidbody2D>();
        hitEffect   = GetComponent<ParticleSystem>();
        RuntimeHost.BindFixedUpdate(AdvanceMovement);
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
        // Falling behind is not a kill: return the pooled enemy without farming rewards.
        if (!isAlive || !hasEnteredScreen || playerTrain == null || Camera.main == null ||
            playerTrain.RelativeWorldSpeed <= moveSpeed || GameManager.Instance == null ||
            (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Boss)) return;
        if (Camera.main.WorldToViewportPoint(transform.position).x < -0.15f) DespawnWithoutExp();
    }

    private void FixedUpdate()
    {
        RuntimeHost.FixedUpdate(Time.fixedDeltaTime);
    }

    private void AdvanceMovement(float deltaTime)
    {
        if (rigid2D == null) return;

        // Right after death
        if (!isAlive && !isStunned)
        {
            moveDirection           = Vector2.left;
            rigid2D.linearVelocity = MovementRules.DeathVelocity(rigid2D.linearVelocity.y);

            return;
        }

        // Movement Logic
        if (isAlive && !isStunned)
        {
            if (targetRigid == null) return;

            SetMoveDirection(targetRigid.position);

            rigid2D.linearVelocity = PlanVelocity(playerTrain != null ? playerTrain.RelativeWorldSpeed : 0f);
        }
    }

    /// <summary>
    /// 1D move direction setting for GroundMob
    /// </summary>
    protected virtual void SetMoveDirection(Vector2 targetPos)
    {
        moveDirection = MovementRules.GroundDirection(transform.position, targetPos);
        RuntimeHost.Presentation.SetFlipX(moveDirection.x > 0f);
    }

    protected virtual Vector2 PlanVelocity(float worldSpeed)
    {
        return MovementRules.GroundVelocity(moveDirection, moveSpeed, rigid2D.linearVelocity.y, worldSpeed);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isAlive) return;
        if (Time.timeScale <= 0f || GameManager.Instance == null ||
            (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Boss)) return;
        if (collision.gameObject.layer == LayerMask.NameToLayer("Train"))
        {
            Train train = collision.transform.GetComponentInParent<Train>();

            if (train != null)
            {
                train.TakeCollisionDamage(isEliteMob ? 15f : 10f, isEliteMob ? 30f : 20f);
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
        if (GameManager.Instance == null || PoolManager.instance == null)
        {
            calibratedMaxHP = hp;
            return calibratedMaxHP;
        }

        // [수정 1] 게임 시간(분)을 정수(int)로 변환하여 소수점 버림
        // 예: 0분 59초(0.9xxx) -> 0, 1분 1초(1.0xxx) -> 1
        int gameTimeMin = (int)(GameManager.Instance.gameTime / 60.0f);

        // 엘리트 몹 보정
        float eliteMultiplier = isEliteMob ? 1.5f : 1.0f;

        // 최종 보정 체력 계산
        calibratedMaxHP = HealthState.TimeScaled(hp, gameTimeMin, PoolManager.instance.hpIncrease,
            PoolManager.instance.eventDebuff, eliteMultiplier);

        return calibratedMaxHP;
    }

    public override void TakeDamage(float damageAmount)
    {
        base.TakeDamage(damageAmount);

        if (hitEffect != null) hitEffect.Play();
    }

    public void Knockback(Vector2 direction, float power)
    {
        if (rigid2D == null) return;

        Vector2 force = KnockbackState.Impulse(direction, power);

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
        if (!TryBeginDeath()) yield break;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddKillCount(isEliteMob);
        }

        yield return base.Die(); // base.Die()가 isAlive = false 처리

        SoundEventBus.Publish(SoundID.Enemy_Die);

        if (sprite != null) sprite.enabled = false;
        CompleteDeathPresentation();
        gameObject.SetActive(false);

        if (OnDied != null) OnDied(this);
    }
}
