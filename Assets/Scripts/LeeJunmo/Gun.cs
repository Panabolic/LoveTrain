using UnityEngine;
using UnityEngine.InputSystem;

// 데이터 전달용 구조체
[System.Serializable]
public struct GunStats
{
    public float damage;
    public float speed;
    public float fireRate;
    public GameObject projectilePrefab; // 일반 총알 프리팹
    public GameObject laserPrefab;      // 레이저 프리팹
}

public class Gun : MonoBehaviour
{
    [Header("입력")]
    public InputActionReference fireAction;

    [Header("현재 스탯")]
    public GunStats CurrentStats;

    // 현재 장착된 전략
    private IWeaponStrategy currentStrategy;
    public Transform firePoint;

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
        // 시작 시 기본 무기(투사체) 장착
        SetWeapon(new ProjectileStrategy());
    }

    // [핵심] 무기 교체 함수 (아이템 획득 시 호출)
    public void SetWeapon(IWeaponStrategy newStrategy)
    {
        if (currentStrategy != null) currentStrategy.Unequip();

        currentStrategy = newStrategy;
        currentStrategy.Initialize(this, CurrentStats);
    }

    void Update()
    {
        if (Time.timeScale == 0) return;

        // 입력 상태 확인
        bool isTriggerHeld = fireAction != null && fireAction.action.IsPressed();

        // 전략에게 위임
        if (currentStrategy != null)
        {
            currentStrategy.Process(isTriggerHeld);
        }
    }
}