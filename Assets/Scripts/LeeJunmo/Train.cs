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
    [Tooltip("사망 시 폭발 이펙트")]
    [SerializeField] private GameObject explosionObject;
    [Tooltip("손이 이동할 목표 위치 (로컬 혹은 월드)")]
    [SerializeField] private Transform handTargetPos;
    [Tooltip("손 이동 시간")]
    [SerializeField] private float handMoveDuration = 2.0f;
    [Header("사망 후 끌려가는 연출 설정")]
    [Tooltip("손이 기차를 잡고 이동할 목표 위치 (월드 기준)")]
    [SerializeField] private Vector3 dragDestinationPos = new Vector3(-30f, 0, 0);
    [Tooltip("끌려가는 데 걸리는 시간")]
    [SerializeField] private float dragDuration = 3.0f;

    [Header("오버레이 연출")]
    [Tooltip("테두리 오버레이 애니메이터")]
    [SerializeField] private Animator overlayBorderAnim;
    [Tooltip("이펙트 오버레이 애니메이터")]
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

        // 손 오브젝트 초기화
        if (handObject != null)
        {
            // 원래 위치(화면 밖 등)를 기억해둠
            handInitialPos = handObject.transform.position;
            handObject.SetActive(false);
        }

        // 시작 시 애니메이션 멈춤 상태로 초기화
        SetCarAnimSpeed(0f);
    }

    void Update()
    {
        if (isDead) return;
        _currentSpeedForInspector = CurrentSpeed;

        // 사망 연출 중일 때 속도 감소 및 위치 이동 처리
        if (isDying && !inKnockback)
        {
            HandleDyingState();
        }

        // ✨ [핵심 수정 1] Playing이나 Boss 상태일 때만 바퀴가 굴러가게 함
        // (Start, Event, Die 상태에서는 멈춤)
        if (GameManager.Instance.CurrentState == GameState.Playing ||
            GameManager.Instance.CurrentState == GameState.Boss)
        {
            float currentSpeed = CurrentSpeed;
            int displaySpeed = Mathf.RoundToInt(currentSpeed);

            // 속도에 비례해 애니메이션 속도 조절
            SetCarAnimSpeed(Mathf.Clamp(displaySpeed, 0, 300) * 0.05f);
        }
        else
        {
            // 그 외 상태(일시정지 등)에서는 바퀴 정지
            SetCarAnimSpeed(0f);
        }
    }

    // 애니메이션 속도 조절 헬퍼 함수
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
        if(isBossAttack)
        {
            BossModifySpeed(-damageAmount);
        }
        else
        {
            ModifySpeed(-damageAmount);
        }
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

        if (isDying && CurrentSpeed > deathSpeedThreshold)
        {
            RecoverControl();
        }
        else if (!isDying && CurrentSpeed <= deathSpeedThreshold)
        {
            StartDyingSequence();
        }
        else if(CurrentSpeed <= 0)
        {
            CurrentSpeed = 0;
            StartCoroutine(DestroyProcess());
        }
    }


    private void CheckState()
    {
        // 이미 죽어가고 있는데 속도가 회복된 경우 -> 회복
        if (isDying && CurrentSpeed > deathSpeedThreshold)
        {
            RecoverControl();
        }
        // 살아있는데 속도가 임계치 이하로 떨어진 경우 -> 사망 연출 시작
        else if (!isDying && CurrentSpeed <= deathSpeedThreshold || CurrentSpeed < 0)
        {
            // 속도가 한 번에 0 이하로 떨어지는 것 방지 (연출을 위해)
            if (CurrentSpeed < deathSpeedThreshold) CurrentSpeed = deathSpeedThreshold;
            StartDyingSequence();
        }
    }

    // ========================================================================
    // 💀 사망 연출 프로세스 (Dying Sequence)
    // ========================================================================

    private void StartDyingSequence()
    {
        isDying = true;
        Debug.Log("위험 상태 진입! 연출 시작.");

        // 1. 기차 조작 끄기
        if (trainController != null) trainController.enabled = false;

        // 2. ✨ [핵심 수정 2] 몬스터 정리 및 '후방 스폰'만 차단
        if (PoolManager.instance != null)
        {
            Instantiate(explosionObject);
            SoundEventBus.Publish(SoundID.Player_Dying);
            PoolManager.instance.DespawnAllEnemiesExceptBoss(); // 화면상의 적 정리
        }

        if (Spawner.Instance != null)
        {
            Spawner.Instance.SetSpawning(true);      // 전체 스폰은 켜둠 (앞에서는 나와야 함)
            Spawner.Instance.SetRearSpawning(false); // 뒤쪽(손 나오는 곳) 스폰만 끔
        }

        // 3. 넉백 연출
        inKnockback = true;
        transform.DOMoveX(deathKnockbackPositionX, deathKnockbackDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => inKnockback = false);

        // 4. 손 오브젝트 활성화 및 이동
        if (handObject != null && handTargetPos != null)
        {
            handObject.SetActive(true);
            handObject.transform.DOKill(); // 혹시 돌아가던 중이면 취소

            // 목표 위치로 서서히 이동
            handObject.transform.DOMove(handTargetPos.position, handMoveDuration)
                .SetEase(Ease.OutQuad);
        }

        // 5. 오버레이 연출 (Dying 트리거)
        SetOverlayTrigger("Dying");
    }

    private void HandleDyingState()
    {
        if (handTargetPos == null)
        {
            // 예외 처리: 타겟이 없으면 기존 방식대로 감속
            CurrentSpeed -= (deathSpeedThreshold / 3.0f) * Time.deltaTime;
            if (CurrentSpeed <= 0) Die();
            return;
        }

        // 1. 시간 경과에 따른 자연스러운 속도 감소 (저항력 느낌)
        // (플레이어가 아무것도 안 하면 이 속도로 줄어들어 결국 사망)
        float naturalDropRate = (deathSpeedThreshold / 5.0f); // 예: 5초 뒤 사망
        CurrentSpeed -= naturalDropRate * Time.deltaTime;

        // 2. 속도 범위 제한 (0 ~ 임계치)
        if (CurrentSpeed > deathSpeedThreshold) CurrentSpeed = deathSpeedThreshold;
        // (회복되면 CheckState에서 RecoverControl이 호출되므로 여기선 상한선만 둠)

        // 3. ✨ [핵심 수정] 현재 속도에 따른 위치 계산 (Lerp)
        // ratio 1.0 = 속도 최대 (임계치) -> 시작 위치 (deathKnockbackPositionX)
        // ratio 0.0 = 속도 0 (정지) -> 목표 위치 (handTargetPos)

        float ratio = Mathf.Clamp01(CurrentSpeed / deathSpeedThreshold);

        // 시작점(넉백 후 위치)과 끝점(손 위치) 사이를 속도 비율로 보간
        // 속도가 높을수록 시작점에, 낮을수록 손 위치에 가까워짐
        float targetX = Mathf.Lerp(handTargetPos.position.x, deathKnockbackPositionX, ratio);

        // 부드러운 이동을 위해 MoveTowards나 Lerp 사용
        // (프레임마다 목표 위치가 바뀌므로 부드럽게 따라가도록 설정)
        Vector3 newPos = transform.position;
        newPos.x = Mathf.Lerp(transform.position.x, targetX, Time.deltaTime * 5f); // 5f는 따라가는 반응 속도
        transform.position = newPos;

        // 4. 사망 체크
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

        // ✨ [핵심 수정 3] 후방 스폰 다시 활성화
        if (Spawner.Instance != null)
        {
            Spawner.Instance.SetRearSpawning(true);
        }

        // 손 오브젝트 복귀 연출
        if (handObject != null)
        {
            handObject.transform.DOKill(); // 다가오던 움직임 중단

            // 원래 위치(InitialPos)로 되돌아감
            handObject.transform.DOMove(handInitialPos, handMoveDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
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
        StartCoroutine(DragAndDestroySequence());
    }

    private IEnumerator DragAndDestroySequence()
    {
        // 1. 손 오브젝트 확인
        if (handObject != null)
        {
            // 2. 기차를 손의 자식으로 설정 (이제 손 움직임에 따라 기차가 같이 움직임)
            // worldPositionStays = true로 설정하여 현재 위치 유지한 채 부모만 변경
            transform.SetParent(handObject.transform, true);

            // 3. (선택) 손과 기차의 정렬을 맞추고 싶다면 여기서 미세 조정
            // 예: 기차의 Y축 위치를 손의 특정 지점에 맞춤
            // transform.DOLocalMoveY(0f, 0.5f); 
        }

        // 4. UI 호출 (게임오버 UI 등을 미리 띄움)
        tempDieUICall();

        SoundEventBus.Publish(SoundID.Player_GameOver);
        // 5. 손이 기차를 끌고 화면 밖으로 이동
        if (handObject != null)
        {
            // 목표 지점이 설정되어 있다면 그곳으로, 없다면 현재 위치에서 X축으로 -20만큼 이동
            Vector3 targetPos = (dragDestinationPos != null) ? dragDestinationPos :
                                handObject.transform.position + new Vector3(-30f, 0f, 0f);

            // Y축은 유지하고 싶다면:
            targetPos.y = handObject.transform.position.y; 

            // DOTween으로 이동
            yield return handObject.transform
                .DOMove(targetPos, dragDuration)
                .SetEase(Ease.InQuad) // 점점 빨라지며 끌려가는 느낌
                .SetUpdate(true)      // TimeScale이 0이어도 움직이도록 (필요시)
                .WaitForCompletion();
        }
        else
        {
            // 손 오브젝트가 없다면 그냥 대기
            yield return new WaitForSecondsRealtime(destroyDelay);
        }

        // 6. 이동 완료 후 파괴 및 씬 전환
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

    // --- Test & Helper Methods ---
    public void IncreaseSpeedTest() { if (!isDead) ModifySpeed(20); }
    public void DecreaseSpeedTest() { if (!isDead) ModifySpeed(-20); }
    public void DieTest() { if (!isDead) ModifySpeed(-500); }

    public void IncreaseMaxSpeed(float amount) { maxSpeedValue += amount; }
    public void HealPercent(float percentage) { if (!isDead) ModifySpeed(maxSpeedValue * percentage); }

    private void tempDieUICall() { if (tempDieUI != null) tempDieUI.SetActive(true); }
    public float GetDeathSpeed() { return deathSpeedThreshold; }

    public bool IsDead { get { return isDead; } }
}