using UnityEngine;

public class LonginusLauncher : MonoBehaviour, IInstantiatedItem, IItemCooldownView
{
    private LonginusLauncher_SO itemData;
    [Header("연결")]
    [SerializeField] private Animator animator;
    private float damage;
    private float speed;
    private float cooldown;
    private float spearLifeTime;
    private GameObject spearPrefab;
    private GameObject pathDataPrefab;
    private LonginusPathData activePathData;
    private Camera mainCamera;
    private ObjectHost host;
    private PresentationLink presentation;
    private SummonAndAwait attack;

    public bool HasCooldown => cooldown > 0f;
    public float GetCooldownFillAmount()
    {
        if (attack == null) return 0f;
        if (attack.Cycle.IsWaitingForCompletion && attack.Cycle.RemainingCooldown <= 0f) return 1f;
        return ItemCooldownFill.FromRemaining(attack.Cycle.RemainingCooldown, cooldown);
    }

    private void Awake()
    {
        EnsureComposition();
        mainCamera = Camera.main;
    }

    private void EnsureComposition()
    {
        if (host != null) return;
        if (animator == null) animator = GetComponent<Animator>();
        host = new ObjectHost(gameObject);
        presentation = new PresentationLink(this, null, animator);
        attack = new SummonAndAwait(HasVisibleEnemy, CaptureTarget,
            () => presentation.Trigger("Summon"), EmitSpear,
            () => activePathData.paths.Length, () => presentation.Trigger("Shoot"),
            () => presentation.Trigger("Return"), () => cooldown, host.Scope.Guard);
        presentation.BindSignal(nameof(SpawnSpearFromAnim), attack.EmitFromAnimation);
        host.BindUpdate(attack.Advance);
    }

    public void Initialize(LonginusLauncher_SO data, GameObject user) { itemData = data; }

    public void UpgradeInstItem(ItemInstance instance)
    {
        EnsureComposition();
        int level = Mathf.Clamp(instance.currentUpgrade - 1, 0, itemData.damageByLevel.Length - 1);
        damage = itemData.damageByLevel[level];
        cooldown = itemData.cooldownByLevel[level];
        speed = itemData.spearSpeed;
        spearLifeTime = itemData.spearLifeTime;
        spearPrefab = itemData.SpearPrefab;
        pathDataPrefab = itemData.PathDataPrefab;
        if (activePathData == null && pathDataPrefab != null)
        {
            GameObject path = host.Scope.Spawn(pathDataPrefab, Vector3.zero, Quaternion.identity, true);
            path.name = pathDataPrefab.name + " (ActivePath)";
            activePathData = path.GetComponent<LonginusPathData>();
        }
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        Debug.Log($"롱기누스의 창 업그레이드 완료 (Lv.{instance.currentUpgrade})");
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;
        host.Update(Time.deltaTime);
    }

    private Vector3 CaptureTarget()
    {
        TargetHandle target = FindRandomVisibleEnemy();
        if (target != null) return target.Position;
        Vector3 fallback = mainCamera != null ? mainCamera.transform.position : transform.position;
        fallback.z = 0f;
        return fallback;
    }

    public void SpawnSpearFromAnim() { presentation.Signal(nameof(SpawnSpearFromAnim)); }

    private bool EmitSpear(int pathIndex, Vector3 capturedTarget, System.Action completed)
    {
        if (spearPrefab == null || activePathData == null || activePathData.paths.Length == 0) return false;
        var path = activePathData.paths[pathIndex];
        if (path.startPoint == null) return false;
        Vector3 start = path.startPoint.position;
        TargetHandle target = FindRandomVisibleEnemy();
        Vector3 destination = target != null ? target.Position : capturedTarget;
        GameObject spear = host.Scope.Spawn(spearPrefab, start, Quaternion.identity, false);
        LonginusSpear logic = spear.GetComponent<LonginusSpear>();
        if (logic != null)
        {
            SoundEventBus.Publish(SoundID.Item_LonginusSpawn);
            logic.Initialize(damage, speed, spearLifeTime, start, destination, completed);
        }
        return true;
    }

    private TargetHandle FindRandomVisibleEnemy()
    {
        if (PoolManager.instance == null || mainCamera == null) return null;
        return TargetQuery.RandomVisible(PoolManager.instance.CombatTargets.Targets, mainCamera);
    }

    private bool HasVisibleEnemy()
    {
        return PoolManager.instance != null && mainCamera != null &&
            TargetQuery.AnyVisible(PoolManager.instance.CombatTargets.Targets, mainCamera);
    }

    internal void ReleaseAttachment()
    {
        presentation?.ClearSignals();
        host?.Release();
    }

    private void OnDestroy() { ReleaseAttachment(); }
}
