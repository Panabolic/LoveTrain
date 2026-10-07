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
    private float currentDamage;
    private int currentBulletCount;
    private float spreadAngle;
    private float bulletSpeed;
    private GameObject bulletPrefab;
    private bool isShooting = false;
    private ObjectHost host;
    private PresentationLink presentation;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        host = new ObjectHost(gameObject);
        presentation = new PresentationLink(this, null, animator);
        presentation.BindSignal(nameof(SpawnBulletFromAnim), EmitSpread);
        host.BindUpdate(_ => UpdateAnimation());
    }

    public void Initialize(RearGun_SO data, ItemInstance instance, GameObject user)
    {
        itemData = data;
        itemInstance = instance;
        playerGun = user.GetComponentInChildren<Gun>();
        if (playerGun == null) Debug.LogError("[RearGun] Gun을 찾을 수 없음!");
    }

    public void UpgradeInstItem(ItemInstance instance)
    {
        int level = Mathf.Clamp(instance.currentUpgrade - 1, 0, itemData.damageByLevel.Length - 1);
        currentDamage = itemData.damageByLevel[level];
        currentBulletCount = itemData.bulletCountByLevel[level];
        spreadAngle = itemData.fanSpreadAngle;
        bulletSpeed = itemData.bulletSpeed;
        bulletPrefab = itemData.BulletPrefab;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    private void Update() { host.Update(Time.deltaTime); }

    private void UpdateAnimation()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Boss &&
            GameManager.Instance.CurrentState != GameState.Ending)
        {
            if (isShooting) { isShooting = false; presentation.SetBool("IsFiring", false); }
            return;
        }
        if (Time.timeScale == 0 || playerGun == null) return;
        presentation.SetPlaybackSpeed(1f + playerGun.FireRateMultiplier);
        bool held = playerGun.fireAction != null && playerGun.fireAction.action != null &&
            playerGun.fireAction.action.IsPressed();
        if (isShooting != held) { isShooting = held; presentation.SetBool("IsFiring", held); }
    }

    public void SpawnBulletFromAnim() { presentation.Signal(nameof(SpawnBulletFromAnim)); }

    private void EmitSpread()
    {
        // Keep late animation shots even after input release.
        if (firePoint == null || bulletPrefab == null) return;
        float finalDamage = playerGun != null ? currentDamage * (1f + playerGun.DamageMultiplier) : currentDamage;
        BurstEmission.Fan(currentBulletCount, spreadAngle, offset => SpawnBullet(finalDamage, offset));
    }

    private void SpawnBullet(float damage, float angleOffset)
    {
        SoundEventBus.Publish(SoundID.Item_MeatGun);
        Quaternion rotation = firePoint.rotation * Quaternion.Euler(0f, 0f, 180f + angleOffset);
        Vector3 direction = rotation * Vector3.right;
        GameObject bullet = BulletPoolManager.Instance.Spawn(bulletPrefab, firePoint.position, rotation);
        Projectile logic = bullet.GetComponent<Projectile>();
        if (logic != null) logic.Init(damage, bulletSpeed, direction, bulletPrefab, true);
    }

    private void OnDestroy()
    {
        presentation?.ClearSignals();
        host?.Release();
    }
}
