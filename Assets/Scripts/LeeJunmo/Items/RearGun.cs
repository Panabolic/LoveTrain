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
        // 쿨타임 데이터는 이제 애니메이션 속도 조절용 기준값으로 사용하거나 무시해도 됨
        // currentFireInterval = itemData.cooldownByLevel[levelIndex]; 

        currentBulletCount = itemData.bulletCountByLevel[levelIndex];
        spreadAngle = itemData.fanSpreadAngle;
        bulletSpeed = itemData.bulletSpeed;
        bulletPrefab = itemData.BulletPrefab;

        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    private void Update()
    {
        if (Time.timeScale == 0 || playerGun == null) return;

        // 1. 공격 속도 반영 (애니메이션 배속 조절)
        // 기본 1배속 + 플레이어 공속 배율
        if (animator != null)
        {
            animator.speed = 1f + playerGun.FireRateMultiplier;
        }

        // 2. 입력 감지
        bool isTriggerHeld = false;
        if (playerGun.fireAction != null && playerGun.fireAction.action != null)
        {
            isTriggerHeld = playerGun.fireAction.action.IsPressed();
        }

        // 3. 상태 전환 (트리거 방식)
        if (isTriggerHeld)
        {
            // 누르고 있는데 아직 쏘는 상태가 아니라면 -> 발사 시작!
            if (!isShooting)
            {
                StartShooting();
            }
        }
        else
        {
            // 뗐는데 아직 쏘는 상태라면 -> 발사 중지!
            if (isShooting)
            {
                StopShooting();
            }
        }
    }

    private void StartShooting()
    {
        isShooting = true;
        if (animator != null)
        {
            animator.SetTrigger("Fire"); // 루프 시작
        }
    }

    private void StopShooting()
    {
        isShooting = false;
        if (animator != null)
        {
            animator.SetTrigger("StopFire"); // 루프 탈출
        }
    }

    // --- 애니메이션 이벤트 (루프 돌 때마다 호출됨) ---
    public void SpawnBulletFromAnim()
    {
        // 안전장치: 뗐는데도 이벤트가 늦게 들어오는 경우 방지
/*        if (!isShooting && playerGun != null && playerGun.fireAction != null)
        {
            if (!playerGun.fireAction.action.IsPressed()) return;
        }
*/
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