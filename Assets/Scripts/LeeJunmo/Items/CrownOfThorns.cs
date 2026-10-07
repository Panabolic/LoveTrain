using UnityEngine;
using System.Collections;

public class CrownOfThorns : MonoBehaviour, IInstantiatedItem, IItemCooldownView
{
    private CrownOfThorns_SO itemData;
    private Animator animator;
    private float damage;
    private int lightningCount;
    private float cooldown;
    private float spawnRangeX;
    private float minDelay = 0.1f;
    private float maxDelay = 0.5f;
    private GameObject lightningPrefab;
    private readonly AttackCycle cycle = new AttackCycle();
    private ObjectHost host;

    public bool HasCooldown => cooldown > 0f;
    public float GetCooldownFillAmount() => ItemCooldownFill.FromRemaining(cycle.RemainingCooldown, cooldown);

    private void Awake()
    {
        animator = GetComponent<Animator>();
        host = new ObjectHost(gameObject);
        host.BindUpdate(StepAttack);
        host.Presentation.BindSignal(nameof(SpawnLightningFromAnim), () => StartCoroutine(SpawnLightningRoutine()));
    }

    public void Initialize(CrownOfThorns_SO data) { itemData = data; }

    public void UpgradeInstItem(ItemInstance instance)
    {
        int level = Mathf.Clamp(instance.currentUpgrade - 1, 0, itemData.damageByLevel.Length - 1);
        damage = itemData.damageByLevel[level];
        lightningCount = itemData.countByLevel[level];
        cooldown = itemData.cooldownByLevel[level];
        spawnRangeX = itemData.spawnRangeX;
        minDelay = itemData.spawnDelayMin;
        maxDelay = itemData.spawnDelayMax;
        lightningPrefab = itemData.LightningPrefab;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        Debug.Log($"면류관 업그레이드 완료 (Lv.{instance.currentUpgrade})");
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;
        host.Update(Time.deltaTime);
    }

    private void StepAttack(float deltaTime)
    {
        if (cycle.RemainingCooldown > 0f) { cycle.Elapse(deltaTime); return; }
        if (PoolManager.instance != null && PoolManager.instance.activeEnemies.Count > 0 && animator != null)
        {
            host.Presentation.Trigger("Flash");
            cycle.StartCooldown(cooldown);
        }
    }

    public void SpawnLightningFromAnim() { host.Presentation.Signal(nameof(SpawnLightningFromAnim)); }

    private IEnumerator SpawnLightningRoutine()
    {
        return BurstEmission.Sequence(() => lightningCount, () =>
        {
            SpawnSingleLightning();
            SoundEventBus.Publish(SoundID.Item_CrownOfThorn);
        }, () => Random.Range(minDelay, maxDelay), true);
    }

    private void SpawnSingleLightning()
    {
        if (lightningPrefab == null) return;
        Vector3 position = new Vector3(Random.Range(-spawnRangeX, spawnRangeX), -8.7f, 0f);
        GameObject bolt = host.Scope.Spawn(lightningPrefab, position, Quaternion.identity, false);
        LightningBolt logic = bolt.GetComponent<LightningBolt>();
        if (logic != null) logic.Initialize(damage);
    }

    private void OnDestroy() { host?.Release(); }
}
