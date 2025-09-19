using UnityEngine;
using UnityEngine.InputSystem; // <-- �� ���ӽ����̽� �߰�

public class GunRotator : MonoBehaviour
{
    void Update()
    {
        if (Mouse.current == null) return; // ���콺�� ������� ���� ��� ó��

        // ���ο� �Է� �ý������� ���콺 ��ġ ��������
        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = 0;
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos);

        Vector3 direction = worldMousePos - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }
}
