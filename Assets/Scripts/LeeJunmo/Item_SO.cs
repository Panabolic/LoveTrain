using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class Item_SO : ScriptableObject
{
    [Header("아이템 이름")]
    public string itemName;
    [Header("부착 오브젝트 설정")]
    [Tooltip("장착 시 씬에 생성될 프리팹 (시각 전용 또는 로직 포함)")]
    public GameObject instantiatedPrefab;
    [Header("부착 위치 설정")]
    [Tooltip("아이템이 부착될 소켓 이름 (예: WeaponSocket). 비워두면 루트에 부착됨.")]
    public string attachmentSocketName; // <-- [추가]
    [Header("아이콘")]
    public Sprite iconSprite;
    [Header("비주얼 업그레이드 (공통)")]
    [Tooltip("레벨별로 교체될 '정적' 스프라이트 (Animator가 없을 경우)")]
    public Sprite[] spritesByLevel;
    [Tooltip("레벨별로 교체될 '애니메이터 컨트롤러' (Animator가 있을 경우)")]
    public RuntimeAnimatorController[] controllersByLevel;
    [Header("업그레이드 최대 횟수")]
    public int MaxUpgrade;
    [Header("아이템 설명")]
    [TextArea(3, 10)]
    public string itemScript;
    [Header("아이템 설명")]
    [TextArea(3, 10)]
    public string itemSimpleScript;
    [Header("쿨타임 설정")]
    public float cooldownTime = 0f; // 이 아이템의 쿨타임 (0이면 쿨타임 없음)
    /// <summary>
    /// ✨ [추가됨] true일 경우 시스템이 쿨타임을 자동 초기화하지 않음.
    /// (BloodyBible 처럼 장판이 사라질 때 수동으로 쿨타임을 돌려야 하는 경우 사용)
    /// 기본값: false (기존 아이템들과 동일하게 자동 쿨타임)
    /// </summary>
    public virtual bool IsManualCooldown => false;
    // --- 이벤트 훅 (Event Hooks) ---
    // 자식 클래스들이 이 중에서 필요한 것만 골라 재정의(override)합니다.

    /// <summary>
    /// 1. 이 함수는 이제 자식들이 '반드시' 구현(override)해야 할 수 있습니다.
    /// (혹은 기본 로직을 여기에 넣어도 됩니다. 아래 헬퍼 방식 참조)
    /// </summary>
    public virtual GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        // '박동하는 심장' 같은 아이템은 이 헬퍼 함수를 호출합니다.
        return InstantiateVisual(user);
    }

    /// <summary>
    /// [수정] 자식들이 공통으로 사용할 '프리팹 실체화' 헬퍼 함수
    /// </summary>
    protected GameObject InstantiateVisual(GameObject user)
    {
        // 1. 프리팹이 없으면 아무것도 안 함 (비주얼이 없는 아이템)
        if (instantiatedPrefab == null)
        {
            return null;
        }

        // 2. 부착될 소켓 찾기 (기본값 = user 루트)
        Transform parentTransform = user.transform;
        if (!string.IsNullOrEmpty(attachmentSocketName))
        {
            Transform socket = FindChildSocket(user.transform, attachmentSocketName);
            if (socket != null)
            {
                parentTransform = socket;
            }
            else
            {
                Debug.LogWarning($"[ItemSO] {user.name}에서 '{attachmentSocketName}' 소켓을 못찾음.");
            }
        }

        // 3. 소켓에 프리팹 생성 및 위치 초기화
        GameObject itemGO = Instantiate(instantiatedPrefab, parentTransform);
        itemGO.transform.localPosition = Vector3.zero;
        itemGO.transform.localRotation = Quaternion.identity;

        return itemGO;
    }

    // 4. 재귀적으로 소켓을 찾는 헬퍼 함수 (추가)
    private Transform FindChildSocket(Transform parent, string socketName)
    {
        Transform socket = parent.Find(socketName);
        if (socket != null) return socket;

        foreach (Transform child in parent)
        {
            socket = FindChildSocket(child, socketName);
            if (socket != null) return socket;
        }
        return null;
    }


    /// </summary>
    /// 2. 소유자가 피해를 '받았을' 때 호출됩니다. (예: 가시 갑옷)
    /// </summary>
    public virtual void OnTakeDamage(GameObject user, GameObject attacker) { }

    /// <summary>
    /// 3. 소유자가 피해를 '입혔을' 때 호출됩니다.
    /// ✨ [수정] ItemInstance 파라미터 추가! (이제 레벨 정보를 알 수 있음)
    /// </summary>
    public virtual void OnDealDamage(GameObject user, GameObject target, GameObject source, ItemInstance instance) { }

    /// <summary>
    /// 4. 소유자가 적을 처치했을 때 호출됩니다. (예: 영혼 흡수)
    /// </summary>
    public virtual void OnKillEnemy(GameObject user, GameObject killedEnemy) { }

    /// <summary>
    /// 5. 설정된 쿨타임이 완료될 때마다 호출됩니다.
    /// </summary>
    public virtual void OnCooldownComplete(GameObject user,ItemInstance instance) { }

    /// <summary>
    /// 6.ItemInstance가 자신의 현재 레벨에 맞는 쿨타임을 가져갈 수 있게 함
    /// </summary>
    public virtual float GetCooldownForLevel(int level)
    {
        return 0f; // 쿨타임 없는 아이템을 위한 기본값
    }

    /// <summary>
    /// 7.ItemInstance의 RequestUpgrade()에 의해 호출됩니다.
    /// </summary>
    public virtual void UpgradeLevel(ItemInstance instance)
    {
        // "일반적인" 업그레이드 로직 (레벨 1 증가)
        instance.currentUpgrade++;

        // 실체화된 아이템이 있다면 동기화하도록 알림
        instance.instantiatedItemUpgrade();
    }

    /// <summary>
    /// 자식 아이템이 자신의 스탯을 딕셔너리로 반환 (Override용)
    /// </summary>
    protected virtual Dictionary<string, string> GetStatReplacements(int level)
    {
        return new Dictionary<string, string>();
    }

    /// <summary>
    /// UI가 호출: 템플릿의 {Tag}를 실제 값으로 변환하여 반환
    /// </summary>
    public string GetFormattedDescription(int level)
    {
        string desc = itemScript;
        if (string.IsNullOrEmpty(desc)) return "";

        var replacements = GetStatReplacements(level);
        foreach (var pair in replacements)
        {
            desc = desc.Replace("{" + pair.Key + "}", pair.Value);
        }
        return desc;
    }
}