using UnityEngine;

public class LightningBolt : MonoBehaviour
{
    private float damage;
    [Header("기본 설정")]
    [Tooltip("번개가 유지될 시간")]
    [SerializeField] private float lifeTime = 1.0f;
    [Tooltip("랜덤 스케일 범위 (최소 ~ 최대)")]
    [SerializeField] private Vector2 scaleRange = new Vector2(0.85f, 2.0f);
    [System.Serializable]
    public struct LightningType { public string triggerName; }
    [Header("번개 종류 설정 (3가지)")]
    [Tooltip("3개의 번개 애니메이션 트리거 이름을 등록하세요.")]
    [SerializeField] private LightningType[] lightningTypes;
    private Animator animator;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private ObjectHost host;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        host = new ObjectHost(gameObject);
        host.BindCollision(Hit);
    }

    public void Initialize(float dmg)
    {
        damage = dmg;
        float randomScale = Random.Range(scaleRange.x, scaleRange.y);
        transform.localScale = new Vector3(randomScale, 1f, 1f);
        if (lightningTypes != null && lightningTypes.Length > 0)
            host.Presentation.Trigger(lightningTypes[Random.Range(0, lightningTypes.Length)].triggerName);
        Destroy(gameObject, lifeTime);
    }

    private void LateUpdate()
    {
        if (boxCollider != null && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            boxCollider.size = spriteRenderer.sprite.bounds.size;
            boxCollider.offset = spriteRenderer.sprite.bounds.center;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) { host.Collision(collision); }
    private void Hit(Collider2D collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Enemy")) return;
        TargetHandle target = TargetRegistry.Resolve(collision);
        if (target != null && target.IsActive) target.RequestDamage(damage);
    }

    private void OnDestroy() { host?.Release(); }
}
