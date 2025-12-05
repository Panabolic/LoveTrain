using UnityEngine;

public class AutoScrollBackground : MonoBehaviour
{
    [Tooltip("속도 정보를 가져올 TrainController (자동으로 찾습니다)")]
    public Train train;

    [Tooltip("카메라를 기준으로 배경 위치를 재설정합니다.")]
    public Transform cameraTransform;

    [Tooltip("관리할 배경 레이어들을 등록해주세요.")]
    public ParallaxLayer[] layers;

    private float spriteWidth;
    private bool isScrolling = false;

    void Start()
    {
        if (cameraTransform == null) cameraTransform = Camera.main.transform;

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
        if (newState == GameState.Start || newState == GameState.Event || newState == GameState.Die)
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

        // 2. 무한 스크롤 재배치
        foreach (ParallaxLayer layer in layers)
        {
            if (layer.layerTransform.childCount < 2) continue;

            spriteWidth = layer.layerTransform.GetChild(0).GetComponent<SpriteRenderer>().bounds.size.x;

            Transform leftChild = layer.layerTransform.GetChild(0);
            Transform rightChild = layer.layerTransform.GetChild(1);

            if (currentTrainSpeed > 0 && cameraTransform.position.x > rightChild.position.x)
            {
                leftChild.position = new Vector3(rightChild.position.x + spriteWidth, leftChild.position.y, leftChild.position.z);
                leftChild.SetAsLastSibling();
            }
            else if (currentTrainSpeed < 0 && cameraTransform.position.x < leftChild.position.x)
            {
                rightChild.position = new Vector3(leftChild.position.x - spriteWidth, rightChild.position.y, rightChild.position.z);
                rightChild.SetAsFirstSibling();
            }
        }
    }
}