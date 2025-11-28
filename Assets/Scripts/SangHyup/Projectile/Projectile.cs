using UnityEngine;

public class Projectile : MonoBehaviour
{
    protected float speed;
    protected float damage;
    protected Vector3 direction;

    // 풀링 반납용 원본 프리팹 참조
    protected GameObject originalPrefab;

    // 공격 판정 활성화 여부
    protected bool isCanHit = true;

    /// <summary>
    /// 투사체 초기화 함수 (자식에서 오버라이드 가능)
    /// </summary>
    public virtual void Init(float _damage, float _speed, Vector3 _dir, GameObject _prefab, bool startActive = true)
    {
        this.damage = _damage;
        this.speed = _speed;
        this.direction = _dir;
        this.originalPrefab = _prefab;
        this.isCanHit = startActive;

        // 방향에 맞춰 회전 (기본 로직)
        if (_dir != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        // 5초 뒤 자동 반납 (안전장치)
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

        if (enemy != null)
        {
            OnHitEnemy(enemy); // 적중 시 로직 분리
            Despawn();
        }
    }

    /// <summary>
    /// 적에게 적중했을 때 호출되는 함수 (자식에서 오버라이드하여 특수 효과 구현 가능)
    /// </summary>
    protected virtual void OnHitEnemy(Enemy enemy)
    {
        enemy.TakeDamage(damage);
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