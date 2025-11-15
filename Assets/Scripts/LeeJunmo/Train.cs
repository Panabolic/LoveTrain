
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

    [Tooltip("기차의 속도 최대치 값입니다.")]
    [SerializeField] private float maxSpeedValue = 460;

    // ✨ [추가] 사망 처리되는 속도 임계값
    [Tooltip("CurrentSpeed가 이 값 이하가 되면 사망 처리됩니다.")]
    [SerializeField] private float deathSpeedThreshold = 160f;

    public int Level = 1;

    // CurrentSpeed가 Train의 프로퍼티가 됨
    public float CurrentSpeed { get; private set; }

    [Header("디버그 정보")]
    [SerializeField] private float _currentSpeedForInspector;

    // ✨ [추가] UI가 최대 속도 값을 읽을 수 있도록 public getter를 추가합니다.
    public float MaxSpeedValue => maxSpeedValue;

    [Tooltip("사망 시 기차가 왼쪽으로 이동할 거리")]
    [SerializeField] private float deathMoveDistance = -30f;
    [Tooltip("사망 시 기차가 이동하는 속도")]   
    [SerializeField] private float deathMoveSpeed = 10f;
    [Tooltip("이동 시작 후 몇 초 뒤에 기차 오브젝트를 파괴할지")]
    [SerializeField] private float destroyDelay = 2.0f;

    private bool isDead = false;

    [SerializeField]
    private GameObject tempDieUI;

    /// <summary>
    /// 기차가 피격당했을 때 SpeedMeterUI에 알리기 위한 이벤트
    /// </summary>
    public event Action OnTrainDamaged;

    private void Awake()
    {
        carsAnim = GetComponentsInChildren<Animator>();
        //carColliders = GetComponentsInChildren<Collider2D>().ToList();
    }

    // 속도 초기화
    private void Start()
    {
        CurrentSpeed = maxSpeedValue;
        _currentSpeedForInspector = CurrentSpeed;
        isDead = false;
    }

    // 디버그용
    void Update()
    {
        if (isDead) return;
        _currentSpeedForInspector = CurrentSpeed;
    }

    public virtual void TakeDamage(float damageAmount/*, TrainCar trainCar*/)
    {
        if (isDead) return;

        OnTrainDamaged?.Invoke();

        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.ShakeCamera();
        }
/*
        switch (trainCar)
        {
            case TrainCar.front:
                break;

            case TrainCar.middle:
                break;

            case TrainCar.rear:
                break;
        }*/

        // 속도 감소 로직
        if (CurrentSpeed > deathSpeedThreshold) // ✨ 0 대신 임계값 사용
        {
            CurrentSpeed -= damageAmount;
            if (CurrentSpeed <= deathSpeedThreshold) // ✨ 0 대신 임계값 사용
            {
                CurrentSpeed = deathSpeedThreshold; // ✨ 정확히 임계값으로 설정 (선택적)
                Die(); // ✨ 사망 처리 함수 호출
            }
        }
        // ✨ 만약 시작부터 임계값 이하라면 바로 사망 처리 (선택적)
        else if (CurrentSpeed <= deathSpeedThreshold)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("기차 사망!");

        // ✨ [핵심 수정] 속도를 기반으로 이동 시간(duration) 계산
        float distance = Mathf.Abs(deathMoveDistance); // 이동할 거리 (절대값)
        float duration = 0f; // 이동 시간 초기화
        if (deathMoveSpeed > 0) // 속도가 0보다 클 때만 계산 (0으로 나누기 방지)
        {
            duration = distance / deathMoveSpeed; // 시간 = 거리 / 속력
        }
        else
        {
            Debug.LogWarning("deathMoveSpeed가 0 또는 음수입니다. 이동 시간이 0이 됩니다.", this);
        }

        // ✨ 계산된 duration 사용
        transform.DOMoveX(transform.position.x + deathMoveDistance, duration) // deathMoveDuration -> duration
            .SetEase(Ease.Linear) // 등속 이동
            .SetUpdate(true);

        // 3. 지정된 시간 후 오브젝트 파괴 코루틴 시작
        StartCoroutine(DestroyAfterDelay(destroyDelay));

        // (선택) 기차의 다른 컴포넌트 비활성화 (예: Collider2D)
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders) { col.enabled = false; }
        // TrainController 비활성화 등...
        TrainController controller = GetComponent<TrainController>();
        if (controller != null) controller.enabled = false;
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        tempDieUICall();
        // Time.timeScale에 영향을 받지 않도록 WaitForSecondsRealtime 사용
        yield return new WaitForSecondsRealtime(delay);
        Destroy(gameObject);
        SceneLoader.Instance.LoadStartScene();
    }

    public void IncreaseSpeedTest()
    {
        if (isDead) return;
        CurrentSpeed += 20;
    }

    /// <summary>
    /// 이벤트 시스템에서 기차의 현재 속도를 직접 조절합니다.
    /// </summary>
    public void ModifySpeed(float amount)
    {
        if (isDead) return; // (Train.cs에 isDead 변수가 있다고 가정)

        CurrentSpeed += amount;

        // 속도가 0(사망 임계값) 이하로 떨어졌는지 확인
        if (CurrentSpeed <= deathSpeedThreshold)
        {
            CurrentSpeed = deathSpeedThreshold;
            Die(); // (Train.cs의 Die 함수 호출)
        }
        // (선택) 최대 속도를 넘지 않게 제한
        else if (CurrentSpeed > CurrentSpeed)
        {
            CurrentSpeed = CurrentSpeed;
        }
    }

    public void DecreaseSpeedTest()
    {
        if (isDead) return;
        CurrentSpeed -= 20;

        Debug.Log($"테스트 감소! 현재 속도: {CurrentSpeed}, 사망 임계값: {deathSpeedThreshold}");

        if (CurrentSpeed <= deathSpeedThreshold)
        {
            CurrentSpeed = deathSpeedThreshold;
            Debug.Log("테스트 감소로 사망 조건 만족! Die() 함수 호출 시도.");
            Die();
        }
    }

    public void DieTest()
    {
        if (isDead) return;
        CurrentSpeed -= 500;

        Debug.Log($"테스트 감소! 현재 속도: {CurrentSpeed}, 사망 임계값: {deathSpeedThreshold}");

        if (CurrentSpeed <= deathSpeedThreshold)
        {
            CurrentSpeed = deathSpeedThreshold;
            Debug.Log("테스트 감소로 사망 조건 만족! Die() 함수 호출 시도.");
            Die();
        }
    }

    private void tempDieUICall()
    {
        tempDieUI.SetActive(true);
    }

    public float GetDeathSpeed()
    {
        return deathSpeedThreshold;
    }
}