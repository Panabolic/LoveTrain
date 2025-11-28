using DG.Tweening;
using System.Collections;
using UnityEngine;
using System;

public enum TrainCar
{
    front, middle, rear
}

public class Train : MonoBehaviour
{
    // Components
    public Animator[] carsAnim;
    private TrainController trainController; // 조작 제어용 참조

    [Tooltip("기차의 속도 최대치 값입니다.")]
    [SerializeField] private float maxSpeedValue = 460;

    // ✨ [사망 임계값] 이 값 이하가 되면 '위험 상태(Dying)' 진입
    [Tooltip("CurrentSpeed가 이 값 이하가 되면 조작 불능/사망 연출이 시작됩니다.")]
    [SerializeField] private float deathSpeedThreshold = 160f;

    public int Level = 1;

    // CurrentSpeed 프로퍼티
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

    [Tooltip("뒤로 밀려나는 속도 (이 속도로 이동하며, 이동이 끝날 때 속도도 0이 됨)")]
    [SerializeField] private float deathMoveSpeed = 5f;

    [Tooltip("완전 정지(속도 0) 후 파괴되기까지 대기 시간")]
    [SerializeField] private float destroyDelay = 1.0f;

    // --- 상태 변수 ---
    private bool isDead = false; // 완전히 사망했는지 (게임 오버)
    private bool isDying = false; // 현재 위험 상태(사망 연출 중)인지
    private bool inKnockback = false; // 넉백 애니메이션 중인지

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

        // 시작 시 조작 가능 상태 보장
        if (trainController != null) trainController.enabled = true;
    }

    void Update()
    {
        if (isDead) return;
        _currentSpeedForInspector = CurrentSpeed;

        // ✨ 핵심: 위험 상태(Dying)일 때의 로직 처리
        if (isDying && !inKnockback)
        {
            HandleDyingState();
        }
    }

    public virtual void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        OnTrainDamaged?.Invoke();

        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.ShakeCamera();
        }

        // 데미지 적용
        ModifySpeed(-damageAmount);
    }

    /// <summary>
    /// 속도를 변경하는 통합 함수 (데미지, 회복 모두 사용)
    /// </summary>
    public void ModifySpeed(float amount)
    {
        if (isDead) return;

        CurrentSpeed += amount;

        // 최대 속도 제한
        if (CurrentSpeed > maxSpeedValue) CurrentSpeed = maxSpeedValue;

        // 상태 체크 로직
        CheckState();
    }

    /// <summary>
    /// 현재 속도에 따라 '정상' vs '위험(Dying)' vs '사망(Dead)' 상태를 결정
    /// </summary>
    private void CheckState()
    {
        // 1. 회복 로직: 위험 상태였다가 속도가 임계값을 넘으면 복구
        if (isDying && CurrentSpeed > deathSpeedThreshold)
        {
            RecoverControl();
        }
        // 2. 위험 상태 진입: 속도가 임계값 이하이고, 아직 죽지는 않음
        else if (!isDying && CurrentSpeed <= deathSpeedThreshold && CurrentSpeed > 0)
        {
            StartDyingSequence();
        }
        // 3. 즉사: 데미지가 커서 한 방에 0 이하가 된 경우
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
        Debug.Log("위험 상태 진입! 조작 불능, 속도 감소 시작.");

        // 1. 기차 이동 조작 끄기
        if (trainController != null) trainController.enabled = false;

        // ✨ [추가] 맵에 있는 몬스터들 전부 소환 해제 (경험치 X)
        if (PoolManager.instance != null)
        {
            PoolManager.instance.DespawnAllEnemies();
        }

        // 2. 1차 넉백 연출 (DOTween)
        inKnockback = true;
        transform.DOMoveX(deathKnockbackPositionX, deathKnockbackDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => inKnockback = false);
    }
    /// <summary>
    /// 매 프레임 호출되어 뒤로 이동시키며 속도를 깎음
    /// </summary>
    private void HandleDyingState()
    {
        // 1. 멈추는 데 필요한 시간 계산 (거리 / 속력)
        // (남은 거리가 아니라 전체 설정값 기준 비율로 깎습니다)
        float moveDelta = deathMoveSpeed * Time.deltaTime;

        // 2. 뒤로 이동
        transform.position += new Vector3(-moveDelta, 0, 0); // 왼쪽(음수)으로 이동

        // 3. 속도 감소 (위치 이동과 동기화)
        // 공식: 감속량 = (임계값 / (총 이동거리 / 이동속도)) * delta
        // 즉, 총 이동거리를 다 가는 동안 속도도 정확히 0이 되게 함.
        float totalTime = Mathf.Abs(deathMoveDistance) / deathMoveSpeed;
        float speedDropRate = deathSpeedThreshold / totalTime;

        CurrentSpeed -= speedDropRate * Time.deltaTime;

        // 4. 완전 사망 체크
        if (CurrentSpeed <= 0)
        {
            CurrentSpeed = 0;
            Die(); // 게임 오버 처리
        }
    }

    // ========================================================================
    // ❤️ 회복 프로세스 (Recovery)
    // ========================================================================

    private void RecoverControl()
    {
        isDying = false;
        inKnockback = false;

        // 진행 중이던 넉백 트윈이 있다면 취소
        transform.DOKill();

        // 조작 다시 활성화!
        if (trainController != null) trainController.enabled = true;

        Debug.Log("속도 회복! 기차 통제권 복구됨.");
    }
        
    // ========================================================================
    // ☠️ 최종 사망 (Game Over)
    // ========================================================================

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        isDying = false; // 연출 루프 종료

        Debug.Log("기차 완전 정지. 사망 처리.");

        // 혹시 모를 잔여 트윈 제거
        transform.DOKill();

        // 파괴 코루틴 시작
        StartCoroutine(DestroyProcess());
    }

    private IEnumerator DestroyProcess()
    {
        tempDieUICall();
        // Time.timeScale 영향 안 받게 Realtime 사용
        yield return new WaitForSecondsRealtime(destroyDelay);

        Destroy(gameObject);
        SceneLoader.Instance.LoadStartScene();
    }

    // ========================================================================
    // 🧪 테스트용 버튼 연결
    // ========================================================================

    public void IncreaseSpeedTest()
    {
        if (isDead) return;
        ModifySpeed(20); // 회복 테스트용
    }

    public void DecreaseSpeedTest()
    {
        if (isDead) return;
        ModifySpeed(-20); // 데미지 테스트용
    }

    public void DieTest()
    {
        if (isDead) return;
        ModifySpeed(-500); // 즉사 테스트
    }

    private void tempDieUICall()
    {
        if (tempDieUI != null)
            tempDieUI.SetActive(true);
    }

    public float GetDeathSpeed()
    {
        return deathSpeedThreshold;
    }
}