using UnityEngine;

public class PoisonGas : MonoBehaviour
{
    private float damagePerTick;
    private float tickRate;
    private float moveSpeed;
    private float destroyXPos = -40f;
    private Collider2D myCollider;
    private readonly AreaPresence area = new AreaPresence();
    private ObjectHost host;
    private System.Action<TargetHandle> damageTarget;

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        host = new ObjectHost(gameObject);
        damageTarget = ApplyDamage;
        host.BindUpdate(StepArea);
        host.Presentation.BindSignal(nameof(AnimEvent_EnableGas), () =>
        {
            if (myCollider != null) myCollider.enabled = true;
        });
    }

    public void Initialize(float damage, float tickRate, float speed)
    {
        damagePerTick = damage;
        this.tickRate = tickRate;
        moveSpeed = speed;
        if (myCollider != null) myCollider.enabled = false;
        // Preserve the existing initialization: clear contacts without resetting elapsed ticks.
        area.Clear();
    }

    public void AnimEvent_EnableGas() { host.Presentation.Signal(nameof(AnimEvent_EnableGas)); }

    private void Update()
    {
        if (Time.timeScale == 0) return;
        host.Update(Time.deltaTime);
    }

    private void StepArea(float deltaTime)
    {
        transform.position += MovementRules.Linear(transform.TransformDirection(Vector3.left), moveSpeed, deltaTime);
        if (transform.position.x < destroyXPos) { Destroy(gameObject); return; }
        area.Advance(deltaTime, tickRate, damageTarget);
    }

    private void ApplyDamage(TargetHandle target) { target.RequestDamage(damagePerTick); }

    private void OnTriggerEnter2D(Collider2D collision) { area.Enter(TargetRegistry.Resolve(collision)); }
    private void OnTriggerExit2D(Collider2D collision) { area.Exit(TargetRegistry.Resolve(collision)); }
    private void OnDestroy() { host?.Release(); }
}
