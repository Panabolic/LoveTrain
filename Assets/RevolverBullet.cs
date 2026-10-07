using UnityEngine;

public class RevolverBullet : MonoBehaviour
{
    [SerializeField] private float damage = 0;
    private GameObject originalPrefab;
    private bool isCanHit = false;
    private ObjectHost host;

    private void Awake()
    {
        host = new ObjectHost(gameObject);
        host.BindCollision(Hit);
        host.Presentation.BindSignal(nameof(CanHit), ActivateHit);
    }

    private void OnEnable() { host.Activate(); }
    private void OnDisable() { host.Deactivate(); }

    public void Init(float damage, GameObject prefab)
    {
        this.damage = damage;
        originalPrefab = prefab;
        isCanHit = false;
    }

    public void CanHit() { host.Presentation.Signal(nameof(CanHit)); }

    private void ActivateHit()
    {
        isCanHit = true;
        transform.SetParent(null);
        CancelInvoke(nameof(Despawn));
        Invoke(nameof(Despawn), 5f);
    }

    private void OnTriggerEnter2D(Collider2D collision) { host.Collision(collision); }
    private void Hit(Collider2D collision)
    {
        if (isCanHit) TargetRegistry.Resolve(collision)?.RequestDamage(damage);
    }

    public void Despawn()
    {
        CancelInvoke(nameof(Despawn));
        if (BulletPoolManager.Instance != null && originalPrefab != null)
            BulletPoolManager.Instance.ReturnToPool(gameObject, originalPrefab);
        else Destroy(gameObject);
    }

    private void OnDestroy() { host?.Release(); }
}
