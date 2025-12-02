using UnityEngine;

public class BloodyZone : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("장판 지속 시간 (버프가 실제로 유지되는 시간)")]
    [SerializeField] private float duration = 5.0f;

    [Tooltip("공격 속도 증가량 (1.0 = 100%)")]
    [SerializeField] private float buffAmount = 1.0f;

    [Header("컴포넌트")]
    [SerializeField] private Animator animator;

    // --- 내부 변수 ---
    private Gun buffedGun = null;        // 버프 받은 총
    private bool isBuffActive = false;   // 현재 버프가 활성화 상태인가?

    // 쿨타임 제어용 변수
    private ItemInstance parentItem;     // 나를 소환한 아이템 인스턴스
    private float targetCooldown;        // 파괴 시 적용할 쿨타임

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    // ✨ 초기화: SO에서 생성 직후 호출
    public void Initialize(ItemInstance item, float cooldown)
    {
        this.parentItem = item;
        this.targetCooldown = cooldown;
    }

    // ----------------------------------------------------------------
    // ✨ 애니메이션 이벤트 (Animator Window에서 설정 필수)
    // ----------------------------------------------------------------

    // 1. OnBuffStart: 장판이 완전히 깔렸을 때 호출
    public void OnBuffStart()
    {
        if (isBuffActive) return;

        isBuffActive = true;
        Debug.Log("[BloodyZone] 애니메이션 이벤트: 버프 활성화 시작");

        // 설정된 지속 시간 후에 버프 해제 및 종료 예약
        Invoke(nameof(OnBuffDurationEnd), duration);
    }

    // 2. OnBuffDurationEnd: 지속 시간이 끝났을 때 (Invoke로 호출됨)
    private void OnBuffDurationEnd()
    {
        // 퇴장 애니메이션 실행 (isOff 트리거 -> 사라지는 모션)
        animator.SetBool("isOff", true);

        // 퇴장 애니메이션 시간 고려하여 잠시 후 파괴 (예: 1초 뒤)
        Destroy(gameObject, 1.0f);
    }

    // ----------------------------------------------------------------
    // ✨ 파괴 및 쿨타임 로직
    // ----------------------------------------------------------------

    private void OnDestroy()
    {
        // 1. 버프가 남아있다면 깔끔하게 해제
        RemoveBuff();

        // 2. ✨ 핵심: 장판이 사라질 때 아이템의 쿨타임을 시작시킴
        if (parentItem != null)
        {
            parentItem.StartCooldownManual(targetCooldown);
            Debug.Log($"[BloodyZone] 장판 파괴됨 -> 쿨타임 {targetCooldown}초 시작!");
        }
    }

    // ----------------------------------------------------------------
    // 충돌 처리 (기존 유지)
    // ----------------------------------------------------------------

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isBuffActive) return; // 아직 깔리는 중이면 무시

        Train train = collision.GetComponentInParent<Train>();
        if (train != null)
        {
            Gun gun = train.GetComponentInChildren<Gun>();
            if (gun != null && buffedGun == null)
            {
                gun.AddFireRateMultiplier(buffAmount);
                buffedGun = gun;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!isBuffActive) return;

        Train train = collision.GetComponentInParent<Train>();
        if (train != null)
        {
            RemoveBuff();
        }
    }

    private void RemoveBuff()
    {
        if (buffedGun != null)
        {
            buffedGun.AddFireRateMultiplier(-buffAmount);
            buffedGun = null;
            // 주의: isBuffActive는 여기서 끄지 않음 (다른 기차가 들어올 수 있으므로)
        }
    }
}