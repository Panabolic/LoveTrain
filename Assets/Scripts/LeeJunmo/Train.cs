using UnityEngine;
using UnityEngine.InputSystem; // �� ���� �߰��ϼ���!

public class Train : MonoBehaviour
{
    public float moveSpeed = 5.0f;

    void Update()
    {
        // Keyboard.current�� ���� ����� Ű���带 �ǹ��մϴ�.
        // 'a' Ű�� ������ �ִ��� Ȯ���մϴ�.
        if (Keyboard.current.aKey.isPressed)
        {
            transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
        }

        // 'd' Ű�� ������ �ִ��� Ȯ���մϴ�.
        if (Keyboard.current.dKey.isPressed)
        {
            transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
        }
    }
}