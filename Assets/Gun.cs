using UnityEngine;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    public GameObject bulletPrefab; // 발사할 탄환 프리팹
    public Transform firePoint;     // 탄환이 생성될 위치 (총구)

    // 새로운 입력 시스템 사용
    public InputAction shootAction;

    private void OnEnable()
    {
        shootAction.Enable();
    }

    private void OnDisable()
    {
        shootAction.Disable();
    }

    void Update()
    {
        // 마우스 왼쪽 버튼(shootAction)이 눌렸는지 확인
        if (shootAction.triggered)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        // 탄환 프리팹 인스턴스화 (생성)
        GameObject bulletInstance = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // 탄환 스크립트 가져오기
        Bullet bulletScript = bulletInstance.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            // 총의 방향을 탄환 발사 방향으로 사용
            bulletScript.Launch(transform.right);
        }
    }
}