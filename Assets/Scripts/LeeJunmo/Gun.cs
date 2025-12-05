using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public struct GunStats
{
    public float damage;
    public float speed;
    public float fireRate; // 발사 간격 (초 단위)
    public GameObject projectilePrefab;
    public GameObject laserPrefab;
}

public class Gun : MonoBehaviour
{
    [Header("입력")]
    public InputActionReference fireAction;

    [Header("기본 참조 (Inspector 할당)")]
    [Tooltip("기본 포신(Gun)의 스프라이트 렌더러")]
    public SpriteRenderer defaultGunRenderer;

    [Tooltip("기본 거치대(TrainHead)의 스프라이트 렌더러")]
    public SpriteRenderer holderRenderer;

    [Tooltip("기본 총구 위치")]
    public Transform defaultFirePoint;

    [Header("현재 스탯")]
    public GunStats CurrentStats; // 실제 게임에서 적용되는 최종 스탯

    public TrainLevelManager levelManager;

    // --- 내부 변수 (언더바 제거) ---
    private GunStats baseStats; // 아이템 배율이 적용되지 않은 '무기 순수 스탯'
    private float damageMultiplier = 0f; // 데미지 배율 (0.1 = 10% 증가)
    private float fireRateMultiplier = 0f; // 공속 배율
    private float damageEachLevel = 2f;

    private Sprite defaultHolderSprite; // 맨 처음 거치대 이미지 백업
    private GameObject currentVisualObj; // 현재 장착된 커스텀 외형 프리팹

    private IWeaponStrategy currentStrategy;

    // 외부에서 현재 총구 위치를 가져갈 수 있는 프로퍼티
    public Transform FirePoint { get; private set; }
    public float DamageMultiplier => damageMultiplier;
    public float FireRateMultiplier => fireRateMultiplier;

    private void Awake()
    {
        // 1. 초기 참조 및 백업
        if (defaultGunRenderer == null) defaultGunRenderer = GetComponent<SpriteRenderer>();
        if (holderRenderer == null && transform.parent != null)
            holderRenderer = transform.parent.GetComponent<SpriteRenderer>();

        if (holderRenderer != null) defaultHolderSprite = holderRenderer.sprite;

        // 초기 총구 설정
        FirePoint = defaultFirePoint;

        // 2. 게임 시작 시 설정된 스탯을 '기본 스탯'으로 저장
        baseStats = CurrentStats;
    }

    private void OnEnable()
    {
        if (fireAction != null) fireAction.action.Enable();
    }
    private void OnDisable()
    {
        if (fireAction != null) fireAction.action.Disable();
    }

    private void Start()
    {
        SetWeapon(new ProjectileStrategy());
        levelManager.OnLevelUp += OnLevelUpDamageIncrease;
    }

    // -------------------------------------------------------
    // 1. 외형 및 총구 교체 로직 (EquipVisual)
    // -------------------------------------------------------
    public GameObject EquipVisual(GameObject visualPrefab, Sprite newHolderSprite = null)
    {
        // 1. 기존 외형 제거
        UnequipVisual();

        if (visualPrefab == null) return null;

        // 2. 기본 포신 이미지 끄기
        if (defaultGunRenderer != null) defaultGunRenderer.enabled = false;

        // 3. 새 외형 프리팹 생성 (Gun의 자식으로)
        currentVisualObj = Instantiate(visualPrefab, transform);
        currentVisualObj.transform.localPosition = Vector3.zero;
        currentVisualObj.transform.localRotation = Quaternion.identity;

        // 4. 총구 위치 및 거치대 이미지 갱신
        WeaponVisual visualData = currentVisualObj.GetComponent<WeaponVisual>();
        if (visualData != null)
        {
            if (visualData.muzzlePoint != null)
            {
                FirePoint = visualData.muzzlePoint;
            }

            Sprite spriteToUse = visualData.customHolderSprite != null ? visualData.customHolderSprite : newHolderSprite;
            if (spriteToUse != null && holderRenderer != null)
            {
                holderRenderer.sprite = spriteToUse;
            }
        }

        // ✨ 생성된 오브젝트 반환 (아이템 관리용)
        return currentVisualObj;
    }

    public void UnequipVisual()
    {
        if (currentVisualObj != null) Destroy(currentVisualObj);

        // 기본 이미지 및 거치대 복구
        if (defaultGunRenderer != null) defaultGunRenderer.enabled = true;
        if (holderRenderer != null && defaultHolderSprite != null)
            holderRenderer.sprite = defaultHolderSprite;

        // 총구 위치 복구
        FirePoint = defaultFirePoint;
    }

    // -------------------------------------------------------
    // 2. 무기 변경 및 스탯 관리 로직
    // -------------------------------------------------------

    /// <summary>
    /// 새로운 무기(레이저 등)를 장착할 때 호출. 
    /// 무기의 '기본 스탯'을 변경하고, 기존 배율을 다시 적용함.
    /// </summary>
    public void ChangeBaseStats(GunStats newStats)
    {
        baseStats = newStats; // 베이스 스탯 교체
        UpdateStats(); // 배율 재적용하여 CurrentStats 갱신
    }

    public void OnLevelUpDamageIncrease()
    {
        CurrentStats.damage += (levelManager.CurrentLevel * damageEachLevel);
    }

    public void AddDamageMultiplier(float amount)
    {
        damageMultiplier += amount;
        UpdateStats();
    }

    public void AddFireRateMultiplier(float amount)
    {
        fireRateMultiplier += amount;
        UpdateStats();
    }

    private void UpdateStats()
    {
        // 공격력: (기본) * (1 + 배율)
        CurrentStats.damage = baseStats.damage * (1f + damageMultiplier);

        // 공속: (기본) / (1 + 배율)
        if (1f + fireRateMultiplier > 0)
        {
            CurrentStats.fireRate = baseStats.fireRate / (1f + fireRateMultiplier);
        }
        else
        {
            CurrentStats.fireRate = baseStats.fireRate;
        }

        CurrentStats.speed = baseStats.speed;
        CurrentStats.projectilePrefab = baseStats.projectilePrefab;
        CurrentStats.laserPrefab = baseStats.laserPrefab;

        if (currentStrategy != null)
        {
            currentStrategy.Initialize(this, CurrentStats);
        }

        Debug.Log($"[Gun] 스탯 갱신: Dmg {CurrentStats.damage}, Rate {CurrentStats.fireRate}");
    }

    // -------------------------------------------------------
    // 3. 전략 패턴 및 업데이트
    // -------------------------------------------------------
    public void SetWeapon(IWeaponStrategy newStrategy)
    {
        if (currentStrategy != null) currentStrategy.Unequip();
        currentStrategy = newStrategy;

        currentStrategy.Initialize(this, CurrentStats);
    }

    void Update()
    {
        // ✨ [수정] 일시정지(이벤트) 상태 처리
        if (Time.timeScale == 0)
        {
            // 게임이 멈췄다면 무조건 '발사 중지' 상태로 처리
            if (currentStrategy != null)
            {
                currentStrategy.Process(false);
            }
            return;
        }

        // 정상 플레이 상태
        bool isTriggerHeld = fireAction != null && fireAction.action.IsPressed();

        if (currentStrategy != null)
        {
            currentStrategy.Process(isTriggerHeld);
        }
    }
}