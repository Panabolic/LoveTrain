using UnityEngine;
using UnityEngine.InputSystem;

public class GunRotator : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("실제로 회전시킬 총신(Pivot) 오브젝트를 연결하세요. 비워두면 이 오브젝트 자체가 회전합니다.")]
    [SerializeField] private Transform gunPivot;

    [Tooltip("스프라이트가 초기 상태에서 오른쪽을 보고 있다면 0, 위를 보고 있다면 -90 등을 입력")]
    [SerializeField] private float angleOffset = 0f;

    void Update()
    {
        if (Mouse.current == null || Time.timeScale == 0) return;

        // 1. 마우스 위치 가져오기 (World Point)
        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = 0; // 2D 게임이므로 Z값 0 고정
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos);
        worldMousePos.z = 0;

        // 2. 회전 대상 결정 (연결된 게 없으면 자기 자신 회전)
        Transform targetTransform = gunPivot != null ? gunPivot : transform;

        // 3. 방향 계산
        Vector3 direction = worldMousePos - targetTransform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 4. 회전 적용 (Offset 보정 포함)
        targetTransform.rotation = Quaternion.Euler(new Vector3(0, 0, angle + angleOffset));
    }
}