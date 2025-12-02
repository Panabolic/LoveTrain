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

    public int Level = 1;
    public float CurrentSpeed { get; private set; }

    [Header("디버그 정보")]
    [SerializeField] private float _currentSpeedForInspector;

    public float MaxSpeedValue => maxSpeedValue;

    [Header("사망 연출 설정")]
    [Tooltip("위험 상태 진입 시 1차로 튕겨나갈 위치 X")]
    [SerializeField] private float deathKnockbackPositionX = 0f;
    [Tooltip("1차 튕겨나감(Knockback)에 걸리는 시간")]
    [SerializeField] private float deathKnockbackDuration = 0.5f;
    [Tooltip("이후 서서히 멈출 때 뒤로 이동할 총 거리 (음수)")]
    [SerializeField] private float deathMoveDistance = -15f;
    [Tooltip("뒤로 밀려나는 속도")]
    [SerializeField] private float deathMoveSpeed = 5f;
    [Tooltip("완전 정지(속도 0) 후 파괴되기까지 대기 시간")]
    [SerializeField] private float destroyDelay = 1.0f;

    [Header("사망 연출 오브젝트")]
    [Tooltip("사망 시 나타날 손 오브젝트")]
    [SerializeField] private GameObject handObject;
    [Tooltip("손이 이동할 목표 위치 (로컬 혹은 월드)")]
    [SerializeField] private Transform handTargetPos;
    [Tooltip("손 이동 시간")]
    [SerializeField] private float handMoveDuration = 2.0f;

    [Header("오버레이 연출")]
    [Tooltip("테두리 오버레이 애니메이터")]
    [SerializeField] private Animator overlayBorderAnim;
    [Tooltip("이펙트 오버레이 애니메이터")]
    [SerializeField] private Animator overlayEffectAnim;

    // --- 상태 변수 ---
    private bool isDead = false;
    private bool isDying = false;
    private bool inKnockback = false;

    // ✨ [추가] 손의 원래 위치 저장용 변수
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

        // ✨ [수정] 손 오브젝트 초기화
        if (handObject != null)
        {
            // 원래 위치(화면 밖 등)를 기억해둠
            handInitialPos = handObject.transform.position;
            handObject.SetActive(false);
        }
    }

    void Update()
    {
        if (isDead) return;
        _currentSpeedForInspector = CurrentSpeed;

        if (isDying && !inKnockback)
        {
            HandleDyingState();
        }
    }

    public virtual void TakeDamage(float damageAmount)
    {
        if (isDead) return;
        OnTrainDamaged?.Invoke();
        if (CameraShakeManager.Instance != null) CameraShakeManager.Instance.ShakeCamera();
        ModifySpeed(-damageAmount);
    }

    public void ModifySpeed(float amount)
    {
        if (isDead) return;
        CurrentSpeed += amount;
        if (CurrentSpeed > maxSpeedValue) CurrentSpeed = maxSpeedValue;
        CheckState();
    }

    private void CheckState()
    {
        if (isDying && CurrentSpeed > deathSpeedThreshold)
        {
            RecoverControl();
        }
        else if (!isDying && CurrentSpeed <= deathSpeedThreshold && CurrentSpeed > 0)
        {
            StartDyingSequence();
        }
        else if (CurrentSpeed <= 0)
        {
            CurrentSpeed = 0;
            Die();
        }
    }

    // ========================================================================
    // 💀 사망 연출 프로세스 (Dying Sequence)
    // ========================================================================

    private void StartDyingSequence()
    {
        isDying = true;
        Debug.Log("위험 상태 진입! 연출 시작.");

        // 1. 기차 조작 끄기 & 몬스터 제거
        if (trainController != null) trainController.enabled = false;
        if (PoolManager.instance != null) PoolManager.instance.DespawnAllEnemies();

        // 2. 넉백 연출
        inKnockback = true;
        transform.DOMoveX(deathKnockbackPositionX, deathKnockbackDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => inKnockback = false);

        // 3. 손 오브젝트 활성화 및 이동
        if (handObject != null && handTargetPos != null)
        {
            handObject.SetActive(true);
            handObject.transform.DOKill(); // 혹시 돌아가던 중이면 취소

            // 목표 위치로 서서히 이동
            handObject.transform.DOMove(handTargetPos.position, handMoveDuration)
                .SetEase(Ease.OutQuad);
        }

        // 4. 오버레이 연출 (Dying 트리거)
        SetOverlayTrigger("Dying");
    }

    private void HandleDyingState()
    {
        float moveDelta = deathMoveSpeed * Time.deltaTime;
        transform.position += new Vector3(-moveDelta, 0, 0);

        float totalTime = Mathf.Abs(deathMoveDistance) / deathMoveSpeed;
        float speedDropRate = deathSpeedThreshold / totalTime;

        CurrentSpeed -= speedDropRate * Time.deltaTime;

        if (CurrentSpeed <= 0)
        {
            CurrentSpeed = 0;
            Die();
        }
    }

    // ========================================================================
    // ❤️ 회복 프로세스 (Recovery)
    // ========================================================================

    private void RecoverControl()
    {
        isDying = false;
        inKnockback = false;

        transform.DOKill();
        if (trainController != null) trainController.enabled = true;

        // ✨ [수정] 손 오브젝트 복귀 연출
        if (handObject != null)
        {
            handObject.transform.DOKill(); // 다가오던 움직임 중단

            // 원래 위치(InitialPos)로 되돌아감
            handObject.transform.DOMove(handInitialPos, handMoveDuration)
                .SetEase(Ease.InQuad) // 돌아갈 땐 InQuad가 자연스러움 (혹은 OutQuad 취향껏)
                .OnComplete(() =>
                {
                    // 도착하면 비활성화
                    handObject.SetActive(false);
                });
        }

        // 오버레이 연출 해제
        SetOverlayTrigger("Escape");

        Debug.Log("속도 회복! 기차 통제권 복구됨.");
    }

    // 오버레이 제어 헬퍼 함수
    private void SetOverlayTrigger(string triggerName)
    {
        if (overlayBorderAnim != null) overlayBorderAnim.SetTrigger(triggerName);
        if (overlayEffectAnim != null) overlayEffectAnim.SetTrigger(triggerName);
    }

    // ========================================================================
    // ☠️ 최종 사망 (Game Over)
    // ========================================================================

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        isDying = false;

        if (handObject != null) handObject.transform.DOKill();

        Debug.Log("기차 완전 정지. 사망 처리.");
        transform.DOKill();
        GameManager.Instance.PlayerDied();
        StartCoroutine(DestroyProcess());
    }

    private IEnumerator DestroyProcess()
    {
        tempDieUICall();
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
}