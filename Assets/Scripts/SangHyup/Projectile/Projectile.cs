using UnityEngine;

public class Projectile : MonoBehaviour, IRicochetSource
{
    private float speed;
    private float damage;
    private GameObject originalPrefab;
    private bool isCanHit = true;
    [Header("도탄 설정")]
    [Tooltip("이 투사체가 도탄될 때 생성될 프리팹")]
    [SerializeField] private GameObject ricochetPrefab;
    private int currentBounceDepth = 0;
    private Collider2D myCollider;
    private ObjectHost host;

    public GameObject GetRicochetPrefab() => ricochetPrefab;
    public int GetBounceDepth() => currentBounceDepth;
    public void SetBounceDepth(int depth) => currentBounceDepth = depth;
    public float GetDamage() => damage;
    public float GetSpeed() => speed;

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        host = new ObjectHost(gameObject);
        host.BindUpdate(deltaTime =>
        {
            if (isCanHit) transform.position += MovementRules.Linear(transform.right, speed, deltaTime);
        });
        host.BindCollision(Hit);
    }

    private void OnEnable() { host.Activate(); }
    private void OnDisable() { host.Deactivate(); }

    public void Init(float _damage, float _speed, Vector3 _dir, GameObject _prefab, bool startActive = true,
        int bounceDepth = 0, Collider2D ignoreCollider = null)
    {
        damage = _damage;
        speed = _speed;
        originalPrefab = _prefab;
        isCanHit = startActive;
        if (_dir != Vector3.zero) transform.rotation = MovementRules.LookRotation2D(_dir);
        currentBounceDepth = bounceDepth;
        if (ricochetPrefab == null) ricochetPrefab = _prefab;
        if (ignoreCollider != null && myCollider != null) Physics2D.IgnoreCollision(myCollider, ignoreCollider, true);
        CancelInvoke(nameof(Despawn));
        Invoke(nameof(Despawn), 5f);
    }

    public void ActivateHit()
    {
        isCanHit = true;
        transform.parent = null;
    }

    private void Update() { host.Update(Time.deltaTime); }
    private void OnTriggerEnter2D(Collider2D collision) { host.Collision(collision); }

    private void Hit(Collider2D collision)
    {
        if (!isCanHit) return;
        TargetHandle target = TargetRegistry.Resolve(collision);
        if (target == null || !target.IsTargetable) return;
        target.RequestDamage(damage);
        if (GameManager.Instance != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) player.GetComponent<Inventory>()?.ProcessHitEvent(collision.gameObject, gameObject);
        }
        Despawn();
    }

    public void SetDamage(float damage) { this.damage = damage; }

    public void Despawn()
    {
        CancelInvoke(nameof(Despawn));
        if (BulletPoolManager.Instance != null && originalPrefab != null)
            BulletPoolManager.Instance.ReturnToPool(gameObject, originalPrefab);
        else Destroy(gameObject);
    }

    private void OnDestroy() { host?.Release(); }
}
