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
    private readonly KnockbackState knockback = new KnockbackState();
    private bool isStunned { get => knockback.IsStunned; set => knockback.IsStunned = value; }

    private bool isPhase2 = false;

    // 넉백 쿨타임 (다단히트 방지)
    private float knockbackCooldown = 0.2f;

    private Vector2 moveDirection = Vector2.zero;
    private const float ContactPushSpeed = 7f;
    private BossContactTracker contacts;

    [Header("Phase Colliders")]
    [Tooltip("1페이즈용 콜라이더")]
    [SerializeField] private Collider2D phase1Collider;
    [Tooltip("2페이즈용 콜라이더")]
    [SerializeField] private Collider2D phase2Collider;

    protected override void Awake()
    {
        base.Awake();
        rigid2D = GetComponent<Rigidbody2D>();
        contacts = new BossContactTracker(ResolveContact, OverlapsActivePhase, () =>
        {
            if (CameraShakeManager.Instance != null)
                CameraShakeManager.Instance.ShakeCamera(0.3f, 1f, 15, 90f);
        });
        RuntimeHost.BindFixedUpdate(AdvanceMovement);
        SoundEventBus.Publish(SoundID.Boss_TrainBossSpawn);

        // ✨ [추가] 콜라이더 초기화 (1페이즈 ON, 2페이즈 OFF)
        if (phase1Collider != null) phase1Collider.enabled = true;
        if (phase2Collider != null) phase2Collider.enabled = false;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        knockback.Reset();
        isPhase2 = false;
        if (phase1Collider != null) phase1Collider.enabled = true;
        if (phase2Collider != null) phase2Collider.enabled = false;
    }

    private void FixedUpdate()
    {
        RuntimeHost.FixedUpdate(Time.fixedDeltaTime);
    }

    private void AdvanceMovement(float deltaTime)
    {
        if (!isAlive || !CombatIsRunning())
        {
            ClearTrainContacts();
            return;
        }
        contacts.Advance(ContactPushSpeed * deltaTime);
        if (rigid2D == null || targetRigid == null) return;

        // 2. 방향 설정 (무조건 왼쪽)
        SetMoveDirection(targetRigid.position);

        // 3. 스턴 상태가 아니면 이동 (플레이어 생존 여부 상관없이 계속 전진)
        if (!isStunned)
        {
            rigid2D.linearVelocity = MovementRules.GroundVelocity(moveDirection, moveSpeed, rigid2D.linearVelocity.y, 0f);
        }
    }

    protected virtual void SetMoveDirection(Vector2 targetPos)
    {
        // 플레이어 위치와 상관없이 무조건 왼쪽으로 이동
        moveDirection = Vector2.left;
        RuntimeHost.Presentation.SetFlipX(false);
    }

    public override void TakeDamage(float damageAmount)
    {
        if (!isAlive || !hasEnteredScreen) return;
        base.TakeDamage(damageAmount);
        if (!isAlive) return;

        CheckPhase();

        // 넉백 쿨타임 체크
        if (knockback.CanApply(Time.time, knockbackCooldown))
        {
            Knockback();
            knockback.MarkApplied(Time.time);
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
        RuntimeHost.Presentation.Trigger("phase2");

        // ✨ [추가] 콜라이더 교체
        if (phase1Collider != null) phase1Collider.enabled = false;
        if (phase2Collider != null) phase2Collider.enabled = true;
        RefreshTrainContacts();

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

    private void OnTriggerEnter2D(Collider2D other) => RegisterTrainContact(other);
    private void OnTriggerStay2D(Collider2D other) => RegisterTrainContact(other);
    private void OnCollisionEnter2D(Collision2D other) => RegisterTrainContact(other.collider);
    private void OnCollisionStay2D(Collision2D other) => RegisterTrainContact(other.collider);
    private void OnCollisionExit2D(Collision2D other) => OnTriggerExit2D(other.collider);

    private void RegisterTrainContact(Collider2D other)
    {
        if (!isAlive || !CombatIsRunning() || other == null) return;
        contacts.Register(other);
    }

    private BossContactTracker.Binding ResolveContact(Collider2D other)
    {
        Train train = other.GetComponentInParent<Train>();
        if (train == null) return default;
        TrainController controller = train.GetComponent<TrainController>();
        return new BossContactTracker.Binding(train, () => train != null && !train.IsDead,
            active => { if (train != null) train.SetBossContact(this, active); },
            distance => { if (controller != null) controller.PushLeft(distance); });
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Phase changes may deliver old-collider Exit after new-collider Enter.
        // Keep the reservation while either current phase collider still overlaps.
        contacts.Exit(other);
    }

    private void RefreshTrainContacts()
    {
        contacts.Refresh();
    }

    private bool OverlapsActivePhase(Collider2D other)
    {
        if (other == null || !other.enabled) return false;
        return Overlaps(phase1Collider, other) || Overlaps(phase2Collider, other);
    }

    private static bool Overlaps(Collider2D bossCollider, Collider2D other)
    {
        return bossCollider != null && bossCollider.enabled && bossCollider.gameObject.activeInHierarchy &&
            bossCollider.Distance(other).distance <= 0.02f;
    }

    private static bool CombatIsRunning()
    {
        return Time.timeScale > 0f && GameManager.Instance != null &&
            (GameManager.Instance.CurrentState == GameState.Playing || GameManager.Instance.CurrentState == GameState.Boss);
    }

    private void ClearTrainContacts()
    {
        contacts?.Clear();
    }

    protected override void OnDisable()
    {
        ClearTrainContacts();
        StopAllCoroutines();
        base.OnDisable();
    }

    private void OnDestroy() => ClearTrainContacts();

    protected override IEnumerator Die()
    {
        if (!TryBeginDeath()) yield break;
        ClearTrainContacts();
        if (phase1Collider != null) phase1Collider.enabled = false;
        if (phase2Collider != null) phase2Collider.enabled = false;
        if (rigid2D != null) rigid2D.linearVelocity = Vector2.zero;
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

        CompleteDeathPresentation();
        Destroy(gameObject);
    }
}
