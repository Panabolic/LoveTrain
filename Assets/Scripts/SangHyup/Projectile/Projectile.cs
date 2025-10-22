using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] protected float damage = 0.0f;

    protected Rigidbody2D rigid2D;

    protected virtual void Awake()
    {
        rigid2D = GetComponent<Rigidbody2D>();
    }

    public virtual void Launch(Vector2 direction) { }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();

            enemy.TakeDamage(damage);
            OnHitTarget();
        }
    }

    protected virtual void OnHitTarget() { }

    protected virtual void Deactivate()
    {
        gameObject.SetActive(false);
    }
}
