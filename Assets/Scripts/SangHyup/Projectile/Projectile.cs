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
    // ✨ [추가] 인터페이스 구현
    public float GetDamage() => damage; // 현재 데미지 반환

    // 만약 Inspector에 설정된 기본값을 쓰고 싶다면 별도 변수가 필요하겠지만,
    // 보통은 현재 날아가는 속도를 유지하는 것이 자연스럽습니다.
    public float GetSpeed() => speed;


    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }

    // ✨ [수정] GameObject ignore 대신 Collider2D ignoreCollider를 받음
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

        // ✨ [핵심] 물리 엔진 차원에서 충돌 무시 설정 (위치 겹쳐도 충돌 안 함)
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

        // IgnoreCollision을 썼으므로 여기서 별도의 ignore 체크 불필요

        Enemy enemy = collision.GetComponent<Enemy>();

        if (enemy != null)
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