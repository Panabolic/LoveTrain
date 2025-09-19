using UnityEngine;
using UnityEngine.InputSystem; // <-- 이 네임스페이스 추가

public class GunRotator : MonoBehaviour
{
    void Update()
    {
        if (Mouse.current == null) return; // 마우스가 연결되지 않은 경우 처리

        // 새로운 입력 시스템으로 마우스 위치 가져오기
        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = 0;
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos);

        Vector3 direction = worldMousePos - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }
}
