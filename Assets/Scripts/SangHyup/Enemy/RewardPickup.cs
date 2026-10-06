using UnityEngine;

public enum RewardPickupKind { Flesh = 0, Soul = 1, Fuel = 2 }

// The authored prefab owns presentation; this component only moves it and requests its reward.
public sealed class RewardPickup : MonoBehaviour
{
    [SerializeField] private RewardPickupKind kind;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D pickupCollider;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private Sprite[] fleshSprites;
    [SerializeField] private SpriteRenderer[] outlineRenderers;
    [SerializeField, Min(0f)] private float attractionDelay = 0.5f;
    [SerializeField, Min(0.1f)] private float attractionSpeed = 18f;
    [SerializeField, Min(0f)] private float gravity = 14f;
    [SerializeField, Range(0f, 1f)] private float bounce = 0.15f;
    [SerializeField, Min(0f)] private float floatRiseSpeed = 1.5f;
    [SerializeField, Min(1f)] private float lifeTime = 20f;
    [SerializeField, Min(0.05f)] private float collectDistance = 0.45f;
    [SerializeField] private LayerMask trackMask = 1 << 0;

    private TrainLevelManager wallet;
    private Train train;
    private GameManager gameManager;
    private Camera mainCamera;
    private Vector2 velocity;
    private float groundY;
    private bool hasGround;
    private int flesh;
    private int souls;
    private float fuelPercent;
    private float age;
    private bool initialized;
    private bool collected;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (pickupCollider == null) pickupCollider = GetComponent<Collider2D>();
        if (trail == null) trail = GetComponent<TrailRenderer>();
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        gameManager = GameManager.Instance;
        if (gameManager != null) gameManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        if (gameManager != null) gameManager.OnGameStateChanged -= OnGameStateChanged;
        if (trail != null) trail.emitting = false;
    }

    public static void Spawn(RewardPickup prefab, Vector3 position, TrainLevelManager owner,
        int fleshReward = 0, int soulReward = 0, float fuelReward = 0f)
    {
        if (owner == null || (fleshReward <= 0 && soulReward <= 0 && fuelReward <= 0f)) return;
        if (prefab == null)
        {
            Debug.LogError("Reward pickup prefab is not assigned; configure the enemy's drop reference.", owner);
            return;
        }
        RewardPickup pickup = Instantiate(prefab, position, Quaternion.identity);
        pickup.Initialize(owner, fleshReward, soulReward, fuelReward);
    }

    public void Initialize(TrainLevelManager owner, int fleshReward, int soulReward, float fuelReward)
    {
        wallet = owner;
        train = wallet != null ? wallet.GetComponent<Train>() : null;
        flesh = Mathf.Max(0, fleshReward);
        souls = Mathf.Max(0, soulReward);
        fuelPercent = Mathf.Clamp01(fuelReward);
        age = 0f;
        collected = false;
        initialized = true;
        if (body != null) body.linearVelocity = Vector2.zero;
        if (trail != null) { trail.Clear(); trail.emitting = kind == RewardPickupKind.Soul; }
        if (kind == RewardPickupKind.Flesh)
        {
            if (spriteRenderer != null && fleshSprites != null && fleshSprites.Length > 0)
                spriteRenderer.sprite = fleshSprites[Random.Range(0, fleshSprites.Length)];
            velocity = new Vector2(Random.Range(-4f, 4f), Random.Range(2f, 4f));
            RaycastHit2D ground = default;
            hasGround = false;
            foreach (RaycastHit2D hit in Physics2D.RaycastAll(transform.position + Vector3.up * 0.5f,
                Vector2.down, 1000f, trackMask))
            {
                if (hit.collider == null || hit.collider == pickupCollider || hit.collider.isTrigger || hit.normal.y < 0.5f) continue;
                ground = hit;
                hasGround = true;
                break;
            }
            if (hasGround)
            {
                groundY = ground.point.y + VisualRadius;
                float distance = transform.position.y - groundY;
                // High flying enemies still drop onto the rail before attraction starts.
                if (distance > 1f) velocity.y = -distance / Mathf.Max(0.05f, attractionDelay * 0.7f);
            }
        }
        RefreshOutline();
        if (gameManager != null) OnGameStateChanged(gameManager.CurrentState);
    }

    private void RefreshOutline()
    {
        if (spriteRenderer == null || outlineRenderers == null) return;
        foreach (SpriteRenderer outline in outlineRenderers)
        {
            if (outline == null) continue;
            outline.sprite = spriteRenderer.sprite;
            outline.flipX = spriteRenderer.flipX;
            outline.flipY = spriteRenderer.flipY;
            outline.sortingLayerID = spriteRenderer.sortingLayerID;
            outline.sortingOrder = spriteRenderer.sortingOrder - 1;
        }
    }

    private float VisualRadius => spriteRenderer != null ? Mathf.Max(0.05f, spriteRenderer.bounds.extents.y) : 0.12f;

    private void Update()
    {
        if (!initialized || collected) return;
        if (wallet == null || train == null || train.IsDead || train.IsDying)
        {
            Discard();
            return;
        }
        if (gameManager == null || Time.timeScale <= 0f ||
            (gameManager.CurrentState != GameState.Playing && gameManager.CurrentState != GameState.Boss)) return;

        float dt = Time.deltaTime;
        age += dt;
        if (age >= lifeTime) { Discard(); return; }
        Vector3 position = transform.position;
        if (age < attractionDelay)
        {
            if (kind == RewardPickupKind.Flesh)
            {
                velocity.y -= gravity * dt;
                position += (Vector3)velocity * dt;
                if (hasGround && position.y < groundY)
                {
                    position.y = groundY;
                    velocity.y = Mathf.Abs(velocity.y) * bounce;
                    velocity.x *= 0.8f;
                    if (velocity.y < 0.25f) velocity.y = 0f;
                }
                ReflectAtScreenBounds(ref position);
            }
            else if (kind == RewardPickupKind.Soul)
                position += Vector3.up * (floatRiseSpeed * dt);
        }
        else
        {
            float speed = attractionSpeed * (1f + Mathf.Min(2f, age - attractionDelay));
            position = Vector3.MoveTowards(position, train.transform.position, speed * dt);
            if (Vector3.Distance(position, train.transform.position) <= collectDistance)
            {
                Collect();
                return;
            }
        }
        if (body != null && body.simulated) body.position = position;
        else transform.position = position;
    }

    private void ReflectAtScreenBounds(ref Vector3 position)
    {
        if (mainCamera == null || !mainCamera.orthographic) return;
        float halfHeight = mainCamera.orthographicSize;
        float halfWidth = halfHeight * mainCamera.aspect;
        Vector3 center = mainCamera.transform.position;
        float radius = Mathf.Min(VisualRadius, halfHeight * 0.25f);
        float left = center.x - halfWidth + radius;
        float right = center.x + halfWidth - radius;
        if (position.x < left) { position.x = left; velocity.x = Mathf.Abs(velocity.x) * bounce; }
        if (position.x > right) { position.x = right; velocity.x = -Mathf.Abs(velocity.x) * bounce; }
        float top = center.y + halfHeight - radius;
        if (position.y > top) { position.y = top; velocity.y = -Mathf.Abs(velocity.y) * bounce; }
    }

    private void OnGameStateChanged(GameState state)
    {
        if (!initialized || collected) return;
        if (state == GameState.Ending)
        {
            // Victory must retain the final boss's currency even when homing is interrupted.
            if (kind != RewardPickupKind.Fuel) Collect();
            else Discard();
        }
        else if (state == GameState.Die || state == GameState.Title || state == GameState.Start)
            Discard();
        else if (trail != null)
            trail.emitting = kind == RewardPickupKind.Soul &&
                (state == GameState.Playing || state == GameState.Boss);
    }

    private void Collect()
    {
        if (collected) return;
        collected = true;
        if (pickupCollider != null) pickupCollider.enabled = false;
        if (wallet != null && (flesh > 0 || souls > 0)) wallet.AddRewards(flesh, souls);
        if (train != null && fuelPercent > 0f) train.HealPercent(fuelPercent);
        Destroy(gameObject);
    }

    private void Discard()
    {
        if (collected) return;
        collected = true;
        if (pickupCollider != null) pickupCollider.enabled = false;
        Destroy(gameObject);
    }
}
