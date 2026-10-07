using UnityEngine;

public class LaserBeamSprite : MonoBehaviour, IRicochetSource
{
    private float damageInterval = 0.1f;
    private float damage;
    [Header("도탄 설정")]
    [SerializeField] private GameObject ricochetPrefab;
    private int currentBounceDepth = 0;
    private float ricochetCooldown = 0.2f;
    [SerializeField] private float ricochetBulletSpeed = 60f;
    private Animator animator;
    private readonly AreaPresence area = new AreaPresence(true, false);
    private ObjectHost host;
    private System.Action<TargetHandle> hitTarget;
    private System.Func<TargetHandle, bool> notifyHit;

    public GameObject GetRicochetPrefab() => ricochetPrefab;
    public int GetBounceDepth() => currentBounceDepth;
    public void SetBounceDepth(int depth) => currentBounceDepth = depth;
    public float GetDamage() => damage;
    public float GetSpeed() => ricochetBulletSpeed;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        host = new ObjectHost(gameObject);
        hitTarget = Hit;
        notifyHit = NotifyInventoryHit;
        host.BindUpdate(deltaTime => area.Advance(deltaTime, damageInterval, hitTarget));
        host.Presentation.BindSignal(nameof(AnimEvent_EnableHit), area.EnableHit);
        host.Presentation.BindSignal(nameof(AnimEvent_Deactivate), DeactivateSelf);
    }

    public void Init(float dmg, float tickRate)
    {
        damage = dmg;
        ricochetPrefab.GetComponent<Projectile>().SetDamage(dmg / 2.7f);
        damageInterval = Mathf.Max(tickRate, 0.02f);
    }

    private void OnEnable()
    {
        host.Activate();
        area.ResetForActivation();
    }

    private void OnDisable() { host.Deactivate(); }

    private void Update()
    {
        if (Time.timeScale == 0) return;
        host.Update(Time.deltaTime);
    }

    public void StopFiring()
    {
        if (!gameObject.activeSelf || !area.BeginStopping()) return;
        if (Time.timeScale == 0) { DeactivateSelf(); return; }
        if (animator != null) host.Presentation.Trigger("LaserEnd");
        else DeactivateSelf();
    }

    private void Hit(TargetHandle target)
    {
        target.RequestDamage(damage);
        area.NotifyWhenReady(Time.time, ricochetCooldown, target, notifyHit);
    }

    private bool NotifyInventoryHit(TargetHandle target)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;
        player.GetComponent<Inventory>()?.ProcessHitEvent(target.Owner, gameObject);
        return true;
    }

    private void OnTriggerEnter2D(Collider2D collision) { area.Enter(TargetRegistry.Resolve(collision)); }
    private void OnTriggerExit2D(Collider2D collision) { area.Exit(TargetRegistry.Resolve(collision)); }
    public void AnimEvent_EnableHit() { host.Presentation.Signal(nameof(AnimEvent_EnableHit)); }
    public void AnimEvent_Deactivate() { host.Presentation.Signal(nameof(AnimEvent_Deactivate)); }
    private void DeactivateSelf() { gameObject.SetActive(false); }
    private void OnDestroy() { host?.Release(); }
}
