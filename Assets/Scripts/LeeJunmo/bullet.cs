using UnityEngine;

public class Bullet : MonoBehaviour
{
    private float speed;
    private float damage;
    private Vector3 direction;
    private GameObject originalPrefab;

    public void Init(float _damage, float _speed, Vector3 _dir, GameObject _prefab)
    {
        this.damage = _damage;
        this.speed = _speed;
        this.direction = _dir;
        this.originalPrefab = _prefab;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        CancelInvoke(nameof(Despawn));
        Invoke(nameof(Despawn), 5.0f);
    }

    void Update()
    {
        // 앞으로 이동
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Mob enemy = collision.GetComponent<Mob>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Despawn();
        }
    }

    private void Despawn()
    {
        CancelInvoke(nameof(Despawn));
        if (BulletPoolManager.Instance != null && originalPrefab != null)
            BulletPoolManager.Instance.ReturnToPool(gameObject, originalPrefab);
        else
            Destroy(gameObject);
    }
}