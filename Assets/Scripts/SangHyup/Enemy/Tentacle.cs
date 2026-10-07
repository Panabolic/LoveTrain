using System; // ✨ Action 사용을 위해 필수
using System.Collections;
using UnityEngine;

public class Tentacle : Enemy
{
    private EyeBoss owner;

    [Header("Base Settings")]
    [SerializeField] private float toDestroy = 0.3f; // 공격 후 소멸 대기 시간

    // 보스에게서 받아올 변수들
    private float attackWaitTime;
    private float animationLength;
    private readonly TimedStrike strike = new TimedStrike();
    private Coroutine attackCoroutine;
    private Camera gameCamera;
    public bool AttackStarted => strike.AttackStarted;
    private static readonly int AttackState = Animator.StringToHash("EyeBoss_Attack");

    // ✨ [추가] 공격 시 실행할 콜백 (보스의 사운드 함수)
    private Action onAttackCallback;

    // ✨ [수정] Setup에서 Action을 받아옵니다.
    public void Setup(EyeBoss owner, float damageAmount, float waitTime, Action onAttackCallback)
    {
        this.owner = owner;
        this.damage = damageAmount;
        this.attackWaitTime = waitTime;
        gameCamera = Camera.main;
        strike.Reset();
        animationLength = 1f;

        // 보스가 전달해준 사운드 재생 함수 저장
        this.onAttackCallback = onAttackCallback;

        if (this.owner != null)
        {
            this.owner.RegisterTentacle(gameObject);
        }

        // 애니메이션 길이 미리 계산
        if (animator != null)
        {
            if (animator.runtimeAnimatorController != null)
            {
                animationLength = 1.0f; // 기본값

                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                {
                    if (clip.name.Contains("Attack") || clip.name.Contains("attack"))
                    {
                        animationLength = clip.length;
                        break;
                    }
                }
            }
        }
    }

    public void BeginAttack()
    {
        if (attackCoroutine == null && isAlive) attackCoroutine = StartCoroutine(AttackRoutine());
    }

    protected override void Update()
    {
        base.Update();
        if (!isAlive) return;
        if (owner == null || !owner.GetIsAlive() || (GameManager.Instance != null &&
            (GameManager.Instance.CurrentState == GameState.Die || GameManager.Instance.CurrentState == GameState.Ending)))
            CancelAttack();
        else if (CombatIsRunning() && !AttackStarted && !WarningIsVisible(gameCamera, transform.position))
            CancelAttack();
    }

    private void OnTriggerEnter2D(Collider2D other) => TryDamageTrain(other);
    private void OnTriggerStay2D(Collider2D other) => TryDamageTrain(other);

    private void TryDamageTrain(Collider2D other)
    {
        if (!isAlive || !strike.IsActive || !CombatIsRunning()) return;
        Train train = other.GetComponentInParent<Train>();
        if (train != null && strike.TryConsumeHit(train)) train.TakeDamage(damage);
    }

    public static bool WarningIsVisible(Camera camera, Vector3 position)
    {
        if (camera == null) return false;
        Vector3 viewport = camera.WorldToViewportPoint(position);
        return viewport.z > 0f && viewport.x >= 0.03f && viewport.x <= 0.97f &&
            viewport.y >= 0.03f && viewport.y <= 0.97f;
    }

    private static bool CombatIsRunning()
    {
        return Time.timeScale > 0f && GameManager.Instance != null &&
            (GameManager.Instance.CurrentState == GameState.Playing || GameManager.Instance.CurrentState == GameState.Boss);
    }

    private IEnumerator AttackRoutine()
    {
        // Unparented world placement keeps the warning and eventual strike aligned
        // while the belt/camera move. Offscreen warnings are cancelled and rerolled.
        strike.BeginWait(attackWaitTime);
        while (strike.Remaining > 0f)
        {
            strike.Advance(Time.deltaTime, CombatIsRunning());
            yield return null;
        }
        while (!CombatIsRunning()) yield return null;
        if (!WarningIsVisible(gameCamera, transform.position))
        {
            CancelAttack();
            yield break;
        }

        strike.MarkAttackStarted();
        if (animator != null)
        {
            RuntimeHost.Presentation.Trigger("attack");
            // Both existing controllers use this state. Avoid their warning exit
            // time/transition delaying the strike beyond the advertised deadline.
            animator.Play(AttackState, 0, 0f);
        }
        strike.BeginAttack(animationLength);
        onAttackCallback?.Invoke();
        strike.BeginWait(animationLength);
        while (strike.Remaining > 0f)
        {
            strike.Advance(Time.deltaTime, CombatIsRunning());
            yield return null;
        }
        strike.EndAttack(toDestroy);
        while (strike.Remaining > 0f)
        {
            strike.Advance(Time.deltaTime, CombatIsRunning());
            yield return null;
        }
        isAlive = false;
        Destroy(gameObject);
    }

    private void CancelAttack()
    {
        isAlive = false;
        strike.Cancel();
        StopAllCoroutines();
        if (collision != null) collision.enabled = false;
        Destroy(gameObject);
    }

    public override void TakeDamage(float damageAmount)
    {
        if (!isAlive) return;

        ApplyHealthDamage(damageAmount);
        StartCoroutine(HitEffect());
        SoundEventBus.Publish(SoundID.Enemy_Hit);

        if (HealthIsDepleted)
        {
            isAlive = false;
            strike.Cancel();
            StopAllCoroutines();
            if (collision != null) collision.enabled = false;
            if (killParticle != null)
            {
                Instantiate(killParticle, transform.position, Quaternion.identity);
            }

            SoundEventBus.Publish(SoundID.Enemy_Die);
            Destroy(gameObject);
        }
    }

    protected override float CalculateCalibratedHP()
    {
        if (GameManager.Instance == null || PoolManager.instance == null) return hp;

        int gameTimeMin = (int)(GameManager.Instance.gameTime / 60.0f);
        calibratedMaxHP = HealthState.TimeScaled(hp, gameTimeMin, PoolManager.instance.hpIncrease,
            PoolManager.instance.eventDebuff);
        return calibratedMaxHP;
    }

    private void OnDestroy()
    {
        if (owner != null)
        {
            owner.UnregisterTentacle(gameObject);
        }
    }

    protected override void OnDisable()
    {
        strike.Cancel();
        StopAllCoroutines();
        attackCoroutine = null;
        if (owner != null) owner.UnregisterTentacle(gameObject);
        base.OnDisable();
    }

    public float GetTotalDuration()
    {
        return attackWaitTime + (animationLength > 0 ? animationLength : 1.0f) + toDestroy;
    }
}
