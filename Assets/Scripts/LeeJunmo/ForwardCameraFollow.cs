using UnityEngine;

// The scene-authored parent moves; its child camera keeps the existing shake offset.
public sealed class ForwardCameraFollow : MonoBehaviour
{
    [SerializeField] private TrainController trainController;
    private float initialX;
    public float CurrentOffsetX => transform.position.x - initialX;

    private void Awake()
    {
        initialX = transform.position.x;
        if (trainController != null) trainController.SetCameraFollow(this);
    }

    private void LateUpdate()
    {
        if (trainController == null || GameManager.Instance == null || Time.timeScale <= 0f ||
            (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Boss)) return;
        Vector3 position = transform.position;
        float nextOffset = trainController.transform.position.x - trainController.MaxXPosition;
        position.x = Mathf.Max(position.x, initialX + nextOffset);
        transform.position = position;
    }
}
