using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeBoss : Boss
{
    [Header("Pattern Settings")]
    [SerializeField] private float waitTimeAfterPatternEnd = 2.0f;

    // Retain existing prefab data; the forced low-health pattern is retired.
    [SerializeField, HideInInspector] private float EnragePatternThreshold = 0.2f;
    [SerializeField, HideInInspector] private float enrageTentacleDamage = 100f;
    [SerializeField, HideInInspector] private float enrageAttackDelay = 8f;

    [Header("Damage & Delay Settings")]
    [SerializeField] private float normalTentacleDamage = 50f;
    [SerializeField] private float normalAttackDelay = 3.0f;

    [Header("Reference")]
    [SerializeField] private GameObject tentaclePrefab;
    [SerializeField] private GameObject weakTentaclePrefab;
    [SerializeField] private Transform[] tentacleSpawnPoints;
    [SerializeField] private EyeBossBelt belt;

    private readonly AttackReservation pattern = new AttackReservation();
    private readonly List<int> visibleSpawnPoints = new List<int>(8);
    private Coroutine runningPatternCoroutine;
    private Train playerTrain;
    private Camera gameCamera;
    private float lastAttackSoundTime = -10f;

    protected override void Awake()
    {
        base.Awake();
        playerTrain = levelManager != null ? levelManager.GetComponent<Train>() : null;
        if (belt == null) belt = GetComponent<EyeBossBelt>();
        gameCamera = Camera.main;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        lastAttackSoundTime = -10f;
        runningPatternCoroutine = null;
        pattern.Cancel();
        if (belt != null) belt.ResetScroll();
    }

    protected override void Start()
    {
        base.Start();
        SoundEventBus.Publish(SoundID.Boss_Roar);
    }

    protected override void Update()
    {
        base.Update();
        if (!isAlive || isEntranceActive || !CombatIsRunning()) return;
        if (gameCamera == null) gameCamera = Camera.main;
        if (belt != null && gameCamera != null)
            belt.Advance(Time.deltaTime, playerTrain != null ? playerTrain.CurrentSpeed : 0f, gameCamera);

        if (gameCamera != null && pattern.TryReserve())
        {
            Coroutine started = StartCoroutine(SingleTentacleRoutine());
            // Instantiate callbacks can disable this owner before StartCoroutine returns.
            if (pattern.IsReserved) runningPatternCoroutine = started;
            else if (started != null) StopCoroutine(started);
        }
    }

    private IEnumerator SingleTentacleRoutine()
    {
        // The authored points follow the belt. Only their current visible positions
        // are candidates; a reserved warning is then fixed in world space.
        visibleSpawnPoints.Clear();
        if (tentacleSpawnPoints != null)
        {
            for (int i = 0; i < tentacleSpawnPoints.Length; i++)
            {
                Transform point = tentacleSpawnPoints[i];
                if (point != null && Tentacle.WarningIsVisible(gameCamera, point.position))
                    visibleSpawnPoints.Add(i);
            }
        }

        if (visibleSpawnPoints.Count == 0 || tentaclePrefab == null)
        {
            yield return null;
            runningPatternCoroutine = null;
            pattern.Complete();
            yield break;
        }

        int index = visibleSpawnPoints[Random.Range(0, visibleSpawnPoints.Count)];
        GameObject prefab = weakTentaclePrefab != null && Random.Range(0, 4) == 0
            ? weakTentaclePrefab : tentaclePrefab;
        GameObject obj = RuntimeHost.Scope.Spawn(prefab, tentacleSpawnPoints[index].position, Quaternion.identity, true);
        if (!RuntimeHost.Scope.IsActive || obj == null)
        {
            pattern.Cancel();
            yield break;
        }
        Tentacle tentacle = obj.GetComponent<Tentacle>();
        if (tentacle == null)
        {
            Destroy(obj);
            yield return null;
            runningPatternCoroutine = null;
            pattern.Complete();
            yield break;
        }

        tentacle.Setup(this, normalTentacleDamage, normalAttackDelay, RuntimeHost.Scope.Guard(TryPlayAttackSound));
        tentacle.BeginAttack();
        SoundEventBus.Publish(SoundID.Boss_TentacleSpawn);

        // Never overlap attack reservations, including the animation/cleanup phase.
        bool attackStarted = false;
        while (obj != null)
        {
            attackStarted |= tentacle.AttackStarted;
            yield return null;
        }
        float remaining = attackStarted ? Mathf.Max(0f, waitTimeAfterPatternEnd) : 0f;
        while (isAlive && remaining > 0f)
        {
            if (CombatIsRunning()) remaining -= Time.deltaTime;
            yield return null;
        }
        runningPatternCoroutine = null;
        pattern.Complete();
    }

    private static bool CombatIsRunning()
    {
        return Time.timeScale > 0f && GameManager.Instance != null &&
            (GameManager.Instance.CurrentState == GameState.Playing || GameManager.Instance.CurrentState == GameState.Boss);
    }

    private void TryPlayAttackSound()
    {
        if (Time.time - lastAttackSoundTime <= 0.1f) return;
        SoundEventBus.Publish(SoundID.Boss_TentacleAttack);
        lastAttackSoundTime = Time.time;
    }

    private void ClearAllTentacles()
    {
        RuntimeHost?.Scope.ReleaseBoundObjects();
    }

    public void RegisterTentacle(GameObject tentacle)
    {
        RuntimeHost.Scope.Track(tentacle);
    }

    public void UnregisterTentacle(GameObject tentacle) => RuntimeHost?.Scope.Forget(tentacle);

    protected override void OnDisable()
    {
        StopAllCoroutines();
        runningPatternCoroutine = null;
        pattern.Cancel();
        ClearAllTentacles();
        if (belt != null) belt.Hide();
        base.OnDisable();
    }

    protected override IEnumerator Die()
    {
        if (!TryBeginDeath()) yield break;
        if (runningPatternCoroutine != null) StopCoroutine(runningPatternCoroutine);
        runningPatternCoroutine = null;
        pattern.Cancel();
        ClearAllTentacles();
        if (belt != null) belt.Hide();

        if (killExplosionEffect != null)
            Instantiate(killExplosionEffect, transform.position, Quaternion.identity);
        yield return base.Die();
        yield return new WaitForSeconds(2.0f);

        if (killEvent != null && GameManager.Instance != null && EventManager.Instance != null &&
            !GameManager.Instance.IsTimeForEnding)
            EventManager.Instance.RequestEvent(killEvent);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddBossKillCount();
            GameManager.Instance.BossDied();
        }
        else if (StageManager.Instance != null)
            StageManager.Instance.StartStageTransitionSequence();

        CompleteDeathPresentation();
        Destroy(gameObject);
    }
}
