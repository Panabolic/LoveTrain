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

    // 넉백 쿨타임
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

        // 2. ✨ [수정] 플레이어 사망 여부와 상관없이 이동 로직 실행
        // (정지 로직 삭제함)

        // 3. 방향 설정 (무조건 왼쪽)
        SetMoveDirection(targetRigid.position);

        // 4. 스턴 상태가 아니면 이동
        if (!isStunned)
        {
            rigid2D.linearVelocity = new Vector2(moveDirection.x * moveSpeed, rigid2D.linearVelocity.y);
        }
    }

    protected virtual void SetMoveDirection(Vector2 targetPos)
    {
        // ✨ [핵심 수정] 플레이어 위치와 상관없이 무조건 왼쪽으로 이동
        moveDirection = Vector2.left;
        sprite.flipX = false;
    }

    public override void TakeDamage(float damageAmount)
    {
        base.TakeDamage(damageAmount);

        CheckPhase();

        if (Time.time >= lastKnockbackTime + knockbackCooldown)
        {
            Knockback();
            lastKnockbackTime = Time.time;
        }
    }

    private void CheckPhase()
    {
        if (currentHP > hp * p2HpRatio && isPhase2 == false) return;

        isPhase2 = true;
        animator.SetTrigger("phase2");
        Debug.Log("TrainBoss: Entered Phase 2!");
    }

    private void Knockback()
    {
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
        yield return new WaitForSeconds(3.0f);

        if (killEvent != null)
        {
            EventManager.Instance.RequestEvent(killEvent);
        }

        StageManager.Instance.StartStageTransitionSequence();
        Destroy(gameObject);
    }
}