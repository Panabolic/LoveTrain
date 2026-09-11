using System;
using System.Collections;
using UnityEngine;

public class TrainBoss : Boss
{
    // Components
    protected Rigidbody2D rigid2D;

    [Header("Train Boss Specification")]
    [Tooltip("이동 속도 (km/h)")]
    [SerializeField] private float moveSpeed = 20.0f;

    [Header("Knockback Settings (Fixed Force)")]
    [Tooltip("Phase 1 피격 시 넉백 힘 (고정값)")]
    [SerializeField] private float p1KnockbackForce = 10.0f;

    [Tooltip("Phase 2 전환 체력 비율")]
    [Range(0f, 1f)]
    [SerializeField] private float p2HpRatio = 0.4f;

    [Tooltip("Phase 2 피격 시 넉백 힘 (고정값)")]
    [SerializeField] private float p2KnockbackForce = 5.0f;

    [Tooltip("넉백 지속시간 (second)")]
    [Range(0f, 1.0f)]
    [SerializeField] private float stunDuration = 0.4f;
    private bool isStunned = false;

    private bool isPhase2 = false;

    // 넉백 쿨타임 (다단히트 방지)
    private float knockbackCooldown = 0.2f;
    private float lastKnockbackTime = -999f;

    private Vector2 moveDirection = Vector2.zero;

    [Header("Phase Colliders")]
    [Tooltip("1페이즈용 콜라이더")]
    [SerializeField] private Collider2D phase1Collider;
    [Tooltip("2페이즈용 콜라이더")]
    [SerializeField] private Collider2D phase2Collider;

    protected override void Awake()
    {
        base.Awake();
        rigid2D = GetComponent<Rigidbody2D>();
        SoundEventBus.Publish(SoundID.Boss_TrainBossSpawn);

        // ✨ [추가] 콜라이더 초기화 (1페이즈 ON, 2페이즈 OFF)
        if (phase1Collider != null) phase1Collider.enabled = true;
        if (phase2Collider != null) phase2Collider.enabled = false;
    }

    private void FixedUpdate()
    {
        if (rigid2D == null || targetRigid == null) return;

        // 2. 방향 설정 (무조건 왼쪽)
        SetMoveDirection(targetRigid.position);

        // 3. 스턴 상태가 아니면 이동 (플레이어 생존 여부 상관없이 계속 전진)
        if (!isStunned)
        {
            rigid2D.linearVelocity = new Vector2(moveDirection.x * moveSpeed, rigid2D.linearVelocity.y);
        }
    }

    protected virtual void SetMoveDirection(Vector2 targetPos)
    {
        // 플레이어 위치와 상관없이 무조건 왼쪽으로 이동
        moveDirection = Vector2.left;
        if (sprite != null) sprite.flipX = false;
    }

    public override void TakeDamage(float damageAmount)
    {
        if (!isAlive || !hasEnteredScreen) return;
        base.TakeDamage(damageAmount);

        CheckPhase();

        // 넉백 쿨타임 체크
        if (Time.time >= lastKnockbackTime + knockbackCooldown)
        {
            Knockback();
            lastKnockbackTime = Time.time;
        }
    }

    private void CheckPhase()
    {
        // 아직 페이즈 2 체력이 아니거나, 이미 페이즈 2라면 리턴
        if (currentHP > calibratedMaxHP * p2HpRatio || isPhase2 == true) return;

        // --- 페이즈 2 진입 ---
        if (!isPhase2)
        {
            SoundEventBus.Publish(SoundID.Boss_Roar);
        }
        isPhase2 = true;
        if (animator != null) animator.SetTrigger("phase2");

        // ✨ [추가] 콜라이더 교체
        if (phase1Collider != null) phase1Collider.enabled = false;
        if (phase2Collider != null) phase2Collider.enabled = true;

        Debug.Log("TrainBoss: Entered Phase 2! Collider Switched.");
    }

    private void Knockback()
    {
        if (rigid2D == null) return;

        // 고정된 힘(Force) 사용
        float currentKnockbackForce = isPhase2 ? p2KnockbackForce : p1KnockbackForce;

        // 보스는 왼쪽으로 가므로 넉백은 오른쪽(+)
        Vector2 force = new Vector2(currentKnockbackForce, 0);

        StopCoroutine("Stun");
        StartCoroutine("Stun");

        // 확실한 넉백을 위해 속도 초기화 후 힘 적용
        rigid2D.linearVelocity = Vector2.zero;
        rigid2D.AddForce(force, ForceMode2D.Impulse);
    }

    private IEnumerator Stun()
    {
        isStunned = true;
        yield return new WaitForSeconds(stunDuration);
        isStunned = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isAlive) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Train"))
        {
            Train train = collision.transform.GetComponentInParent<Train>();

            if (train != null)
            {
                train.TakeDamage(damage, true);
                if (CameraShakeManager.Instance != null)
                {
                    CameraShakeManager.Instance.ShakeCamera(0.3f, 1f, 15, 90f);
                }
            }
        }
    }

    protected override IEnumerator Die()
    {
        yield return base.Die();
        Vector2 explosionEffectPivot = new Vector2(-5f, 2f);

        if (killExplosionEffect != null)
        {
            Instantiate(killExplosionEffect, transform.position + (Vector3)explosionEffectPivot, Quaternion.identity);
        }
        SoundEventBus.Publish(SoundID.Boss_Die);
        yield return new WaitForSeconds(2.0f); // 사망 연출 대기

        if (killEvent != null)
        {
            if (GameManager.Instance != null &&
                EventManager.Instance != null &&
                !GameManager.Instance.IsTimeForEnding)
            {
                EventManager.Instance.RequestEvent(killEvent);
            }
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddBossKillCount();
            GameManager.Instance.BossDied();
        }
        else
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.StartStageTransitionSequence();
            }
        }

        Destroy(gameObject);
    }
}
