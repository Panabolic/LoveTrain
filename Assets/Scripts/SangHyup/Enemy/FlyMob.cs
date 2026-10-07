using UnityEngine;

public class FlyMob : Mob
{
    [Header("Fly Mob Specification")]
    [Tooltip("기차에게 꼬라박는 거리 조건")]
    [SerializeField] private float diveDistance = 15.0f;

    private void FixedUpdate()
    {
        RuntimeHost.FixedUpdate(Time.fixedDeltaTime);
    }

    protected override void SetMoveDirection(Vector2 targetPos)
    {
        moveDirection = MovementRules.FlyingDirection(transform.position, targetPos, diveDistance);
        RuntimeHost.Presentation.SetFlipX(moveDirection.x > 0f);
    }

    protected override Vector2 PlanVelocity(float worldSpeed)
    {
        return MovementRules.FlyingVelocity(moveDirection, moveSpeed, worldSpeed);
    }
}
