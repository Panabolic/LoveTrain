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

    protected override void Awake()
    {
        base.Awake();
        rigid2D = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // 1. 보스가 죽었을 때 (왼쪽으로 퇴장)
        if (!isAlive)
        {
            moveDirection = Vector2.left;
            float deathMoveSpeed = 30.0f;
            rigid2D.linearVelocity = new Vector2(moveDirection.x * deathMoveSpeed, rigid2D.linearVelocity.y);
            return;
        }

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
        sprite.flipX = false;
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
        if (currentHP > calibratedMaxHP * p2HpRatio && isPhase2 == false) return;

        isPhase2 = true;
        animator.SetTrigger("phase2");
        Debug.Log("TrainBoss: Entered Phase 2!");
    }

    private void Knockback()
    {
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
        yield return new WaitForSeconds(2.0f); // 사망 연출 대기

        // ✨ [수정] 킬 이벤트(보상) 띄우기 전 엔딩 여부 체크
        if (killEvent != null)
        {
            // GameManager가 있고, 아직 엔딩 시간이 아니라면 -> 보상 획득 이벤트 실행
            // (엔딩 시간이면 보상 창 안 띄움)
            if (GameManager.Instance != null && !GameManager.Instance.IsTimeForEnding)
            {
                EventManager.Instance.RequestEvent(killEvent);
            }
        }

        // ✨ [수정] GameManager에게 사망 보고 (엔딩/스테이지 전환 위임)
        if (GameManager.Instance != null)
        {
            // 보스 킬 카운트 증가 + 엔딩 판정 요청
            GameManager.Instance.AddBossKillCount();
            GameManager.Instance.BossDied();
        }
        else
        {
            // 비상용 (GameManager 없을 때)
            StageManager.Instance.StartStageTransitionSequence();
        }

        Destroy(gameObject);
    }
}