using UnityEngine;

public class LightningBolt : MonoBehaviour
{
    private float damage;

    [Header("기본 설정")]
    [Tooltip("번개가 유지될 시간")]
    [SerializeField] private float lifeTime = 1.0f;

    [Tooltip("랜덤 스케일 범위 (최소 ~ 최대)")]
    [SerializeField] private Vector2 scaleRange = new Vector2(0.85f, 2.0f);

    // ✨ [수정] 이제 트리거 이름만 있으면 됩니다. (크기는 자동)
    [System.Serializable]
    public struct LightningType
    {
        public string triggerName;
    }

    [Header("번개 종류 설정 (3가지)")]
    [Tooltip("3개의 번개 애니메이션 트리거 이름을 등록하세요.")]
    [SerializeField] private LightningType[] lightningTypes;

    private Animator animator;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer; // ✨ 스프라이트 확인용

    private void Awake()
    {
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // 컴포넌트 가져오기
    }

    public void Initialize(float dmg)
    {
        this.damage = dmg;

        // 1. 랜덤 스케일 적용
        float randomScale = Random.Range(scaleRange.x, scaleRange.y);
        transform.localScale = Vector3.one * randomScale;

        // 2. 랜덤 번개 타입 선택 및 애니메이션 실행
        if (lightningTypes != null && lightningTypes.Length > 0)
        {
            int randomIndex = Random.Range(0, lightningTypes.Length);
            ApplyLightningType(lightningTypes[randomIndex]);
        }

        // 3. 자동 파괴
        Destroy(gameObject, lifeTime);
    }

    private void ApplyLightningType(LightningType type)
    {
        if (animator != null)
        {
            animator.SetTrigger(type.triggerName);
        }
        // ✨ 수동 설정 로직 삭제됨
    }

    // ✨ [핵심 추가] 애니메이션에 따라 콜라이더 크기 자동 맞춤
    // (애니메이터가 Update에서 스프라이트를 바꾸므로, LateUpdate에서 크기를 맞춰야 정확함)
    private void LateUpdate()
    {
        if (boxCollider != null && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            // Reset Collider to Match Sprite Size
            boxCollider.size = spriteRenderer.sprite.bounds.size;
            boxCollider.offset = spriteRenderer.sprite.bounds.center;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 적 충돌 처리
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null && enemy.gameObject.activeSelf)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}