using UnityEngine;

public class AutoScrollBackground : MonoBehaviour
{
    [Tooltip("속도 정보를 가져올 TrainController (자동으로 찾습니다)")]
    public Train train;

    [Tooltip("카메라를 기준으로 배경 위치를 재설정합니다.")]
    public Transform cameraTransform;

    [Tooltip("관리할 배경 레이어들을 등록해주세요.")]
    public ParallaxLayer[] layers;

    private bool isScrolling = false;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        if (cameraTransform == null) { enabled = false; return; }

        if (train == null)
        {
            train = FindFirstObjectByType<Train>();
            if (train == null)
            {
                Debug.LogError("[AutoScrollBackground] Train을 찾을 수 없습니다!");
                this.enabled = false;
                return;
            }
        }

        EnsureViewportCoverage();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChange;
            HandleGameStateChange(GameManager.Instance.CurrentState);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChange;
    }

    private void HandleGameStateChange(GameState newState)
    {
        // ✨ [수정] StageTransition 상태는 제외 (StageManager가 수동으로 끌 예정)
        if (newState == GameState.Start || newState == GameState.Event || newState == GameState.Die || newState == GameState.Pause)
        {
            isScrolling = false;
        }
        else
        {
            isScrolling = true; // Playing, Boss, StageTransition 등에서는 일단 켬
        }
    }

    // ✨ [추가] 외부에서 스크롤을 강제로 멈추기 위한 함수
    public void SetScrolling(bool active)
    {
        isScrolling = active;
    }

    void Update()
    {
        if (!isScrolling || train == null) return;

        float currentTrainSpeed = train.CurrentSpeed;

        // 1. 레이어 이동 (Parallax)
        foreach (ParallaxLayer layer in layers)
        {
            if (Mathf.Approximately(currentTrainSpeed, 0)) continue;

            float movement = (currentTrainSpeed / 10f) * layer.parallaxFactor * Time.deltaTime;
            layer.layerTransform.position -= new Vector3(movement, 0, 0);
        }

        // Recycle only tiles wholly outside the viewport; camera-center recycling exposes gaps when zoomed out.
        float halfWidth = Camera.main != null ? Camera.main.orthographicSize * Camera.main.aspect : 0f;
        float leftEdge = cameraTransform.position.x - halfWidth;
        float rightEdge = cameraTransform.position.x + halfWidth;
        foreach (ParallaxLayer layer in layers)
        {
            if (layer.layerTransform == null || layer.layerTransform.childCount < 2) continue;
            int limit = layer.layerTransform.childCount;
            for (int i = 0; i < limit; i++)
            {
                Transform first = layer.layerTransform.GetChild(0);
                Transform last = layer.layerTransform.GetChild(layer.layerTransform.childCount - 1);
                var firstSprite = first.GetComponent<SpriteRenderer>();
                var lastSprite = last.GetComponent<SpriteRenderer>();
                if (firstSprite == null || lastSprite == null) break;
                if (currentTrainSpeed > 0f && firstSprite.bounds.max.x < leftEdge)
                {
                    first.position += Vector3.right * (lastSprite.bounds.max.x - firstSprite.bounds.min.x);
                    first.SetAsLastSibling();
                }
                else if (currentTrainSpeed < 0f && lastSprite.bounds.min.x > rightEdge)
                {
                    last.position += Vector3.right * (firstSprite.bounds.min.x - lastSprite.bounds.max.x);
                    last.SetAsFirstSibling();
                }
                else break;
            }
        }
    }

    private void EnsureViewportCoverage()
    {
        if (Camera.main == null || layers == null) return;
        float viewWidth = Camera.main.orthographicSize * Camera.main.aspect * 2f;
        var visited = new System.Collections.Generic.HashSet<Transform>();
        foreach (var layer in layers)
        {
            Transform root = layer.layerTransform;
            if (root == null || root.childCount == 0 || !visited.Add(root)) continue;
            var first = root.GetChild(0).GetComponent<SpriteRenderer>();
            if (first == null || first.bounds.size.x <= 0f) continue;
            // Keep authored tile artwork and vertical offsets, but make horizontal seams contiguous.
            for (int i = 1; i < root.childCount; i++)
            {
                var previous = root.GetChild(i - 1).GetComponent<SpriteRenderer>();
                var current = root.GetChild(i).GetComponent<SpriteRenderer>();
                if (previous == null || current == null) continue;
                current.transform.position += Vector3.right * (previous.bounds.max.x - current.bounds.min.x);
            }
            float leftEdge = cameraTransform.position.x - viewWidth * 0.5f;
            float rightEdge = cameraTransform.position.x + viewWidth * 0.5f;
            // Move an unused right tile to the left before the first rendered frame, when needed.
            for (int i = 0; i < root.childCount; i++)
            {
                var left = root.GetChild(0).GetComponent<SpriteRenderer>();
                var right = root.GetChild(root.childCount - 1).GetComponent<SpriteRenderer>();
                if (left == null || right == null || left.bounds.min.x <= leftEdge || right.bounds.min.x <= rightEdge) break;
                right.transform.position += Vector3.right * (left.bounds.min.x - right.bounds.max.x);
                right.transform.SetAsFirstSibling();
            }
            int count = Mathf.Max(2, Mathf.CeilToInt(viewWidth / first.bounds.size.x) + 1);
            // Authored tiles are already contiguous. Append enough tiles to cover the larger viewport while recycling.
            while (root.childCount < count)
            {
                var left = root.GetChild(0).GetComponent<SpriteRenderer>();
                bool prepend = left != null && left.bounds.min.x > leftEdge;
                Transform source = prepend ? root.GetChild(0) : root.GetChild(root.childCount - 1);
                var sourceSprite = source.GetComponent<SpriteRenderer>();
                if (sourceSprite == null) break;
                GameObject extra = Instantiate(source.gameObject, root);
                var extraSprite = extra.GetComponent<SpriteRenderer>();
                if (prepend)
                {
                    extra.transform.position += Vector3.right * (sourceSprite.bounds.min.x - extraSprite.bounds.max.x);
                    extra.transform.SetAsFirstSibling();
                }
                else
                {
                    extra.transform.position += Vector3.right * (sourceSprite.bounds.max.x - extraSprite.bounds.min.x);
                    extra.transform.SetAsLastSibling();
                }
            }
        }
    }
}
