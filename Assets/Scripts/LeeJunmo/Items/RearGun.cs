using UnityEngine;
using UnityEngine.InputSystem;

public class RearGun : MonoBehaviour, IInstantiatedItem
{
    private RearGun_SO itemData;
    private ItemInstance itemInstance;
    private Gun playerGun;

    [Header("컴포넌트")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Animator animator;

    // 스탯
    private float currentDamage;
    private int currentBulletCount;
    private float spreadAngle;
    private float bulletSpeed;
    private GameObject bulletPrefab;

    // 상태 관리
    private bool isShooting = false; // 현재 사격 모드인지 여부

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    public void Initialize(RearGun_SO data, ItemInstance instance, GameObject user)
    {
        this.itemData = data;
        this.itemInstance = instance;
        this.playerGun = user.GetComponentInChildren<Gun>();

        if (this.playerGun == null) Debug.LogError("[RearGun] Gun을 찾을 수 없음!");
    }

    public void UpgradeInstItem(ItemInstance instance)
    {
        int levelIndex = Mathf.Clamp(instance.currentUpgrade - 1, 0, itemData.damageByLevel.Length - 1);

        currentDamage = itemData.damageByLevel[levelIndex];
        // 쿨타임 데이터는 애니메이션 속도나 밸런싱에 필요 없다면 무시해도 됨

        currentBulletCount = itemData.bulletCountByLevel[levelIndex];
        spreadAngle = itemData.fanSpreadAngle;
        bulletSpeed = itemData.bulletSpeed;
        bulletPrefab = itemData.BulletPrefab;

        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Boss)
        {
            if (isShooting)
            {
                isShooting = false;
                UpdateAnimationState(); // 애니메이터에게 '멈춰!' 신호 보냄
            }
            return;
        }

        if (Time.timeScale == 0 || playerGun == null) return;

        // 1. 애니메이션 속도 동기화 (공속 버프 적용)
        if (animator != null)
        {
            animator.speed = 1f + playerGun.FireRateMultiplier;
        }

        // 2. 입력 감지 (안전하게)
        bool isTriggerHeld = false;
        if (playerGun.fireAction != null && playerGun.fireAction.action != null)
        {
            isTriggerHeld = playerGun.fireAction.action.IsPressed();
        }

        // 3. 상태 동기화 (SetBool 사용 - 버그 방지)
        // 입력 상태가 바뀌었을 때만 애니메이터 업데이트
        if (isShooting != isTriggerHeld)
        {
            isShooting = isTriggerHeld;
            UpdateAnimationState();
        }
    }

    private void UpdateAnimationState()
    {
        if (animator != null)
        {
            // Trigger 대신 Bool을 사용하여 상태를 강제로 맞춤
            // (누르고 있으면 true -> 계속 발사, 떼면 false -> 루프 종료)
            animator.SetBool("IsFiring", isShooting);
        }
    }

    // --- 애니메이션 이벤트 (루프 돌 때마다 호출됨) ---
    public void SpawnBulletFromAnim()
    {
        // [안전장치 제거됨] 
        // 마우스를 뗐더라도 마지막 모션의 탄환은 나가게 하기 위해
        // isShooting 체크를 하지 않습니다.

        if (firePoint == null || bulletPrefab == null) return;

        float finalDamage = currentDamage;
        if (playerGun != null)
        {
            finalDamage = currentDamage * (1f + playerGun.DamageMultiplier);
        }

        // 탄환 발사 로직
        if (currentBulletCount <= 1)
        {
            SpawnBullet(finalDamage, 0f);
        }
        else
        {
            float startAngleOffset = -((currentBulletCount - 1) * spreadAngle) / 2f;
            for (int i = 0; i < currentBulletCount; i++)
            {
                float angleOffset = startAngleOffset + (i * spreadAngle);
                SpawnBullet(finalDamage, angleOffset);
            }
        }
    }

    private void SpawnBullet(float damage, float angleOffset)
    {
        Quaternion finalRotation = firePoint.rotation * Quaternion.Euler(0, 0, 180f + angleOffset);
        Vector3 direction = finalRotation * Vector3.right;

        GameObject bullet = BulletPoolManager.Instance.Spawn(
            bulletPrefab,
            firePoint.position,
            finalRotation
        );

        Projectile bulletScript = bullet.GetComponent<Projectile>();
        if (bulletScript != null)
        {
            bulletScript.Init(damage, bulletSpeed, direction, bulletPrefab, true);
        }
    }
}