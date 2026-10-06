using System.Collections;
using UnityEngine;

// A shootable fuel container; reward is granted only by the death path, never by cleanup.
public class FuelBarrel : Enemy
{
    [SerializeField, Range(0f, 1f)] private float fuelRewardPercent = 0.1f;
    [SerializeField] private float driftSpeed = 1.5f;
    [SerializeField] private float lifetime = 25f;
    private float age;
    private float direction;
    private static Sprite barrelSprite;

    protected override void Awake()
    {
        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetBarrelSprite();
        renderer.sortingOrder = 5;
        var box = gameObject.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.65f, 0.85f);
        box.isTrigger = true;
        var body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        hp = 15f; damage = 0f; exp = 0f;
        base.Awake();
        direction = targetRigid != null && transform.position.x < targetRigid.position.x ? 1f : -1f;
    }

    protected override void Update()
    {
        base.Update();
        if (GameManager.Instance == null || Time.timeScale <= 0f ||
            (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Boss)) return;
        transform.position += Vector3.right * (direction * driftSpeed * Time.deltaTime);
        age += Time.deltaTime;
        if (age >= lifetime) DespawnWithoutExp();
    }

    protected override IEnumerator Die()
    {
        if (!isAlive) yield break;
        isAlive = false;
        if (levelManager != null)
        {
            var train = levelManager.GetComponent<Train>();
            if (train != null) train.HealPercent(fuelRewardPercent);
        }
        yield return base.Die();
        Destroy(gameObject);
    }

    public override void DespawnWithoutExp()
    {
        base.DespawnWithoutExp();
        Destroy(gameObject);
    }

    private static Sprite GetBarrelSprite()
    {
        if (barrelSprite != null) return barrelSprite;
        var texture = new Texture2D(16, 20, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        var pixels = new Color[16 * 20];
        for (int y = 1; y < 19; y++)
        for (int x = 2; x < 14; x++)
        {
            bool edge = x == 2 || x == 13 || y == 1 || y == 18;
            bool band = y == 5 || y == 14;
            pixels[y * 16 + x] = edge || band ? new Color(0.92f, 0.94f, 0.97f) : new Color(0.23f, 0.3f, 0.38f);
            if (x >= 6 && x <= 9 && y >= 8 && y <= 11) pixels[y * 16 + x] = new Color(1f, 0.58f, 0.2f);
        }
        texture.SetPixels(pixels); texture.Apply();
        barrelSprite = Sprite.Create(texture, new Rect(0, 0, 16, 20), new Vector2(0.5f, 0.5f), 22f);
        return barrelSprite;
    }
}
