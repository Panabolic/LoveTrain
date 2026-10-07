using UnityEngine;

public class PoisonMissileLauncher : MonoBehaviour, IInstantiatedItem, IItemCooldownView
{
    private PoisonMissileLauncher_SO itemData;
    [Header("연결")]
    [Tooltip("미사일이 발사될 위치들")]
    [SerializeField] private Transform[] spawnPoints;
    [Header("비주얼")]
    [SerializeField] private Animator animatorFront;
    [SerializeField] private Animator animatorBack;
    private float missileSpeed;
    private float verticalDistance;
    private float cooldown;
    private float gasMoveSpeed;
    private float gasDamage;
    private float gasTickRate;
    private GameObject missilePrefab;
    private GameObject gasPrefab;
    private ObjectHost host;
    private PresentationLink presentation;
    private AnimatedPortFire attack;

    public bool HasCooldown => cooldown > 0f;
    public float GetCooldownFillAmount() => attack == null ? 0f : ItemCooldownFill.FromRemaining(attack.Cycle.RemainingCooldown, cooldown);

    private void Awake() { EnsureComposition(); }

    private void EnsureComposition()
    {
        if (host != null) return;
        if (animatorFront == null) animatorFront = transform.Find("FrontVisual")?.GetComponent<Animator>();
        if (animatorBack == null) animatorBack = transform.Find("BackVisual")?.GetComponent<Animator>();
        host = new ObjectHost(gameObject);
        presentation = new PresentationLink(this, null, animatorFront, animatorBack);
        attack = new AnimatedPortFire(
            () => PoolManager.instance != null && PoolManager.instance.activeEnemies.Count > 0,
            BeginPresentation, EmitMissile, () => spawnPoints.Length, () => cooldown);
        presentation.BindSignal(nameof(SpawnMissileFromAnim), attack.EmitFromAnimation);
        host.BindUpdate(attack.Advance);
    }

    public void Initialize(PoisonMissileLauncher_SO data, GameObject user) { itemData = data; }

    public void UpgradeInstItem(ItemInstance instance)
    {
        EnsureComposition();
        int level = Mathf.Clamp(instance.currentUpgrade - 1, 0, itemData.gasDamageByLevel.Length - 1);
        gasDamage = itemData.gasDamageByLevel[level];
        gasTickRate = itemData.gasTickRateByLevel[level];
        cooldown = itemData.cooldownByLevel[level];
        gasMoveSpeed = itemData.gasMoveSpeed;
        missileSpeed = itemData.missileSpeed;
        verticalDistance = itemData.verticalDistance;
        missilePrefab = itemData.MissilePrefab;
        gasPrefab = itemData.GasPrefab;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        Debug.Log($"독 미사일 런처 업그레이드 완료 (Lv.{instance.currentUpgrade})");
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Boss &&
            GameManager.Instance.CurrentState != GameState.Ending) return;
        if (Time.timeScale == 0) return;
        host.Update(Time.deltaTime);
    }

    private void BeginPresentation()
    {
        presentation.SetPlaybackSpeed(1f);
        presentation.Trigger("Fire");
        if (animatorFront == null && animatorBack == null) SpawnMissileFromAnim();
    }

    public void SpawnMissileFromAnim() { presentation.Signal(nameof(SpawnMissileFromAnim)); }

    private bool EmitMissile(int index)
    {
        if (missilePrefab == null || spawnPoints.Length == 0) return false;
        Transform point = spawnPoints[index];
        GameObject missile = host.Scope.Spawn(missilePrefab, point.position, point.rotation, false);
        PoisonMissile logic = missile.GetComponent<PoisonMissile>();
        SoundEventBus.Publish(SoundID.Item_Missile);
        if (logic != null) logic.Initialize(missileSpeed, verticalDistance, gasPrefab, gasDamage, gasTickRate, gasMoveSpeed);
        return true;
    }

    internal void ReleaseAttachment()
    {
        presentation?.ClearSignals();
        host?.Release();
    }

    private void OnDestroy() { ReleaseAttachment(); }
}
