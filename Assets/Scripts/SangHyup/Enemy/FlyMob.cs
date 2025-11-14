using UnityEngine;

public class FlyMob : Mob
{
    [Header("Fly Mob Specification")]
    [Tooltip("기차에게 꼬라박는 거리 조건")]
    [SerializeField] private float diveDistance = 15.0f;

    private void FixedUpdate()
    {
        if (!isAlive && !isStunned)
        {
            moveDirection           = Vector2.left;
            float deathMoveSpeed    = 30.0f;

            rigid2D.linearVelocity  = new Vector2(moveDirection.x * deathMoveSpeed, rigid2D.linearVelocity.y);

            return;
        }

        // Movement Logic
        if (isAlive && !isStunned)
        {
            SetMoveDirection(targetRigid.position);

            rigid2D.linearVelocity = moveDirection.normalized * moveSpeed;
        }
    }

    protected override void SetMoveDirection(Vector2 targetPos)
    {
        float deltaX = targetPos.x - transform.position.x;
        
        // 1) 수평 15 초과 → 좌/우로만 이동
        if (Mathf.Abs(deltaX) > diveDistance)
        {
            moveDirection = (deltaX > 0f) ? Vector2.right : Vector2.left;

            sprite.flipX = (moveDirection.x > 0f);

            return;
        }
        else
        {
            moveDirection = (targetPos - (Vector2)transform.position).normalized;
        }

        // Set sprite to move direction
        sprite.flipX = (moveDirection.x > 0f);

        return;
    }
}
