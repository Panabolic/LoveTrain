using DG.Tweening;
using System.Collections;
using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class Train : MonoBehaviour
{
    // Components
    public Animator[] carsAnim;
    private TrainController trainController;

    [Tooltip("기차의 속도 최대치 값입니다.")]
    [SerializeField] private float maxSpeedValue = 460;

    [Tooltip("기존 계기판의 저속 구간 경계입니다. 연료 체력과 사망에는 관여하지 않습니다.")]
    [SerializeField] private float deathSpeedThreshold = 160f;

    public float CurrentSpeed { get => drive.Speed; private set => drive.Speed = value; }

    [Header("Fuel and acceleration")]
    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private float baseSpeed = 320f;
    [SerializeField] private float acceleration = 70f;
    [SerializeField] private float fuelDrainPerSecond = 0.25f;
    [SerializeField] private float accelerationFuelMultiplier = 4f;
    [SerializeField] private float dashSpeedMultiplier = 1.5f;
    [SerializeField] private float dashDuration = 5f;
    [SerializeField] private float dashFuelPerSecond = 10f;
    public float CurrentFuel { get; private set; }
    public float MaxFuel => maxFuel;
    public bool IsDashing => drive.DashRemaining > 0f;
    private readonly TrainDriveState drive = new TrainDriveState();
    [SerializeField] private float decelerationDelay = 1f;
    [SerializeField] private float deceleration = 70f;

    [Header("디버그 정보")]
    [SerializeField] private float _currentSpeedForInspector;

    public float MaxSpeedValue => maxSpeedValue;
    public float BaseSpeed => baseSpeed;
    public float AccelerationProgress => Mathf.Clamp01((CurrentSpeed - baseSpeed) / Mathf.Max(1f, maxSpeedValue - baseSpeed));
    // World movement relative to the train; base speed preserves existing monster tuning.
    [SerializeField, Min(0f)] private float relativeSpeedScale = 0.02f;
    [SerializeField, Min(0.01f)] private float relativeSpeedChangeRate = 2f;
    private float relativeWorldSpeed;
    public float RelativeWorldSpeed => relativeWorldSpeed;

    [Header("사망 연출 설정")]
    [SerializeField] private float deathKnockbackPositionX = 0f;
    [SerializeField] private float deathKnockbackDuration = 0.5f;
    [SerializeField] private float deathMoveDistance = -15f;
    [SerializeField] private float deathMoveSpeed = 5f;
    [SerializeField] private float destroyDelay = 1.0f;

    [Header("사망 연출 오브젝트")]
    [SerializeField] private GameObject handObject;
    [SerializeField] private GameObject explosionObject;
    [SerializeField] private Transform handTargetPos;
    [SerializeField] private float handMoveDuration = 2.0f;
    [Header("사망 후 끌려가는 연출 설정")]
    [SerializeField] private Vector3 dragDestinationPos = new Vector3(-30f, 0, 0);
    [SerializeField] private float dragDuration = 3.0f;

    [Header("오버레이 연출")]
    [SerializeField] private Animator overlayBorderAnim;
    [SerializeField] private Animator overlayEffectAnim;

    // --- 상태 변수 ---
    private bool isDead = false;
    private bool isDying = false;

    // 손의 원래 위치 저장용 변수
    private Vector3 handInitialPos;

    [SerializeField]
    private GameObject tempDieUI;

    public event Action OnTrainDamaged;

    private void Awake()
    {
        carsAnim = GetComponentsInChildren<Animator>();
        trainController = GetComponent<TrainController>();
    }

    private void Start()
    {
        CurrentSpeed = Mathf.Min(baseSpeed, maxSpeedValue);
        CurrentFuel = maxFuel;
        drive.Reset();
        relativeWorldSpeed = 0f;
        _currentSpeedForInspector = CurrentSpeed;
        isDead = false;
        isDying = false;

        if (trainController != null) trainController.enabled = true;

        if (handObject != null)
        {
            handInitialPos = handObject.transform.position;
            handObject.SetActive(false);
        }

        SetCarAnimSpeed(0f);
    }

    void Update()
    {
        if (isDead) return;
        _currentSpeedForInspector = CurrentSpeed;

        bool combat = GameManager.Instance != null &&
            (GameManager.Instance.CurrentState == GameState.Playing || GameManager.Instance.CurrentState == GameState.Boss);
        if (!combat || Time.timeScale <= 0f) { SetCarAnimSpeed(0f); return; }
        bool shift = Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        float fuelUsed = drive.Advance(Time.deltaTime, shift, baseSpeed, maxSpeedValue, acceleration,
            deceleration, decelerationDelay, dashDuration, dashSpeedMultiplier,
            fuelDrainPerSecond, accelerationFuelMultiplier, dashFuelPerSecond);
        relativeWorldSpeed = Mathf.MoveTowards(relativeWorldSpeed,
            Mathf.Max(0f, CurrentSpeed - baseSpeed) * Mathf.Max(0f, relativeSpeedScale),
            Mathf.Max(0.01f, relativeSpeedChangeRate) * Time.deltaTime);
        ModifyFuel(-fuelUsed);
        SetCarAnimSpeed(CurrentSpeed * 0.05f);
    }

    public void ModifyFuel(float amount)
    {
        if (isDead) return;
        CurrentFuel = Mathf.Clamp(CurrentFuel + amount, 0f, maxFuel);
        if (CurrentFuel <= 0f) { drive.Reset(); relativeWorldSpeed = 0f; CurrentSpeed = 0f; Die(); }
    }

    private void SetCarAnimSpeed(float speed)
    {
        foreach (Animator carAnim in carsAnim)
        {
            carAnim.SetFloat("moveSpeed", speed);
        }
    }

    public virtual void TakeDamage(float damageAmount, bool isBossAttack = false)
    {
        if (isDead || IsDashing || damageAmount <= 0f) return;
        OnTrainDamaged?.Invoke();
        if (CameraShakeManager.Instance != null) CameraShakeManager.Instance.ShakeCamera();
        SoundEventBus.Publish(SoundID.Player_Hit);
        ModifyFuel(-damageAmount);
    }

    public void ModifySpeed(float amount)
    {
        if (isDead || IsDashing) return;
        CurrentSpeed = Mathf.Clamp(CurrentSpeed + amount, 0f, maxSpeedValue);
    }

    public void BossModifySpeed(float amount) => ModifySpeed(amount);

    // ========================================================================
    // ☠️ [핵심] 엔딩 진입 시 모든 상태 강제 초기화 함수
    // ========================================================================
    public void ForceStopDyingState()
    {
        // 1. 진행 중인 모든 코루틴 강제 중단
        StopAllCoroutines();

        ResetDyingPresentation(false);

        // 2. 상태 플래그 '생존'으로 강제 변경
        isDying = false;
        isDead = false;

        // 3. 물리/트윈 움직임 정지 및 부모 관계 해제
        transform.SetParent(null);

        // 6. 게임오버 UI 끄기
        if (tempDieUI != null) tempDieUI.SetActive(false);

        // Restore fuel and base speed for the ending sequence.
        CurrentSpeed = Mathf.Min(baseSpeed, maxSpeedValue);
        CurrentFuel = maxFuel;
        drive.Reset();
        relativeWorldSpeed = 0f;
        _currentSpeedForInspector = CurrentSpeed; // 인스펙터 갱신용

        // 8. 컨트롤러 비활성화 (엔딩 중 조작 방지)
        if (trainController != null) trainController.enabled = false;

        Debug.Log("🚂 엔딩 진입: 기차 상태 강제 정상화 완료 (연료 회복)");
    }

    private void ResetDyingPresentation(bool playEscapeTrigger)
    {

        transform.DOKill();
        if (handObject != null)
        {
            handObject.transform.DOKill();
            if (playEscapeTrigger)
            {
                handObject.transform.DOMove(handInitialPos, handMoveDuration)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true)
                    .OnComplete(() => handObject.SetActive(false));
            }
            else
            {
                handObject.transform.position = handInitialPos;
                handObject.SetActive(false);
            }
        }

        ResetOverlayAnimator(overlayBorderAnim);
        ResetOverlayAnimator(overlayEffectAnim);

        if (playEscapeTrigger)
        {
            SetOverlayTrigger("Escape");
        }
    }

    private void ResetOverlayAnimator(Animator animator)
    {
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger("Dying");
        animator.ResetTrigger("Escape");
        animator.Rebind();
        animator.Update(0f);
    }

    private void SetOverlayTrigger(string triggerName)
    {
        if (overlayBorderAnim != null) overlayBorderAnim.SetTrigger(triggerName);
        if (overlayEffectAnim != null) overlayEffectAnim.SetTrigger(triggerName);
    }

    private void Die()
    {
        // 이미 엔딩 상태라면 죽지 않음
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Ending) return;

        if (isDead) return;
        isDead = true;
        isDying = false;
        if (trainController != null) trainController.enabled = false;
        SetCarAnimSpeed(0f);
        SetOverlayTrigger("Dying");
        if (handObject != null) handObject.SetActive(true);

        if (handObject != null) handObject.transform.DOKill();

        Debug.Log("기차 완전 정지. 사망 처리.");
        transform.DOKill();
        if (GameManager.Instance != null) GameManager.Instance.PlayerDied();
        StartCoroutine(DragAndDestroySequence());
    }

    private IEnumerator DragAndDestroySequence()
    {
        if (handObject != null)
        {
            transform.SetParent(handObject.transform, true);
        }

        tempDieUICall();

        SoundEventBus.Publish(SoundID.Player_GameOver);

        if (handObject != null)
        {
            Vector3 targetPos = (dragDestinationPos != null) ? dragDestinationPos :
                                handObject.transform.position + new Vector3(-30f, 0f, 0f);
            targetPos.y = handObject.transform.position.y;

            yield return handObject.transform
                .DOMove(targetPos, dragDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .WaitForCompletion();
        }
        else
        {
            yield return new WaitForSecondsRealtime(destroyDelay);
        }

        Destroy(gameObject);
        SceneLoader.Instance.LoadStartScene();
    }

    private IEnumerator DestroyProcess()
    {
        tempDieUICall();
        isDead = true;
        isDying = false;
        yield return new WaitForSecondsRealtime(destroyDelay);
        Destroy(gameObject);
        SceneLoader.Instance.LoadStartScene();
    }

    public void IncreaseSpeedTest() { if (!isDead) ModifySpeed(20); }
    public void DecreaseSpeedTest() { if (!isDead) ModifySpeed(-20); }
    public void DieTest() { if (!isDead) ModifyFuel(-maxFuel); }
    public void IncreaseMaxSpeed(float amount) { maxSpeedValue += amount; }
    public void HealPercent(float percentage) { if (!isDead) ModifyFuel(maxFuel * percentage); }

    private void tempDieUICall() { if (tempDieUI != null) tempDieUI.SetActive(true); }
    public float GetDeathSpeed() { return deathSpeedThreshold; }
    public bool IsDead { get { return isDead; } }
    public bool IsDying { get { return isDying; } }
}

