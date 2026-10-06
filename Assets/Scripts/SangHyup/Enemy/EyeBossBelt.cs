using UnityEngine;

// Authored sprite copies share one Enemy/collider/HP owner on this root.
public sealed class EyeBossBelt : MonoBehaviour
{
    [Tooltip("Authored SpriteRenderer children: left 1, right 1, left 2, right 2.")]
    [SerializeField] private SpriteRenderer[] repeatTiles;

    private SpriteRenderer source;
    private PolygonCollider2D bodyCollider;
    private float tileWidth;
    private float scrollOffset;
    private bool initialized;
    private bool scrollingStarted;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (initialized) return;
        source = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<PolygonCollider2D>();
        if (source == null || source.sprite == null || bodyCollider == null || repeatTiles == null) return;
        float scaleX = Mathf.Abs(transform.lossyScale.x);
        if (scaleX <= 0f) return;
        tileWidth = source.sprite.bounds.size.x;
        if (tileWidth <= 0f) return;

        int originalPathCount = bodyCollider.pathCount;
        var originalPaths = new Vector2[originalPathCount][];
        for (int p = 0; p < originalPathCount; p++) originalPaths[p] = bodyCollider.GetPath(p);
        bodyCollider.pathCount = originalPathCount * (repeatTiles.Length + 1);
        for (int i = 0; i < repeatTiles.Length; i++)
        {
            float offset = TileOffset(i) * tileWidth;
            if (repeatTiles[i] != null)
                repeatTiles[i].transform.localPosition = new Vector3(offset, 0f, 0f);
            for (int p = 0; p < originalPathCount; p++)
            {
                var path = new Vector2[originalPaths[p].Length];
                for (int v = 0; v < path.Length; v++) path[v] = originalPaths[p][v] + Vector2.right * offset;
                bodyCollider.SetPath((i + 1) * originalPathCount + p, path);
            }
        }
        tileWidth *= scaleX;
        initialized = true;
        CopyPresentation();
    }

    private static int TileOffset(int index)
    {
        int distance = index / 2 + 1;
        return index % 2 == 0 ? -distance : distance;
    }

    public void ResetScroll()
    {
        scrollingStarted = false;
        if (bodyCollider != null) bodyCollider.enabled = true;
    }

    public void Advance(float deltaTime, float trainSpeed, Camera camera)
    {
        Initialize();
        if (!initialized || camera == null || deltaTime <= 0f) return;
        if (!scrollingStarted)
        {
            scrollOffset = transform.position.x - camera.transform.position.x;
            scrollingStarted = true;
        }
        scrollOffset = Mathf.Repeat(scrollOffset - Mathf.Max(0f, trainSpeed) / 10f * deltaTime + tileWidth * 0.5f,
            tileWidth) - tileWidth * 0.5f;
        Vector3 position = transform.position;
        position.x = camera.transform.position.x + scrollOffset;
        transform.position = position;
    }

    private void LateUpdate() => CopyPresentation();

    private void CopyPresentation()
    {
        if (source == null || repeatTiles == null) return;
        for (int i = 0; i < repeatTiles.Length; i++)
        {
            SpriteRenderer tile = repeatTiles[i];
            if (tile == null) continue;
            tile.sprite = source.sprite;
            tile.sharedMaterial = source.sharedMaterial;
            tile.color = source.color;
            tile.flipX = source.flipX;
            tile.flipY = source.flipY;
            tile.sortingLayerID = source.sortingLayerID;
            tile.sortingOrder = source.sortingOrder;
            tile.enabled = source.enabled;
        }
    }

    public void Hide()
    {
        if (source != null) source.enabled = false;
        if (bodyCollider != null) bodyCollider.enabled = false;
        if (repeatTiles == null) return;
        for (int i = 0; i < repeatTiles.Length; i++)
            if (repeatTiles[i] != null) repeatTiles[i].enabled = false;
    }
}
