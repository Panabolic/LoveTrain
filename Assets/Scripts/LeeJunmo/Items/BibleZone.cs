using UnityEngine;

public class BloodyZone : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("장판 지속 시간")]
    [SerializeField] private float duration = 5.0f;

    [Tooltip("공격 속도 증가량 (1.0 = 100%)")]
    [SerializeField] private float buffAmount = 1.0f;

    private Gun buffedGun = null; // 현재 버프를 받고 있는 총기 참조

    private void Start()
    {
        // 지속 시간 후 자동 파괴
        Destroy(gameObject, duration);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ✨ [핵심 수정] 태그 대신 Train 컴포넌트를 부모에서 찾음
        // (자식인 TrainF와 충돌해도 부모의 Train을 찾아냄)
        Train train = collision.GetComponentInParent<Train>();

        if (train != null)
        {
            // Train을 찾았다면, 그 자식들에 있는 Gun을 찾음
            Gun gun = train.GetComponentInChildren<Gun>();

            if (gun != null && buffedGun == null)
            {
                // 버프 적용
                gun.AddFireRateMultiplier(buffAmount);
                buffedGun = gun;
                Debug.Log("[BloodyZone] 공속 버프 적용!");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // 나갈 때도 동일하게 Train인지 확인
        Train train = collision.GetComponentInParent<Train>();

        if (train != null)
        {
            RemoveBuff();
        }
    }

    private void OnDestroy()
    {
        // 장판이 사라질 때 플레이어가 아직 안에 있다면 버프 해제
        RemoveBuff();
    }

    private void RemoveBuff()
    {
        if (buffedGun != null)
        {
            // 버프 해제 (음수 값 전달)
            buffedGun.AddFireRateMultiplier(-buffAmount);
            buffedGun = null;
            Debug.Log("[BloodyZone] 공속 버프 해제.");
        }
    }
}