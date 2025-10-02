using UnityEngine;

public class Bullet : Projectile
{
    protected float speed = 0.0f;

    private void Start()
    {
        speed = 10.0f;
        damage = 10.0f;
    }

    public override void Launch(Vector2 direction)
    {
        rigid2D.linearVelocity = direction.normalized * speed;
    }

    protected override void OnHitTarget()
    {
        Deactivate();
    }
}