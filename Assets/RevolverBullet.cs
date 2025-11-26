using UnityEngine;

public class RevolverBullet : MonoBehaviour
{
    [SerializeField] private float damage = 0;

    private GameObject originalPrefab; // 풀 반납용
    private bool isCanHit = false;

    public void Init(float damage, GameObject prefab)
    {
        this.damage = damage;
        this.originalPrefab = prefab;
        this.isCanHit = false; // 아직 공격 판정 없음 (부착 상태)
    }

    // [중요] 이 함수는 애니메이션 이벤트(Animation Event)에서 호출해주세요!
    public void CanHit()
    {
        isCanHit = true;
        transform.SetParent(null); // [핵심] 부모(총구)에서 떨어져 나감

        // 안전장치: 5초 뒤 강제 반납
        CancelInvoke(nameof(Despawn));
        Invoke(nameof(Despawn), 5.0f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // CanHit() 이후에만 충돌 처리
        if (isCanHit)
        {
            Mob mob = collision.GetComponent<Mob>();
            if (mob != null)
            {
                mob.TakeDamage(damage);
            }
        }
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