using System;
using UnityEngine;

/// <summary>Independent rising/homing flight and expiry state; its owner's release never cancels its movement.</summary>
internal sealed class HomingFlight
{
    private float speed;
    private float riseDistance;
    private float lifetime = 4f;
    private float elapsed;
    private Vector3 origin;
    private Vector3 lastTargetPosition;
    private bool homing;

    public void Initialize(float moveSpeed, float verticalDistance, float maxLifetime, Vector3 startPosition)
    {
        speed = moveSpeed;
        riseDistance = verticalDistance;
        lifetime = maxLifetime;
        origin = startPosition;
        elapsed = 0f;
        homing = false;
    }

    // True requests the facade's normal explosion; cancellation does not call this result path.
    public bool Advance(float deltaTime, ref Vector3 position, ref Quaternion rotation,
        Vector3? currentTargetPosition, Func<Vector3, Vector3?> chooseNearestTarget)
    {
        elapsed += deltaTime;
        if (elapsed >= lifetime) return true;
        if (!homing)
        {
            position += rotation * (Vector3.up * speed * deltaTime);
            if (Vector3.Distance(origin, position) >= riseDistance)
            {
                homing = true;
                Vector3? selected = chooseNearestTarget(position);
                if (!selected.HasValue) return true;
                lastTargetPosition = selected.Value;
            }
            return false;
        }

        if (currentTargetPosition.HasValue) lastTargetPosition = currentTargetPosition.Value;
        else if (Vector3.Distance(position, lastTargetPosition) < 0.5f) return true;

        Vector3 direction = lastTargetPosition - position;
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            rotation = Quaternion.Slerp(rotation, Quaternion.AngleAxis(angle, Vector3.forward), 15f * deltaTime);
        }
        position += rotation * (Vector3.up * (speed * 2.5f) * deltaTime);
        return false;
    }
}
