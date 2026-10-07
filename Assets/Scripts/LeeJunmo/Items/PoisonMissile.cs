using UnityEngine;

public class PoisonMissile : MonoBehaviour
{
    private GameObject gasPrefab;
    private float gasDamage;
    private float gasTickRate;
    private float gasMoveSpeed;
    private TargetHandle target;
    private readonly HomingFlight flight = new HomingFlight();
    private ObjectHost host;
    private System.Func<Vector3, Vector3?> selectTarget;

    private void Awake()
    {
        host = new ObjectHost(gameObject);
        selectTarget = ChooseNearestTarget;
        host.BindUpdate(StepFlight);
        host.BindCollision(Hit);
    }

    public void Initialize(float speed, float vDist, GameObject gasPrefab, float gDmg, float gTick, float gSpeed)
    {
        this.gasPrefab = gasPrefab;
        gasDamage = gDmg;
        gasTickRate = gTick;
        gasMoveSpeed = gSpeed;
        flight.Initialize(speed, vDist, 4f, transform.position);
        target = null;
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;
        host.Update(Time.deltaTime);
    }

    private void StepFlight(float deltaTime)
    {
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;
        Vector3? targetPosition = target != null && target.IsActive && target.IsTargetable ? target.Position : (Vector3?)null;
        bool explode = flight.Advance(deltaTime, ref position, ref rotation, targetPosition, selectTarget);
        transform.SetPositionAndRotation(position, rotation);
        if (explode) Explode();
    }

    private Vector3? ChooseNearestTarget(Vector3 position)
    {
        target = PoolManager.instance != null ? TargetQuery.Nearest(PoolManager.instance.CombatTargets.Targets,
            position, candidate => candidate.IsActive && candidate.IsTargetable) : null;
        return target != null ? target.Position : (Vector3?)null;
    }

    private void OnTriggerEnter2D(Collider2D collision) { host.Collision(collision); }

    private void Hit(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground")) Explode();
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            TargetHandle hit = TargetRegistry.Resolve(collision);
            if (hit != null && hit.IsTargetable) Explode();
        }
    }

    private void Explode()
    {
        if (gasPrefab != null)
        {
            GameObject gas = host.Scope.Spawn(gasPrefab, transform.position, Quaternion.identity, false);
            PoisonGas logic = gas.GetComponent<PoisonGas>();
            if (logic != null)
            {
                SoundEventBus.Publish(SoundID.Item_MissileBoom);
                logic.Initialize(gasDamage, gasTickRate, gasMoveSpeed);
            }
        }
        Destroy(gameObject);
    }

    private void OnDestroy() { host?.Release(); }
}
