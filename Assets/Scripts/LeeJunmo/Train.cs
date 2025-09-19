using UnityEngine;
using UnityEngine.InputSystem; // 이 줄을 추가하세요!

public class Train : MonoBehaviour
{
    public float moveSpeed = 5.0f;

    void Update()
    {
        // Keyboard.current는 현재 연결된 키보드를 의미합니다.
        // 'a' 키가 눌리고 있는지 확인합니다.
        if (Keyboard.current.aKey.isPressed)
        {
            transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
        }

        // 'd' 키가 눌리고 있는지 확인합니다.
        if (Keyboard.current.dKey.isPressed)
        {
            transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
        }
    }
}