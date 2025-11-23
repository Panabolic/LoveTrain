using UnityEngine;
using UnityEngine.Assertions.Must;

public class RevolverBullet : MonoBehaviour
{
    [SerializeField]
    private float damage = 0;

    bool isCanHit = false;

    public void Init(float damage)
    {
        this.damage = damage;
    }

    public void CanHit()
    {
        isCanHit = true;
        transform.parent = null;
    }
        
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCanHit)
        {
            collision.GetComponent<Mob>().TakeDamage(damage);
        }
    }
}
