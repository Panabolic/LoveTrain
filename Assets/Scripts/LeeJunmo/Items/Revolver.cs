using UnityEngine;
using System.Collections;

public class Revolver : MonoBehaviour, IInstantiatedItem, IItemCooldownView
{
    private Revolver_SO itemData;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    [SerializeField] private GameObject muzzle;
    private int currentDamage;
    private int currentBulletNum;
    private float currentCooldown;
    private float timeBetweenShots;
    private readonly AttackCycle cycle = new AttackCycle();
    private ObjectHost host;

    public bool HasCooldown => currentCooldown > 0f;
    public float GetCooldownFillAmount() => cycle.IsWaitingForCompletion ? 1f :
        ItemCooldownFill.FromRemaining(cycle.RemainingCooldown, currentCooldown);

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        host = new ObjectHost(gameObject);
        host.BindUpdate(StepAttack);
    }

    public void Initialize(Revolver_SO so) { itemData = so; }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Boss &&
            GameManager.Instance.CurrentState != GameState.Ending) return;
        host.Update(Time.deltaTime);
    }

    private void StepAttack(float deltaTime)
    {
        if (itemData == null || cycle.IsWaitingForCompletion) return;
        // Revolver checks readiness after decrement, unlike the two animation launchers.
        cycle.Elapse(deltaTime);
        if (cycle.IsReady) StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        return BurstEmission.Sequence(() => currentBulletNum, () =>
        {
            CreateBullet();
            SoundEventBus.Publish(SoundID.Item_GunSlave);
        }, () => timeBetweenShots, false, cycle.Hold, () => cycle.ResumeWithCooldown(currentCooldown));
    }

    private void CreateBullet()
    {
        Vector3 position = muzzle != null ? muzzle.transform.position : transform.position;
        GameObject bullet = BulletPoolManager.Instance.Spawn(itemData.BulletPrefab, position, Quaternion.identity);
        if (muzzle != null)
        {
            bullet.transform.SetParent(muzzle.transform);
            bullet.transform.localPosition = Vector3.zero;
            bullet.transform.localRotation = Quaternion.identity;
        }
        RevolverBullet logic = bullet.GetComponent<RevolverBullet>();
        if (logic != null) logic.Init(currentDamage, itemData.BulletPrefab);
    }

    public void UpgradeInstItem(ItemInstance instance)
    {
        if (itemData == null) return;
        int level = instance.currentUpgrade - 1;
        currentDamage = itemData.damageByLevel[level];
        currentBulletNum = itemData.bulletNumByLevel[level];
        currentCooldown = itemData.cooldownByLevel[level];
        timeBetweenShots = 0.2f;
        if (host != null) host.Presentation.ApplyUpgrade(itemData, instance.currentUpgrade);
    }

    private void OnDestroy() { host?.Release(); }
}
