using UnityEngine;
using UnityEngine.InputSystem;

// 데이터 전달용 구조체
[System.Serializable]
public struct GunStats
{
    public float damage;
    public float speed;
    public float fireRate;
    public GameObject projectilePrefab;
    public GameObject laserPrefab;
}

public class Gun : MonoBehaviour
{
    [Header("입력")]
    public InputActionReference fireAction;

    [Header("현재 스탯")]
    public GunStats CurrentStats;

    // ✨ [추가] 게임 시작 시점의 기본 스탯 (백업용)
    private GunStats _baseStats;

    // ✨ [추가] 스탯 배율 (0 = 0%, 0.1 = 10% 증가)
    private float _damageMultiplier = 0f;
    private float _fireRateMultiplier = 0f;

    // 현재 장착된 전략
    private IWeaponStrategy currentStrategy;
    public Transform firePoint;

    private void Awake()
    {
        // 게임 시작 시 초기 설정된 값을 기본값으로 저장합니다.
        _baseStats = CurrentStats;
    }

    private void OnEnable()
    {
        if (fireAction != null) fireAction.action.Enable();
    }
    private void OnDisable()
    {
        if (fireAction != null) fireAction.action.Disable();
    }

    void Start()
    {
        SetWeapon(new ProjectileStrategy());
    }

    public void SetWeapon(IWeaponStrategy newStrategy)
    {
        if (currentStrategy != null) currentStrategy.Unequip();

        currentStrategy = newStrategy;
        // 무기 교체 시에도 현재 계산된 스탯을 적용합니다.
        currentStrategy.Initialize(this, CurrentStats);
    }

    // ✨ [추가] 외부(아이템)에서 공격력 배율을 더하는 함수
    public void AddDamageMultiplier(float amount)
    {
        _damageMultiplier += amount;
        UpdateStats();
    }

    // ✨ [추가] 외부(아이템)에서 공격속도 배율을 더하는 함수
    public void AddFireRateMultiplier(float amount)
    {
        _fireRateMultiplier += amount;
        UpdateStats();
    }

    // ✨ [추가] 최종 스탯 재계산 함수
    // ✨ [수정됨] 최종 스탯 재계산 함수
    private void UpdateStats()
    {
        // 공격력: 높을수록 좋음 -> 곱하기 (*)
        CurrentStats.damage = _baseStats.damage * (1f + _damageMultiplier);

        // 공격 속도(발사 간격): 낮을수록(짧을수록) 좋음 -> 나누기 (/)
        // 예: 공격속도 +100% (배율 1.0) -> 원래 간격 / (1 + 1) = 절반으로 줄어듬 (2배 빨라짐)
        if (1f + _fireRateMultiplier > 0) // 0으로 나누기 방지
        {
            CurrentStats.fireRate = _baseStats.fireRate / (1f + _fireRateMultiplier);
        }

        // 현재 무기에 변경된 스탯 즉시 적용
        if (currentStrategy != null)
        {
            currentStrategy.Initialize(this, CurrentStats);
        }

        Debug.Log($"[Gun] 스탯 갱신됨: 공격력 {CurrentStats.damage}, 발사간격 {CurrentStats.fireRate}");
    }

    void Update()
    {
        if (Time.timeScale == 0) return;

        bool isTriggerHeld = fireAction != null && fireAction.action.IsPressed();

        if (currentStrategy != null)
        {
            currentStrategy.Process(isTriggerHeld);
        }
    }
}