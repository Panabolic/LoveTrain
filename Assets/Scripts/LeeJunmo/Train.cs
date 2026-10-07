using DG.Tweening;
using System.Collections;
using UnityEngine;
using System;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Train : MonoBehaviour
{
    // Components
    public Animator[] carsAnim;
    private TrainController trainController;
    private Animator[] carAnimationBinding;
    private PresentationLink carPresentation;

    [Tooltip("기차의 속도 최대치 값입니다.")]
    [SerializeField] private float maxSpeedValue = 460;

    [Tooltip("기존 계기판의 저속 구간 경계입니다. 연료 체력과 사망에는 관여하지 않습니다.")]
    [SerializeField] private float deathSpeedThreshold = 160f;

    public float CurrentSpeed { get => drive.Speed; private set => drive.Speed = value; }

    [Header("Fuel and acceleration")]
    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private float baseSpeed = 320f;
    [SerializeField] private float acceleration = 70f;
    [SerializeField] private float dashSpeedMultiplier = 1.5f;
    [SerializeField] private float dashDuration = 5f;
    [Tooltip("질주 전체 시간 동안 소모하는 총 연료입니다.")]
    [SerializeField] private float dashFuelCost = 30f;
    private readonly FuelState fuel = new FuelState();
    public float CurrentFuel { get => fuel.Current; private set => fuel.Reset(value); }
    public float MaxFuel => maxFuel;
    public bool IsDashing => drive.DashRemaining > 0f;
    public float FuelDrainPerSecond => IsDashing ? Mathf.Max(0f, dashFuelCost) / Mathf.Max(0.01f, dashDuration) : TrainDriveState.GetFuelDrainPerSecond(CurrentSpeed);
    private readonly TrainDriveState drive = new TrainDriveState();
    [SerializeField] private float decelerationDelay = 0.5f;
    [SerializeField] private float deceleration = 70f;
    private readonly HashSet<UnityEngine.Object> bossContacts = new HashSet<UnityEngine.Object>();
    private float runStartBaseSpeed;
    private float runStartMaxSpeed;
    private int runLevel = 1;
    private bool speedBaselineCaptured;
    private bool runInitialized;
    private PursuingHand pursuingHand;
    public bool IsAccelerating => drive.IsAccelerating;
    public bool AccelerationBlocked => drive.AccelerationBlockRemaining > 0f || bossContacts.Count > 0;

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
    private readonly DeathLifecycle death = new DeathLifecycle();
    private bool isDead { get => !death.IsAlive; set => death.IsAlive = !value; }
    private bool isDying { get => death.PresentationActive; set => death.PresentationActive = value; }
    private ObjectHost runtimeHost;

    // 손의 원래 위치 저장용 변수
    private Vector3 handInitialPos;

    [SerializeField]
    private GameObject tempDieUI;

    public event Action OnTrainDamaged;

    private void Awake()
    {
        carsAnim = GetComponentsInChildren<Animator>();
        trainController = GetComponent<TrainController>();
        runtimeHost = new ObjectHost(gameObject);
        runtimeHost.BindUpdate(AdvanceTrain);
        CaptureSpeedBaseline();
    }

    private void Start()
    {
        // Snapshot permanent upgrades before initializing this run's fuel and driving state.
        maxFuel = Mathf.Max(1f, maxFuel + PermanentUpgradeProgress.FuelCapacityBonus);
        float upgradedDashDuration = PermanentUpgradeProgress.DashDurationOverride;
        float upgradedDashMultiplier = PermanentUpgradeProgress.DashSpeedMultiplierOverride;
        if (upgradedDashDuration > 0f) dashDuration = upgradedDashDuration;
        if (upgradedDashMultiplier > 0f) dashSpeedMultiplier = upgradedDashMultiplier;
        CurrentSpeed = Mathf.Min(baseSpeed, maxSpeedValue);
        CurrentFuel = maxFuel;
        drive.Reset();
        relativeWorldSpeed = 0f;
        _currentSpeedForInspector = CurrentSpeed;
        isDead = false;
        isDying = false;
        runInitialized = true;
        bossContacts.Clear();

        if (trainController != null) trainController.enabled = true;

        if (handObject != null)
        {
            handInitialPos = handObject.transform.position;
            pursuingHand = handObject.GetComponent<PursuingHand>();
            handObject.SetActive(pursuingHand != null);
            if (pursuingHand != null) pursuingHand.InitializeRun(this, runStartBaseSpeed);
        }

        SetCarAnimSpeed(0f);
    }

    void Update()
    {
        runtimeHost.Update(Time.deltaTime);
    }

    private void OnEnable() { runtimeHost?.Activate(); }
    private void OnDisable() { runtimeHost?.Deactivate(); }

    private void AdvanceTrain(float deltaTime)
    {
        if (isDead) return;
        _currentSpeedForInspector = CurrentSpeed;

        bool combat = GameManager.Instance != null &&
            (GameManager.Instance.CurrentState == GameState.Playing || GameManager.Instance.CurrentState == GameState.Boss);
        if (!combat || Time.timeScale <= 0f) { SetCarAnimSpeed(0f); return; }
        bossContacts.RemoveWhere(IsInactiveContact);
        bool shift = Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        float fuelUsed = drive.Advance(deltaTime, shift, baseSpeed, maxSpeedValue, acceleration,
            deceleration, decelerationDelay, dashDuration, dashSpeedMultiplier,
            dashFuelCost, bossContacts.Count > 0);
        relativeWorldSpeed = Mathf.MoveTowards(relativeWorldSpeed,
            Mathf.Max(0f, CurrentSpeed - baseSpeed) * Mathf.Max(0f, relativeSpeedScale),
            Mathf.Max(0.01f, relativeSpeedChangeRate) * deltaTime);
        ModifyFuel(-fuelUsed);
        SetCarAnimSpeed(CurrentSpeed * 0.05f);
    }

    public void ModifyFuel(float amount)
    {
        if (isDead) return;
        if (fuel.Modify(amount, maxFuel)) { drive.Reset(); relativeWorldSpeed = 0f; CurrentSpeed = 0f; Die(); }
    }

    private void SetCarAnimSpeed(float speed)
    {
        // carsAnim stays public; replacing its array rebinds presentation at this boundary.
        if (carPresentation == null || !ReferenceEquals(carAnimationBinding, carsAnim))
        {
            carAnimationBinding = carsAnim;
            carPresentation = new PresentationLink(this, null, carsAnim);
        }
        carPresentation.SetFloat("moveSpeed", speed);
    }

    public virtual void TakeDamage(float damageAmount, bool isBossAttack = false)
    {
        if (GameManager.Instance != null && (Time.timeScale <= 0f ||
            (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Boss))) return;
        if (isDead || IsDashing || damageAmount <= 0f) return;
        drive.BlockAcceleration(0.2f);
        OnTrainDamaged?.Invoke();
        if (CameraShakeManager.Instance != null) CameraShakeManager.Instance.ShakeCamera();
        SoundEventBus.Publish(SoundID.Player_Hit);
        ModifyFuel(-damageAmount);
    }

    public void TakeCollisionDamage(float fuelDamage, float speedLoss)
    {
        if (GameManager.Instance != null && (Time.timeScale <= 0f ||
            (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Boss))) return;
        if (isDead || IsDashing) return;
        TakeDamage(fuelDamage);
        if (!isDead) ModifySpeed(-Mathf.Max(0f, speedLoss));
    }

    public void ModifySpeed(float amount)
    {
        if (isDead || IsDashing) return;
        CurrentSpeed = Mathf.Clamp(CurrentSpeed + amount, 0f, maxSpeedValue);
    }

    public void BossModifySpeed(float amount) => ModifySpeed(amount);

    private void CaptureSpeedBaseline()
    {
        if (speedBaselineCaptured) return;
        runStartBaseSpeed = baseSpeed;
        runStartMaxSpeed = maxSpeedValue;
        speedBaselineCaptured = true;
    }

    public void SetRunLevel(int level)
    {
        CaptureSpeedBaseline();
        float previousBase = baseSpeed;
        runLevel = Mathf.Max(1, level);
        float bonus = (runLevel - 1) * 20f;
        baseSpeed = runStartBaseSpeed + bonus;
        maxSpeedValue = runStartMaxSpeed + bonus;
        if (runInitialized && !isDead)
            CurrentSpeed = IsDashing ? maxSpeedValue * dashSpeedMultiplier : Mathf.Clamp(CurrentSpeed + baseSpeed - previousBase, 0f, maxSpeedValue);
    }

    public void SetBossContact(UnityEngine.Object source, bool active)
    {
        if (source == null) return;
        if (active && !isDead) bossContacts.Add(source);
        else bossContacts.Remove(source);
    }

    private static bool IsInactiveContact(UnityEngine.Object source)
    {
        if (source == null) return true;
        var component = source as Component;
        return component != null && !component.gameObject.activeInHierarchy;
    }

    public void BeginPursuingHandDeath()
    {
        if (!isDead) Die();
    }

    // ========================================================================
    // ☠️ [핵심] 엔딩 진입 시 모든 상태 강제 초기화 함수
    // ========================================================================
    public void ForceStopDyingState()
    {
        // 1. 진행 중인 모든 코루틴 강제 중단
        StopAllCoroutines();
        if (pursuingHand != null) pursuingHand.StopPursuit();
        bossContacts.Clear();

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

        if (!death.TryBegin()) return;
        isDying = false;
        bossContacts.Clear();
        drive.CancelDash();
        if (pursuingHand != null) pursuingHand.StopPursuit();
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
            Vector3 targetPos = dragDestinationPos + Vector3.right *
                (trainController != null ? trainController.CurrentCameraOffsetX : 0f);
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
    public float AccelerationBlockRemaining { get; private set; }
    public bool IsAccelerating { get; private set; }
    private float releasedTime;
    private bool armed = true;
    public void Reset() { DashRemaining = 0f; releasedTime = 0f; armed = true; AccelerationBlockRemaining = 0f; IsAccelerating = false; }
    public void BlockAcceleration(float duration) { AccelerationBlockRemaining = Math.Max(AccelerationBlockRemaining, Math.Max(0f, duration)); }
    public void CancelDash() { DashRemaining = 0f; IsAccelerating = false; }

    private static float MoveTowards(float value, float target, float delta)
    {
        return Math.Abs(target - value) <= delta ? target : value + Math.Sign(target - value) * delta;
    }

    public static float GetFuelDrainPerSecond(float speed)
    {
        if (speed < 355f) return 0.25f;
        if (speed < 390f) return 0.4f;
        if (speed < 425f) return 0.5f;
        if (speed < 460f) return 0.6f;
        return 0.75f;
    }

    public float Advance(float dt, bool held, float baseSpeed, float maxSpeed, float acceleration,
        float deceleration, float delay, float dashDuration, float dashMultiplier,
        float dashFuelCost, bool bossContact = false)
    {
        IsAccelerating = false;
        if (dt <= 0f) return 0f;
        if (!held) armed = true;
        if (held) releasedTime = 0f;
        if (bossContact)
        {
            // Boss contact interrupts even a dash; no fuel hit is attached to contact.
            CancelDash();
            AccelerationBlockRemaining = Math.Max(0f, AccelerationBlockRemaining - dt);
            Speed = MoveTowards(Speed, 0f, 70f * dt);
            return GetFuelDrainPerSecond(Speed) * dt;
        }
        float blockedTime = Math.Min(dt, AccelerationBlockRemaining);
        AccelerationBlockRemaining = Math.Max(0f, AccelerationBlockRemaining - dt);
        float fuel = 0f;
        if (blockedTime > 0f)
        {
            if (!held) ApplyDeceleration(blockedTime, baseSpeed, maxSpeed, deceleration, delay);
            fuel += GetFuelDrainPerSecond(Speed) * blockedTime;
            dt -= blockedTime;
            if (dt <= 0f) return fuel;
        }
        if (DashRemaining > 0f)
        {
            float usedTime = Math.Min(dt, DashRemaining);
            DashRemaining = Math.Max(0f, DashRemaining - usedTime);
            Speed = maxSpeed * Math.Max(1.01f, dashMultiplier);
            if (DashRemaining <= 0f) { Speed = maxSpeed; releasedTime = 0f; }
            fuel += Math.Max(0f, dashFuelCost) / Math.Max(0.01f, dashDuration) * usedTime;
            dt -= usedTime;
            if (dt <= 0f) return fuel;
        }
        if (held)
        {
            IsAccelerating = Speed < maxSpeed;
            Speed = MoveTowards(Speed, maxSpeed, Math.Max(0f, acceleration) * dt);
        }
        else
        {
            ApplyDeceleration(dt, baseSpeed, maxSpeed, deceleration, delay);
        }
        fuel += GetFuelDrainPerSecond(Speed) * dt;
        if (held && armed && Speed >= maxSpeed)
        {
            armed = false;
            DashRemaining = Math.Max(0.01f, dashDuration);
            Speed = maxSpeed * Math.Max(1.01f, dashMultiplier);
        }
        return fuel;
    }

    private void ApplyDeceleration(float dt, float baseSpeed, float maxSpeed, float deceleration, float delay)
    {
        float before = releasedTime;
        releasedTime += dt;
        float decelerationTime = Math.Max(0f, releasedTime - Math.Max(0f, delay)) - Math.Max(0f, before - Math.Max(0f, delay));
        Speed = MoveTowards(Speed, Math.Min(baseSpeed, maxSpeed), Math.Max(0f, deceleration) * decelerationTime);
    }
}
