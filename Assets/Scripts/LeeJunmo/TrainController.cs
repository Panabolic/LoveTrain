using UnityEngine;
using UnityEngine.InputSystem; // Input System을 코드에서 직접 사용하기 위해 필요

public class TrainController : MonoBehaviour
{
    public float CurrentSpeed { get; private set; }

    [Header("기차 이동 설정")]
    [SerializeField] private float trainMoveSpeed = 5f;
    [SerializeField] private float minXPosition = -8f;
    [SerializeField] private float maxXPosition = 8f;

    [Header("월드 속력 설정")]
    [SerializeField] public float minSpeed = 3f;
    [SerializeField] public float maxSpeed = 20f;

    [Header("디버그 정보")]
    [SerializeField] private float _currentSpeedForInspector;

    void Update()
    {
        // 1. 플레이어 입력에 따라 기차를 좌우로 직접 이동시킵니다.
        HandleMovement();
        // 2. 기차의 현재 X위치를 기반으로 월드 속력(CurrentSpeed)을 계산합니다.
        UpdateWorldSpeedByPosition();

        _currentSpeedForInspector = CurrentSpeed;
    }

    private void HandleMovement()
    {
        // ✨ [핵심 수정] 키보드 입력을 직접 확인합니다.
        float moveInput = 0f;

        // Keyboard.current가 null이 아닐 때만 (키보드가 연결되어 있을 때만) 입력을 받습니다.
        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed)
            {
                moveInput = 1f; // D키가 눌렸으면 오른쪽으로
            }
            else if (Keyboard.current.aKey.isPressed)
            {
                moveInput = -1f; // A키가 눌렸으면 왼쪽으로
            }
        }

        // 아래 이동 로직은 이전과 동일합니다.
        Vector3 movement = new Vector3(moveInput * trainMoveSpeed * Time.deltaTime, 0, 0);
        Vector3 newPosition = transform.position + movement;
        newPosition.x = Mathf.Clamp(newPosition.x, minXPosition, maxXPosition);
        transform.position = newPosition;
    }

    private void UpdateWorldSpeedByPosition()
    {
        float positionPercentage = Mathf.InverseLerp(minXPosition, maxXPosition, transform.position.x);
        CurrentSpeed = Mathf.Lerp(minSpeed, maxSpeed, positionPercentage);
    }
}