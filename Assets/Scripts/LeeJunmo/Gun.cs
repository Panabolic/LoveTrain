using UnityEngine;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    public GameObject bulletPrefab; // 발사할 탄환 프리팹
    public Transform firePoint;     // 탄환이 생성될 위치 (총구)

    // 새로운 입력 시스템 사용
    public InputAction shootAction;

    [Header("연사 설정")]
    [Tooltip("총알 사이의 발사 간격 (초). 0.2 = 초당 5발")]
    [SerializeField] private float fireRate = 0.2f;

    // ✨ [추가] 다음에 발사 가능한 시간을 저장할 변수
    private float nextFireTime = 0f;

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
        // ✨ [핵심 수정]
        // 1. .triggered (클릭 시) -> .IsPressed() (누르고 있는 동안)
        // 2. Time.time >= nextFireTime (쿨타임이 지났는지 확인)
        if (shootAction.IsPressed() && Time.time >= nextFireTime)
        {
            // ✨ [추가] 다음 발사 시간을 현재 시간 + 쿨타임으로 갱신
            nextFireTime = Time.time + fireRate;

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