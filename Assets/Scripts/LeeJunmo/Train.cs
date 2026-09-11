using DG.Tweening;
using System.Collections;
using UnityEngine;
using System;

public class Train : MonoBehaviour
{
    // Components
    public Animator[] carsAnim;
    private TrainController trainController;

    [Tooltip("기차의 속도 최대치 값입니다.")]
    [SerializeField] private float maxSpeedValue = 460;

    [Tooltip("CurrentSpeed가 이 값 이하가 되면 조작 불능/사망 연출이 시작됩니다.")]
    [SerializeField] private float deathSpeedThreshold = 160f;

    public float CurrentSpeed { get; private set; }

    [Header("디버그 정보")]
    [SerializeField] private float _currentSpeedForInspector;

    public float MaxSpeedValue => maxSpeedValue;

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
    private bool inKnockback = false;

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
        CurrentSpeed = maxSpeedValue;
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

        if (isDying && !inKnockback)
        {
            HandleDyingState();
        }

        // Playing이나 Boss 상태일 때만 바퀴가 굴러가게 함
        if (GameManager.Instance.CurrentState == GameState.Playing ||
            GameManager.Instance.CurrentState == GameState.Boss)
        {
            float currentSpeed = CurrentSpeed;
            int displaySpeed = Mathf.RoundToInt(currentSpeed);
            SetCarAnimSpeed(Mathf.Clamp(displaySpeed, 0, 300) * 0.05f);
        }
        else
        {
            SetCarAnimSpeed(0f);
        }
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
        if (isDead) return;
        OnTrainDamaged?.Invoke();
        if (CameraShakeManager.Instance != null) CameraShakeManager.Instance.ShakeCamera();
        SoundEventBus.Publish(SoundID.Player_Hit);
        if (isBossAttack) BossModifySpeed(-damageAmount);
        else ModifySpeed(-damageAmount);
    }

    public void ModifySpeed(float amount)
    {
        if (isDead) return;
        CurrentSpeed += amount;
        if (CurrentSpeed > maxSpeedValue) CurrentSpeed = maxSpeedValue;
        CheckState();
    }

    public void BossModifySpeed(float amount)
    {
        if (isDead) return;
        CurrentSpeed += amount;
        if (CurrentSpeed > maxSpeedValue) CurrentSpeed = maxSpeedValue;
        CheckState();
    }

    private void CheckState()
    {
        if (isDying)
        {
            if (CurrentSpeed > deathSpeedThreshold)
            {
                RecoverControl();
            }
            else if (CurrentSpeed <= 0)
            {
                CurrentSpeed = 0;
                Die();
            }

            return;
        }

        if (CurrentSpeed <= deathSpeedThreshold)
        {
            if (CurrentSpeed < deathSpeedThreshold)
            {
                CurrentSpeed = deathSpeedThreshold;
            }

            StartDyingSequence();
        }
    }

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

        // 7. ✨ [수정] 속도를 Max로 회복 (건강한 상태로 복구)
        CurrentSpeed = maxSpeedValue;
        _currentSpeedForInspector = CurrentSpeed; // 인스펙터 갱신용

        // 8. 컨트롤러 비활성화 (엔딩 중 조작 방지)
        if (trainController != null) trainController.enabled = false;

        Debug.Log("🚂 엔딩 진입: 기차 상태 강제 정상화 완료 (Max Speed 회복)");
    }

    // ... (StartDyingSequence, HandleDyingState 등 기존 로직 유지) ...

    private void StartDyingSequence()
    {
        if (isDead || isDying)
        {
            return;
        }

        isDying = true;
        if (trainController != null) trainController.enabled = false;

        if (PoolManager.instance != null)
        {
            Instantiate(explosionObject);
            SoundEventBus.Publish(SoundID.Player_Dying);
            PoolManager.instance.DespawnAllEnemiesExceptBoss();
        }

        if (Spawner.Instance != null)
        {
            Spawner.Instance.SetSpawning(true);
            Spawner.Instance.SetRearSpawning(false);
        }

        inKnockback = true;
        transform.DOMoveX(deathKnockbackPositionX, deathKnockbackDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => inKnockback = false);

        if (handObject != null && handTargetPos != null)
        {
            handObject.SetActive(true);
            handObject.transform.DOKill();
            handObject.transform.DOMove(handTargetPos.position, handMoveDuration)
                .SetEase(Ease.OutQuad);
        }

        SetOverlayTrigger("Dying");
    }

    private void HandleDyingState()
    {
        if (handTargetPos == null)
        {
            CurrentSpeed -= (deathSpeedThreshold / 3.0f) * Time.deltaTime;
            if (CurrentSpeed <= 0) Die();
            return;
        }

        float naturalDropRate = (deathSpeedThreshold / 5.0f);
        CurrentSpeed -= naturalDropRate * Time.deltaTime;

        if (CurrentSpeed > deathSpeedThreshold) CurrentSpeed = deathSpeedThreshold;

        float ratio = Mathf.Clamp01(CurrentSpeed / deathSpeedThreshold);
        float targetX = Mathf.Lerp(handTargetPos.position.x, deathKnockbackPositionX, ratio);

        Vector3 newPos = transform.position;
        newPos.x = Mathf.Lerp(transform.position.x, targetX, Time.deltaTime * 5f);
        transform.position = newPos;

        if (CurrentSpeed <= 0)
        {
            CurrentSpeed = 0;
            Die();
        }
    }

    private void RecoverControl()
    {
        isDying = false;
        ResetDyingPresentation(true);
        if (trainController != null) trainController.enabled = true;

        if (Spawner.Instance != null) Spawner.Instance.SetRearSpawning(true);
    }

    private void ResetDyingPresentation(bool playEscapeTrigger)
    {
        inKnockback = false;

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
        if (GameManager.Instance.CurrentState == GameState.Ending) return;

        if (isDead) return;
        isDead = true;
        isDying = false;

        if (handObject != null) handObject.transform.DOKill();

        Debug.Log("기차 완전 정지. 사망 처리.");
        transform.DOKill();
        GameManager.Instance.PlayerDied();
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
    public void DieTest() { if (!isDead) ModifySpeed(-500); }
    public void IncreaseMaxSpeed(float amount) { maxSpeedValue += amount; }
    public void HealPercent(float percentage) { if (!isDead) ModifySpeed(maxSpeedValue * percentage); }

    private void tempDieUICall() { if (tempDieUI != null) tempDieUI.SetActive(true); }
    public float GetDeathSpeed() { return deathSpeedThreshold; }
    public bool IsDead { get { return isDead; } }
    public bool IsDying { get { return isDying; } }
}
