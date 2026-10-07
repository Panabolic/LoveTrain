using UnityEngine;

/// <summary>Owns straight flight and its scaled lifetime; normal expiry is reported separately from destruction.</summary>
internal sealed class FlightLifetime
{
    private Vector3 direction;
    private float speed;
    private float lifetime;
    private float elapsed;

    public void Initialize(Vector3 direction, float speed, float lifetime)
    {
        this.direction = direction;
        this.speed = speed;
        this.lifetime = lifetime;
        elapsed = 0f;
    }

    public bool Advance(float deltaTime, ref Vector3 position)
    {
        position += MovementRules.Linear(direction, speed, deltaTime);
        elapsed += deltaTime;
        return elapsed >= lifetime;
    }
}
