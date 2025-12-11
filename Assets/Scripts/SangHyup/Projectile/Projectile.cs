using UnityEngine;

public class Projectile : MonoBehaviour, IRicochetSource
{
    protected float speed;
    protected float damage;
    protected Vector3 direction;
    protected GameObject originalPrefab;
    protected bool isCanHit = true;

    [Header("도탄 설정")]
    [Tooltip("이 투사체가 도탄될 때 생성될 프리팹")]
    [SerializeField] private GameObject ricochetPrefab;

    private int currentBounceDepth = 0;
    private Collider2D myCollider; // 내 콜라이더 캐싱

    public GameObject GetRicochetPrefab() => ricochetPrefab;
    public int GetBounceDepth() => currentBounceDepth;
    public void SetBounceDepth(int depth) => currentBounceDepth = depth;

    // 인터페이스 구현
    public float GetDamage() => damage;
    public float GetSpeed() => speed;

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }

    public virtual void Init(float _damage, float _speed, Vector3 _dir, GameObject _prefab, bool startActive = true, int bounceDepth = 0, Collider2D ignoreCollider = null)
    {
        this.damage = _damage;
        this.speed = _speed;
        this.direction = _dir;
        this.originalPrefab = _prefab;
        this.isCanHit = startActive;

        if (_dir != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        this.currentBounceDepth = bounceDepth;
        if (ricochetPrefab == null) ricochetPrefab = _prefab;

        // 물리 엔진 차원에서 충돌 무시 설정
        if (ignoreCollider != null && myCollider != null)
        {
            Physics2D.IgnoreCollision(myCollider, ignoreCollider, true);
        }

        CancelInvoke(nameof(Despawn));
        Invoke(nameof(Despawn), 5.0f);
    }

    public void ActivateHit()
    {
        isCanHit = true;
        transform.parent = null;
    }

    protected virtual void Update()
    {
        if (isCanHit)
        {
            transform.Translate(Vector3.right * speed * Time.deltaTime);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isCanHit) return;

        Enemy enemy = collision.GetComponent<Enemy>();

        // ✨ [핵심 수정] 적이 존재하고 && 타겟팅 가능한 상태(화면 안)일 때만 처리
        if (enemy != null && enemy.IsTargetable)
        {
            OnHitEnemy(enemy);

            if (GameManager.Instance != null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    player.GetComponent<Inventory>()?.ProcessHitEvent(collision.gameObject, this.gameObject);
                }
            }

            Despawn();
        }
        // 화면 밖 적(IsTargetable == false)은 무시하고 통과함
    }

    protected virtual void OnHitEnemy(Enemy enemy)
    {
        enemy.TakeDamage(damage);
    }

    public void SetDamage(float damage)
    {
        this.damage = damage;
    }

    public void Despawn()
    {
        CancelInvoke(nameof(Despawn));
        if (BulletPoolManager.Instance != null && originalPrefab != null)
        {
            BulletPoolManager.Instance.ReturnToPool(gameObject, originalPrefab);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}