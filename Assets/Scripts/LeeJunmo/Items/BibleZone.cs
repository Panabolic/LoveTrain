using UnityEngine;

public class BloodyZone : MonoBehaviour
{
    [Header("컴포넌트")]
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D zoneCollider; // ✨ 충돌 체크용 콜라이더 참조

    // --- 내부 변수 (SO에서 받아옴) ---
    private ItemInstance parentItem;
    private float cooldownToApply;

    // ✨ SO에서 주입받을 변수들 (Inspector 노출 X)
    private float duration;
    private float buffAmount;
    private GameObject ownerUser; // 나를 소환한 플레이어

    private Gun buffedGun = null;
    private bool isBuffActive = false;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (zoneCollider == null) zoneCollider = GetComponent<Collider2D>();
    }

    // ✨ 초기화 함수 수정 (데이터 주입)
    public void Initialize(ItemInstance item, float cooldown, float duration, float buff, GameObject user)
    {
        this.parentItem = item;
        this.cooldownToApply = cooldown;
        this.duration = duration;
        this.buffAmount = buff;
        this.ownerUser = user; // 플레이어 참조 저장
    }

    private void Start()
    {
        animator.SetTrigger("Activate");
    }

    // ----------------------------------------------------------------
    // 애니메이션 이벤트
    // ----------------------------------------------------------------

    public void OnBuffStart()
    {
        if (isBuffActive) return;

        isBuffActive = true;

        // ✨ [핵심 수정] 버프가 켜지는 순간, 플레이어가 이미 안에 있는지 체크!
        // OnTriggerEnter2D는 '들어올 때'만 발동하므로, 이미 겹쳐있으면 발동 안 함.
        // 따라서 수동으로 체크해줍니다.
        CheckInstantOverlap();

        // 지속 시간 후 종료 예약
        Invoke(nameof(OnBuffDurationEnd), duration);
    }

    private void CheckInstantOverlap()
    {
        if (ownerUser == null || zoneCollider == null) return;

        Collider2D userCollider = ownerUser.GetComponent<Collider2D>();

        // 플레이어 콜라이더와 내 장판 콜라이더가 닿아있는지 확인
        if (userCollider != null && zoneCollider.IsTouching(userCollider))
        {
            ApplyBuff(ownerUser.GetComponent<Train>());
        }
    }

    private void OnBuffDurationEnd()
    {
        animator.SetBool("isOff", true);

        // 안전하게 버프 해제 (사라지기 전에 미리 해제)
        RemoveBuff();

        Destroy(gameObject, 1.0f);
    }

    // ----------------------------------------------------------------
    // 파괴 및 쿨타임
    // ----------------------------------------------------------------

    private void OnDestroy()
    {
        RemoveBuff();

        if (parentItem != null)
        {
            parentItem.StartCooldownManual(cooldownToApply);
        }
    }

    // ----------------------------------------------------------------
    // 충돌 및 버프 로직 (분리함)
    // ----------------------------------------------------------------

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isBuffActive) return;

        Train train = collision.GetComponent<Train>();
        if (train != null)
        {
            ApplyBuff(train);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!isBuffActive) return;

        Train train = collision.GetComponentInParent<Train>();
        if (train != null) RemoveBuff();
    }

    // ✨ 버프 적용 로직을 함수로 분리 (중복 제거)
    private void ApplyBuff(Train train)
    {
        if (train == null || buffedGun != null) return; // 이미 적용 중이면 패스

        Gun gun = train.GetComponentInChildren<Gun>();
        if (gun != null)
        {
            gun.AddFireRateMultiplier(buffAmount);
            buffedGun = gun;
            // Debug.Log($"[BloodyZone] 버프 적용: 공속 +{buffAmount * 100}%");
        }
    }

    private void RemoveBuff()
    {
        if (buffedGun != null)
        {
            buffedGun.AddFireRateMultiplier(-buffAmount);
            buffedGun = null;
            // Debug.Log("[BloodyZone] 버프 해제");
        }
    }
}