// Pure driving rules: pause is handled by the caller; no Editor or input dependency.
public sealed class TrainDriveState
{
    public float Speed;
    public float DashRemaining { get; private set; }
    private float releasedTime;
    private bool armed = true;
    public void Reset() { DashRemaining = 0f; releasedTime = 0f; armed = true; }

    private static float MoveTowards(float value, float target, float delta)
    {
        return Math.Abs(target - value) <= delta ? target : value + Math.Sign(target - value) * delta;
    }

    public float Advance(float dt, bool held, float baseSpeed, float maxSpeed, float acceleration,
        float deceleration, float delay, float dashDuration, float dashMultiplier,
        float baseDrain, float fuelMultiplier, float dashDrain)
    {
        if (dt <= 0f) return 0f;
        if (!held) armed = true;
        if (held) releasedTime = 0f;
        if (DashRemaining > 0f)
        {
            float usedTime = Math.Min(dt, DashRemaining);
            DashRemaining = Math.Max(0f, DashRemaining - dt);
            Speed = maxSpeed * Math.Max(1.01f, dashMultiplier);
            if (DashRemaining <= 0f) { Speed = maxSpeed; releasedTime = 0f; }
            return Math.Max(0f, dashDrain) * usedTime;
        }
        if (held) Speed = MoveTowards(Speed, maxSpeed, Math.Max(0f, acceleration) * dt);
        else
        {
            float before = releasedTime;
            releasedTime += dt;
            float decelerationTime = Math.Max(0f, releasedTime - Math.Max(0f, delay)) - Math.Max(0f, before - Math.Max(0f, delay));
            Speed = MoveTowards(Speed, Math.Min(baseSpeed, maxSpeed), Math.Max(0f, deceleration) * decelerationTime);
        }
        float extra = Math.Max(0f, Speed / Math.Max(1f, baseSpeed) - 1f);
        float fuel = Math.Max(0f, baseDrain) * (1f + extra * Math.Max(0f, fuelMultiplier)) * dt;
        if (held && armed && Speed >= maxSpeed)
        {
            armed = false;
            DashRemaining = Math.Max(0.01f, dashDuration);
            Speed = maxSpeed * Math.Max(1.01f, dashMultiplier);
        }
        return fuel;
    }
}
