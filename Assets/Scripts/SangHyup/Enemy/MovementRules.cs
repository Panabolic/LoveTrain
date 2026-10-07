using UnityEngine;

// Calculations do not own transforms, physics clocks, or actor lifetime.
internal static class MovementRules
{
    public static Vector3 Linear(Vector3 direction, float speed, float deltaTime)
    {
        return direction * speed * deltaTime;
    }
    public static Quaternion LookRotation2D(Vector3 direction, float offset = 0f)
    {
        return Quaternion.AngleAxis(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + offset, Vector3.forward);
    }
    public static Vector2 GroundDirection(Vector2 position, Vector2 target)
    {
        float x = target.x - position.x;
        return x > 0f ? Vector2.right : x < 0f ? Vector2.left : Vector2.zero;
    }
    public static Vector2 FlyingDirection(Vector2 position, Vector2 target, float diveDistance)
    {
        float x = target.x - position.x;
        return Mathf.Abs(x) > diveDistance ? (x > 0f ? Vector2.right : Vector2.left) : (target - position).normalized;
    }
    public static Vector2 GroundVelocity(Vector2 direction, float speed, float verticalSpeed, float worldSpeed)
    {
        return new Vector2(direction.x * speed - worldSpeed, verticalSpeed);
    }
    public static Vector2 FlyingVelocity(Vector2 direction, float speed, float worldSpeed)
    {
        return direction.normalized * speed - Vector2.right * worldSpeed;
    }
    public static Vector2 DeathVelocity(float verticalSpeed) { return new Vector2(-30f, verticalSpeed); }
    public static float TrainPosition(float current, float direction, float speed, float deltaTime, float minimum)
    {
        return Mathf.Max(current + direction * speed * deltaTime, minimum);
    }
    public static float PushTrainLeft(float current, float distance, float minimum)
    {
        return Mathf.Max(minimum, current - distance);
    }
}
