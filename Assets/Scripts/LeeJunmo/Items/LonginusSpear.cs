using UnityEngine;

public class LonginusSpear : MonoBehaviour
{
    private float damage;
    private System.Action onDisappearCallback;
    private readonly FlightLifetime flight = new FlightLifetime();
    private ObjectHost host;

    private void Awake()
    {
        host = new ObjectHost(gameObject);
        host.BindUpdate(StepFlight);
        host.BindCollision(Hit);
    }

    public void Initialize(float damage, float speed, float lifeTime, Vector3 startPos, Vector3 targetPos, System.Action onDisappear)
    {
        this.damage = damage;
        onDisappearCallback = onDisappear;
        transform.position = startPos;
        Vector3 direction = (targetPos - startPos).normalized;
        flight.Initialize(direction, speed, lifeTime);
        transform.rotation = MovementRules.LookRotation2D(direction);
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;
        host.Update(Time.deltaTime);
    }

    private void StepFlight(float deltaTime)
    {
        Vector3 position = transform.position;
        bool expired = flight.Advance(deltaTime, ref position);
        transform.position = position;
        if (expired) Disappear();
    }

    private void OnTriggerEnter2D(Collider2D collision) { host.Collision(collision); }

    private void Hit(Collider2D collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Enemy")) return;
        TargetHandle target = TargetRegistry.Resolve(collision);
        if (target != null && target.IsActive)
        {
            SoundEventBus.Publish(SoundID.Item_Longinus);
            target.RequestDamage(damage);
        }
    }

    private void Disappear()
    {
        onDisappearCallback?.Invoke();
        Destroy(gameObject);
    }

    private void OnDestroy() { host?.Release(); }
}
