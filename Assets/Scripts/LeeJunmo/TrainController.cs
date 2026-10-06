using UnityEngine;
using UnityEngine.InputSystem;

public class TrainController : MonoBehaviour
{
    [Header("기차 이동 설정")]
    [SerializeField] private float trainMoveSpeed = 5f; // A/D로 좌우 이동하는 속도
    [SerializeField] private float minXPosition = -8f;
    [SerializeField] private float maxXPosition = 8f;
    [SerializeField] private ForwardCameraFollow cameraFollow;

    // --- 외부 공개 속성 ---
    // 몬스터가 참조할 수 있도록 Min/Max X Position을 public으로 공개
    public float CurrentCameraOffsetX => cameraFollow != null ? cameraFollow.CurrentOffsetX : 0f;
    public float MinXPosition => minXPosition + CurrentCameraOffsetX;
    public float MaxXPosition => maxXPosition;

    public void SetCameraFollow(ForwardCameraFollow follow) { cameraFollow = follow; }

    public void PushLeft(float distance)
    {
        if (distance <= 0f) return;
        Vector3 position = transform.position;
        position.x = Mathf.Max(MinXPosition, position.x - distance);
        transform.position = position;
    }


    void Update()
    {
        if (GameManager.Instance == null || Time.timeScale <= 0f ||
            (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Boss))
        {
            return;
        }
        // 1. 플레이어 입력에 따라 기차를 좌우로 직접 이동
        HandleMovement();
    }

    /// <summary>
    /// A/D 키 입력에 따라 transform.position.x 를 직접 조작합니다.
    /// </summary>
    private void HandleMovement()
    {
        float moveInput = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed)
            {
                moveInput = 1f; // D키 = 오른쪽
            }
            else if (Keyboard.current.aKey.isPressed)
            {
                moveInput = -1f; // A키 = 왼쪽
            }
        }

        // 위치 이동 로직
        Vector3 movement = new Vector3(moveInput * trainMoveSpeed * Time.deltaTime, 0, 0);
        Vector3 newPosition = transform.position + movement;

        // 위치 제한
        newPosition.x = Mathf.Max(newPosition.x, MinXPosition);
        transform.position = newPosition;
    }
}
