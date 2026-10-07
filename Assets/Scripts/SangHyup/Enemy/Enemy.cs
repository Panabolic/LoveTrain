using System.Collections;
using UnityEngine;
using System;

public class Enemy : MonoBehaviour
{
    // Components (Null일 수 있음)
    protected SpriteRenderer sprite;
    protected Collider2D collision;
    protected Animator animator;
    protected Material material;

    [Header("Enemy Specification")]
    [SerializeField] protected float hp;
    [SerializeField] protected float damage;
    [SerializeField] protected float exp;
    [SerializeField] protected float hitEffectDuration = 0.05f;
    [SerializeField] protected GameObject killParticle;
    [Header("Reward pickups")]
    [SerializeField] private RewardPickup fleshPickupPrefab;
    [SerializeField] private RewardPickup soulPickupPrefab;

    private readonly HealthState health = new HealthState();
    private readonly DeathLifecycle death = new DeathLifecycle();
    // Compatibility accessors keep derived adapters on one authoritative state object.
    protected float calibratedMaxHP { get => health.Maximum; set => health.Maximum = value; }
    protected float currentHP { get => health.Current; set => health.Current = value; }
    protected bool isAlive { get => death.IsAlive; set => death.IsAlive = value; }
    protected bool deathRewardGranted { get => death.RewardGranted; set => death.RewardGranted = value; }
    protected virtual EnemyRewardKind RewardKind => EnemyRewardKind.None;

    protected bool hasEnteredScreen = false;
    private Color originalColor;
    protected float deathToDeactive;

    // Target Components
    protected Rigidbody2D targetRigid;
    protected TrainLevelManager levelManager;

    public bool IsTargetable => isAlive && hasEnteredScreen;
    private TargetHandle combatTarget;
    private protected ObjectHost RuntimeHost { get; private set; }
    public TargetHandle CombatTarget
    {
        get
        {
            if (combatTarget == null)
            {
                Mob mob = this as Mob;
                combatTarget = new TargetHandle(this, () => isAlive, () => IsTargetable,
                    TakeDamage, DespawnWithoutExp, GetComponent<Boss>() != null,
                    mob != null ? (Action<Vector2, float>)mob.Knockback : null);
            }
            return combatTarget;
        }
    }
    protected bool TryBeginDeath() { return death.TryBegin(); }
    protected void ApplyHealthDamage(float amount) { health.ApplyDamage(amount); }
    protected bool HealthIsDepleted => health.IsDepleted;
    protected void ResetHealth(float maximum) { health.Reset(maximum); }
    protected void ResetRuntimeLifetime() { death.Reset(); RuntimeHost.Activate(); }
    protected void CompleteDeathPresentation() { death.CompletePresentation(); }

