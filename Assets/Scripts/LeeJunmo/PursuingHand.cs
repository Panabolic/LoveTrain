using UnityEngine;

// Reuses the scene's hand object. Train retains ownership of the death tween.
public sealed class PursuingHand : MonoBehaviour
{
    [SerializeField] private Train train;
    [SerializeField] private StageManager stageManager;
    [SerializeField, Min(0.001f)] private float distanceToWorldScale = 0.1f;
    [SerializeField, Min(0f)] private float stageSpeedIncrease = 30f;

    private readonly HandPursuitState pursuit = new HandPursuitState();
    private SpriteRenderer[] handRenderers;
    private Collider2D trainCollider;
    private float previousTrainX;
    private float runStartBaseSpeed;
    private float frozenGap;
    private bool initialized;
    private bool stopped;

    public float Gap => pursuit.Gap;
    public float Speed => runStartBaseSpeed + Mathf.Max(0, (stageManager != null ? stageManager.StageNumber : 1) - 1) * stageSpeedIncrease;
    public double TotalDistance => pursuit.TotalDistance;
    public Vector3 Position => transform.position;

    public void InitializeRun(Train owner, float initialBaseSpeed)
    {
        train = owner;
        runStartBaseSpeed = Mathf.Max(0f, initialBaseSpeed);
        if (stageManager == null) stageManager = StageManager.Instance;
        handRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        trainCollider = train != null ? train.GetComponent<Collider2D>() : null;
        if (train == null) return;
        float worldGap = Mathf.Max(0f, TrainLeftEdge - HandRightEdge);
        pursuit.Reset(worldGap / Mathf.Max(0.001f, distanceToWorldScale));
        previousTrainX = train.transform.position.x;
        initialized = true;
        stopped = false;
    }

    public void CaptureTransitionGap()
    {
        if (!initialized || train == null) return;
        // Include movement already applied this frame before the transition froze play.
        frozenGap = Mathf.Max(0f, (TrainLeftEdge - HandRightEdge) / Mathf.Max(0.001f, distanceToWorldScale));
        pursuit.SetGap(frozenGap);
        previousTrainX = train.transform.position.x;
    }

    public void RestoreTransitionGap()
    {
        if (!initialized || train == null) return;
        pursuit.SetGap(frozenGap);
        previousTrainX = train.transform.position.x;
        PlaceAtGap();
    }

    public void StopPursuit() { stopped = true; }

    private void LateUpdate()
    {
        if (!initialized || stopped || train == null || train.IsDead) return;
        bool combat = GameManager.Instance != null && Time.timeScale > 0f &&
            (GameManager.Instance.CurrentState == GameState.Playing || GameManager.Instance.CurrentState == GameState.Boss);
        if (!combat)
        {
            previousTrainX = train.transform.position.x;
            return;
        }
        float displacement = train.transform.position.x - previousTrainX;
        previousTrainX = train.transform.position.x;
        bool caught = pursuit.Advance(train.CurrentSpeed, Speed, Time.deltaTime,
            displacement / Mathf.Max(0.001f, distanceToWorldScale));
        PlaceAtGap();
        if (caught) train.BeginPursuingHandDeath();
    }

    private float TrainLeftEdge => trainCollider != null ? trainCollider.bounds.min.x : train.transform.position.x;

    private float HandRightEdge
    {
        get
        {
            float right = float.NegativeInfinity;
            if (handRenderers != null)
                foreach (SpriteRenderer renderer in handRenderers)
                    if (renderer != null) right = Mathf.Max(right, renderer.bounds.max.x);
            return float.IsNegativeInfinity(right) ? transform.position.x : right;
        }
    }

    private void PlaceAtGap()
    {
        Vector3 position = transform.position;
        position.x += TrainLeftEdge - pursuit.Gap * Mathf.Max(0.001f, distanceToWorldScale) - HandRightEdge;
        transform.position = position;
    }
}

// Distances use the same units as train speed and stage progress.
public sealed class HandPursuitState
{
    public float Gap { get; private set; }
    public double TotalDistance { get; private set; }
    public void Reset(float gap) { Gap = System.Math.Max(0f, gap); TotalDistance = 0d; }
    public void SetGap(float gap) { Gap = System.Math.Max(0f, gap); }

    public bool Advance(float trainSpeed, float handSpeed, float dt, float trainPositionDistance)
    {
        if (dt <= 0f) return false;
        float travel = System.Math.Max(0f, handSpeed) * dt;
        TotalDistance += travel;
        float nextGap = Gap + System.Math.Max(0f, trainSpeed) * dt - travel + trainPositionDistance;
        Gap = System.Math.Max(0f, nextGap);
        return nextGap <= 0f;
    }
}
