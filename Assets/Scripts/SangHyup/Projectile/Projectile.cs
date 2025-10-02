using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] protected float damage = 0.0f;

    protected Rigidbody2D rigid2D;

    void Awake()
    {
        rigid2D = GetComponent<Rigidbody2D>();
    }

    public virtual void Launch(Vector2 direction) { }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();

            enemy.TakeDamage(damage);
            OnHitTarget();
        }
    }

    protected virtual void OnHitTarget()
    {
        Deactivate();
    }

    protected virtual void Deactivate()
    {
        gameObject.SetActive(false);
    }
}
