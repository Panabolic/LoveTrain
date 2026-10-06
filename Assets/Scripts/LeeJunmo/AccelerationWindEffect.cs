using UnityEngine;

// Drives a scene-authored particle system; it never creates presentation objects.
[DisallowMultipleComponent]
public sealed class AccelerationWindEffect : MonoBehaviour
{
    [SerializeField] private Train train;
    [SerializeField] private ParticleSystem wind;
    [SerializeField] private Camera viewCamera;
    [SerializeField, Min(1)] private int maximumLines = 128;
    private ParticleSystem.Particle[] particles;
    private float emissionRemainder;
    private float previousSpeed;
    private float lineLength;

    private void Awake()
    {
        particles = new ParticleSystem.Particle[Mathf.Max(1, maximumLines)];
        if (wind != null)
        {
            var main = wind.main;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            wind.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void LateUpdate()
    {
        if (train == null || wind == null || viewCamera == null) return;
        GameManager game = GameManager.Instance;
        bool combat = game != null && !train.IsDead && Time.timeScale > 0f &&
            (game.CurrentState == GameState.Playing || game.CurrentState == GameState.Boss);
        if (!combat)
        {
            wind.Pause();
            if (game != null && (game.CurrentState == GameState.Die || game.CurrentState == GameState.Ending))
                wind.Clear();
            return;
        }
        if (!wind.isPlaying) wind.Play();
        float amount = Mathf.Max(0f, train.CurrentSpeed / Mathf.Max(1f, train.BaseSpeed) - 1f);
        bool slowing = train.CurrentSpeed < previousSpeed - 0.001f;
        previousSpeed = train.CurrentSpeed;
        float targetLength = amount > 0f ? 0.35f + amount * 2.8f : 0f;
        lineLength = Mathf.MoveTowards(lineLength, targetLength, Time.deltaTime * (slowing ? 4f : 8f));
        float movementSpeed = 18f + amount * 55f;
        int count = wind.GetParticles(particles);
        for (int index = 0; index < count; index++)
        {
            particles[index].velocity = Vector3.left * movementSpeed;
            particles[index].startSize3D = new Vector3(Mathf.Max(0.001f, lineLength), 0.035f, 1f);
            particles[index].startColor = new Color(1f, 1f, 1f, Mathf.Clamp01(amount * 1.4f) * 0.45f);
        }
        wind.SetParticles(particles, count);
        if (amount <= 0.001f) { emissionRemainder = 0f; return; }
        emissionRemainder += Mathf.Min(90f, amount * 80f) * Time.deltaTime;
        int emit = Mathf.Min(Mathf.FloorToInt(emissionRemainder), Mathf.Max(0, maximumLines - count));
        emissionRemainder -= emit;
        float halfHeight = viewCamera.orthographicSize;
        float halfWidth = halfHeight * viewCamera.aspect;
        for (int index = 0; index < emit; index++)
        {
            var parameters = new ParticleSystem.EmitParams
            {
                position = new Vector3(halfWidth + 3f, Random.Range(-halfHeight * 0.82f, halfHeight * 0.82f), 0f),
                velocity = Vector3.left * movementSpeed,
                startLifetime = (halfWidth * 2f + 6f) / movementSpeed,
                startSize3D = new Vector3(Mathf.Max(0.001f, lineLength), 0.035f, 1f),
                startColor = new Color(1f, 1f, 1f, Mathf.Clamp01(amount * 1.4f) * 0.45f)
            };
            wind.Emit(parameters, 1);
        }
    }

    private void OnDisable()
    {
        if (wind != null) wind.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        emissionRemainder = 0f;
        previousSpeed = lineLength = 0f;
    }
}
