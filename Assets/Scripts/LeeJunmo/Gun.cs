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

    // --- 내부 변수 ---
    private GunStats baseStats; // 아이템 배율이 적용되지 않은 '무기 순수 스탯'
    private float damageMultiplier = 0f; // 데미지 배율 (0.1 = 10% 증가)
    private float fireRateMultiplier = 0f; // 공속 배율
    private float damageEachLevel = 2f;

    // ✨ [추가] 무기 고유 데미지 비율 (기본값 1.0)
    private float weaponDamageRatio = 1.0f;

    private Sprite defaultHolderSprite;
    private GameObject currentVisualObj;

    private IWeaponStrategy currentStrategy;

    // 외부에서 현재 총구 위치를 가져갈 수 있는 프로퍼티
    public Transform FirePoint { get; private set; }
    public float DamageMultiplier => damageMultiplier;
    public float FireRateMultiplier => fireRateMultiplier;
    public float DamageEachLevel => damageEachLevel;

    // ✨ [추가] 외부에서 순수 베이스 스탯을 읽을 수 있게 함 (LaserGun 등에서 사용)
    public GunStats BaseStats => baseStats;

    private void Awake()
    {
        if (defaultGunRenderer == null) defaultGunRenderer = GetComponent<SpriteRenderer>();
        if (holderRenderer == null && transform.parent != null)
            holderRenderer = transform.parent.GetComponent<SpriteRenderer>();

        if (holderRenderer != null) defaultHolderSprite = holderRenderer.sprite;

        FirePoint = defaultFirePoint;
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
    // 외형 변경 (Visual)
    // -------------------------------------------------------
    public GameObject EquipVisual(GameObject visualPrefab, Sprite newHolderSprite = null)
    {
        UnequipVisual();

        if (visualPrefab == null) return null;

        if (defaultGunRenderer != null) defaultGunRenderer.enabled = false;

        currentVisualObj = Instantiate(visualPrefab, transform);
        currentVisualObj.transform.localPosition = Vector3.zero;
        currentVisualObj.transform.localRotation = Quaternion.identity;

        WeaponVisual visualData = currentVisualObj.GetComponent<WeaponVisual>();
        if (visualData != null)
        {
            if (visualData.muzzlePoint != null) FirePoint = visualData.muzzlePoint;

            Sprite spriteToUse = visualData.customHolderSprite != null ? visualData.customHolderSprite : newHolderSprite;
            if (spriteToUse != null && holderRenderer != null) holderRenderer.sprite = spriteToUse;
        }

        return currentVisualObj;
    }

    public void UnequipVisual()
    {
        if (currentVisualObj != null) Destroy(currentVisualObj);

        if (defaultGunRenderer != null) defaultGunRenderer.enabled = true;
        if (holderRenderer != null && defaultHolderSprite != null)
            holderRenderer.sprite = defaultHolderSprite;

        FirePoint = defaultFirePoint;
    }

    // -------------------------------------------------------
    // 스탯 관리 (핵심 수정됨)
    // -------------------------------------------------------

    public void ChangeBaseStats(GunStats newStats)
    {
        baseStats = newStats;
        UpdateStats();
    }

    // ✨ [추가] 무기 비율 설정 함수
    public void SetWeaponDamageRatio(float ratio)
    {
        weaponDamageRatio = ratio;
        UpdateStats();
    }

    public void OnLevelUpDamageIncrease()
    {
        // 직접 더하지 않고 UpdateStats를 호출하여 공식대로 재계산
        UpdateStats();
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
        // ✨ [핵심 수정] 데미지 계산 공식 통합
        // (기본뎀 + 레벨성장) * (1 + 아이템배율) * (무기비율)
        float growthDamage = levelManager.CurrentLevel * damageEachLevel;

        CurrentStats.damage = (baseStats.damage + growthDamage)
                              * (1f + damageMultiplier)
                              * weaponDamageRatio;

        // 공속 계산
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

        Debug.Log($"[Gun] 스탯 갱신: Dmg {CurrentStats.damage} (Ratio: {weaponDamageRatio}), Rate {CurrentStats.fireRate}");
    }

    // -------------------------------------------------------
    // 전략 및 업데이트
    // -------------------------------------------------------
    public void SetWeapon(IWeaponStrategy newStrategy)
    {
        if (currentStrategy != null) currentStrategy.Unequip();
        currentStrategy = newStrategy;

        currentStrategy.Initialize(this, CurrentStats);
    }

    void Update()
    {
        if (GameManager.Instance != null)
        {
            GameState currentState = GameManager.Instance.CurrentState;
            if (currentState == GameState.Die || currentState == GameState.StageTransition)
            {
                if (currentStrategy != null) currentStrategy.Process(false);
                return;
            }
        }

        if (Time.timeScale == 0)
        {
            if (currentStrategy != null) currentStrategy.Process(false);
            return;
        }

        bool isTriggerHeld = fireAction != null && fireAction.action.IsPressed();

        if (currentStrategy != null)
        {
            currentStrategy.Process(isTriggerHeld);
        }
    }
}