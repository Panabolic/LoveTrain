using UnityEngine;

public class BloodyZone : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("장판 지속 시간 (버프 실제 유지 시간)")]
    [SerializeField] private float duration = 5.0f;

    [Tooltip("공격 속도 증가량 (1.0 = 100%)")]
    [SerializeField] private float buffAmount = 1.0f;

    [Header("컴포넌트")]
    [SerializeField] private Animator animator;

    // --- 내부 변수 ---
    private ItemInstance parentItem; // 나를 만든 아이템
    private float cooldownToApply;   // 적용할 쿨타임
    private Gun buffedGun = null;
    private bool isBuffActive = false;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    // ✨ SO에서 생성 직후 호출해주는 초기화 함수
    public void Initialize(ItemInstance item, float cooldown)
    {
        this.parentItem = item;
        this.cooldownToApply = cooldown;
    }

    private void Start()
    {
        // 생성되자마자 등장 애니메이션
        animator.SetTrigger("Activate");
    }

    // ----------------------------------------------------------------
    // ✨ 애니메이션 이벤트 (Animation Window에서 추가 필요)
    // ----------------------------------------------------------------

    // 1. 장판이 깔리는 타이밍에 호출
    public void OnBuffStart()
    {
        if (isBuffActive) return;

        isBuffActive = true;
        // Debug.Log("[BloodyZone] 버프 활성화");

        // 설정된 지속 시간 후에 종료 함수 예약
        Invoke(nameof(OnBuffDurationEnd), duration);
    }

    // 2. 지속 시간이 끝났을 때 (Invoke로 호출됨)
    private void OnBuffDurationEnd()
    {
        // 퇴장 애니메이션 트리거
        animator.SetBool("isOff", true);

        // 애니메이션이 끝날 즈음 파괴 (예: 1초 뒤)
        Destroy(gameObject, 1.0f);
    }

    // (만약 애니메이션 끝나는 프레임에 이벤트를 심었다면 거기서 Destroy 호출해도 됨)
    public void OnBuffEnd()
    {
        // 애니메이션 이벤트로 종료 처리할 경우 사용
    }

    // ----------------------------------------------------------------
    // ✨ 파괴 및 쿨타임 시작 로직
    // ----------------------------------------------------------------

    private void OnDestroy()
    {
        // 버프 해제
        RemoveBuff();

        // ✨ 핵심: 장판이 사라질 때 아이템의 쿨타임을 수동으로 시작시킴
        if (parentItem != null)
        {
            parentItem.StartCooldownManual(cooldownToApply);
            // Debug.Log($"[BloodyZone] 장판 파괴 -> 쿨타임 {cooldownToApply}초 시작");
        }
    }

    // ----------------------------------------------------------------
    // 충돌 처리
    // ----------------------------------------------------------------

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isBuffActive) return;

        Train train = collision.GetComponent<Train>();
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
        if (train != null) RemoveBuff();
    }

    private void RemoveBuff()
    {
        if (buffedGun != null)
        {
            buffedGun.AddFireRateMultiplier(-buffAmount);
            buffedGun = null;
        }
    }
}