    protected virtual void Awake()
    {
        // ✨ [수정] 컴포넌트 가져오기 (없으면 null로 둠)
        sprite = GetComponent<SpriteRenderer>();
        collision = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();

        // ✨ [수정] 스프라이트가 있을 때만 재질 가져오기
        if (sprite != null)
        {
            material = sprite.material;
            originalColor = sprite.color;
        }

        // 플레이어 찾기 (이건 공통이므로 유지)
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            targetRigid = playerObj.GetComponent<Rigidbody2D>();
            levelManager = playerObj.GetComponent<TrainLevelManager>();
        }
        RuntimeHost = new ObjectHost(gameObject);
        RuntimeHost.BindUpdate(_ => AdvanceScreenEntry());
    }

    protected virtual void OnEnable()
    {
        currentHP = CalculateCalibratedHP();
        ResetRuntimeLifetime();

        // ✨ [수정] null 체크 추가
        if (sprite != null)
        {
            sprite.enabled = true;
            sprite.color = originalColor;
        }

        hasEnteredScreen = false;
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        if (PoolManager.instance != null)
        {
            PoolManager.instance.RegisterEnemy(this);
        }
    }

    protected virtual void Start()
    {
/*        // ✨ [수정] null 체크 추가
        if (animator != null)
        {
            AnimationClip[] animationClips = animator.runtimeAnimatorController.animationClips;
            foreach (AnimationClip clip in animationClips)
            {
                if (clip.name == "Die") deathToDeactive = clip.length;
            }
        }*/
    }

    protected virtual void Update()
    {
        RuntimeHost.Update(Time.deltaTime);
    }

    private void AdvanceScreenEntry()
    {
        if (!hasEnteredScreen && isAlive)
        {
            CheckScreenEntry();
        }
    }

    private void CheckScreenEntry()
    {
        if (Camera.main == null) return;

        float camHeight = Camera.main.orthographicSize * 2f;
        float camWidth = camHeight * Camera.main.aspect;
        Vector3 camPos = Camera.main.transform.position;
        Bounds camBounds = new Bounds(new Vector3(camPos.x, camPos.y, 0), new Vector3(camWidth, camHeight, 1000f));

        Bounds enemyBounds;

        // ✨ [수정] 있는 컴포넌트로 영역 계산 (Collider 우선 -> Sprite -> 점)
        if (collision != null)
        {
            enemyBounds = collision.bounds;
        }
        else if (sprite != null)
        {
            enemyBounds = sprite.bounds;
        }
        else
        {
            // 둘 다 없으면(예: CreditEnemy 초기화 전) 점 기준으로 체크
            Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
            if (viewPos.x >= 0f && viewPos.x <= 1f && viewPos.y >= 0f && viewPos.y <= 1f)
                hasEnteredScreen = true;
            return;
        }

        if (camBounds.Intersects(enemyBounds))
        {
            hasEnteredScreen = true;
        }
    }

    protected virtual float CalculateCalibratedHP()
    {
        calibratedMaxHP = hp;
        return calibratedMaxHP;
    }

    public virtual void TakeDamage(float damageAmount)
    {
        if (!isAlive || !hasEnteredScreen) return;

        health.ApplyDamage(damageAmount);

        // ✨ [수정] HitEffect는 material이 있을 때만 실행
        if (material != null) StartCoroutine(HitEffect());

        SoundEventBus.Publish(SoundID.Enemy_Hit);

        if (health.IsDepleted)
            StartCoroutine(Die());
    }

    protected IEnumerator HitEffect()
    {
        if (material == null) yield break; // 방어 코드

        material.SetInt("_isHit", 1);
        yield return new WaitForSeconds(hitEffectDuration);
        material.SetInt("_isHit", 0);
    }

    protected virtual IEnumerator Die()
    {
        if (!death.TryGrantReward()) yield break;
        // XP retains its original death-time reward; physical currency is paid on collection.
        if (levelManager != null) levelManager.GainExperience(exp);
        if (levelManager != null && RewardKind != EnemyRewardKind.None)
        {
            EnemyReward reward = EnemyRewardRules.Roll(RewardKind, UnityEngine.Random.Range(0, 100), UnityEngine.Random.Range(0, 100));
            RewardPickup.Spawn(fleshPickupPrefab, transform.position, levelManager, reward.Flesh);
            RewardPickup.Spawn(soulPickupPrefab, transform.position, levelManager, 0, reward.Souls);
        }

        Inventory inventory = levelManager?.GetComponent<Inventory>();
        if (inventory != null)
        {
            inventory.ProcessKillEvent(this.gameObject);
        }

        if (sprite != null)
        {
            RuntimeHost.Presentation.ShowSprite(false);
        }

        if (killParticle != null)
        {
            Instantiate(killParticle, gameObject.transform.position, gameObject.transform.rotation);
        }

        yield return new WaitForSeconds(0);
    }

    public virtual void DespawnWithoutExp()
    {
        if (!gameObject.activeInHierarchy) return;
        isAlive = false;
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    public bool GetIsAlive() { return isAlive; }

    protected virtual void OnDisable()
    {
        RuntimeHost?.Deactivate();
        if (PoolManager.instance != null)
        {
            if (material != null)
            {
                material.SetInt("_isHit", 0);
            }

            PoolManager.instance.UnregisterEnemy(this);
        }
    }
}

public enum EnemyRewardKind { None, Normal, Elite, Boss }

public struct EnemyReward
{
    public readonly int Flesh;
    public readonly int Souls;
    public EnemyReward(int flesh, int souls) { Flesh = flesh; Souls = souls; }
}

// Two independent [0, 100) rolls keep the flesh and soul distributions explicit.
public static class EnemyRewardRules
{
    public static EnemyReward Roll(EnemyRewardKind kind, int fleshRoll, int soulRoll)
    {
        if (fleshRoll < 0 || fleshRoll >= 100) throw new ArgumentOutOfRangeException(nameof(fleshRoll));
        if (soulRoll < 0 || soulRoll >= 100) throw new ArgumentOutOfRangeException(nameof(soulRoll));
        switch (kind)
        {
            case EnemyRewardKind.Normal:
                return new EnemyReward(fleshRoll < 50 ? 1 : fleshRoll < 80 ? 2 : 3, soulRoll < 80 ? 0 : 1);
            case EnemyRewardKind.Elite:
                return new EnemyReward(10, soulRoll < 60 ? 0 : soulRoll < 85 ? 1 : 2);
            case EnemyRewardKind.Boss:
                return new EnemyReward(100, soulRoll < 50 ? 1 : soulRoll < 80 ? 2 : 3);
            default:
                return new EnemyReward(0, 0);
        }
    }
}
