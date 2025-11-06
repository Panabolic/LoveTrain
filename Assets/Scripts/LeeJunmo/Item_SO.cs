using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class Item_SO : ScriptableObject
{
    [Header("아이템 이름")]
    public string itemName;
    [Header("태그")]
    public ItemTag tags;
    [Header("아이콘")]
    public Sprite iconSprite;
    [Header("열차 비쥬얼")]
    public Sprite trainSprite;
    [Header("업그레이드 최대 횟수")]
    public int MaxUpgrade;
    [Header("아이템 설명")]
    public string itemScript;
    [Header("쿨타임 설정")]
    public float cooldownTime = 0f; // 이 아이템의 쿨타임 (0이면 쿨타임 없음)

    // --- 이벤트 훅 (Event Hooks) ---
    // 자식 클래스들이 이 중에서 필요한 것만 골라 재정의(override)합니다.

    /// <summary>
    /// 2. 이 아이템을 장착(획득)했을 때 한 번 호출됩니다. (예: 스탯 영구 증가)
    /// </summary>
    public virtual void OnEquip(GameObject user) { }

    /// 4. 소유자가 피해를 '받았을' 때 호출됩니다. (예: 가시 갑옷)
    /// </summary>
    public virtual void OnTakeDamage(GameObject user, GameObject attacker) { }

    /// <summary>
    /// 5. 소유자가 피해를 '입혔을' 때 호출됩니다. (예: 흡혈 무기)
    /// </summary>
    public virtual void OnDealDamage(GameObject user, GameObject target) { }

    /// <summary>
    /// 6. 소유자가 적을 처치했을 때 호출됩니다. (예: 영혼 흡수)
    /// </summary>
    public virtual void OnKillEnemy(GameObject user, GameObject killedEnemy) { }

    /// <summary>
    /// 8. 설정된 쿨타임이 완료될 때마다 호출됩니다.
    /// </summary>
    public virtual void OnCooldownComplete(GameObject user,ItemInstance instance) { }

    /// <summary>
    /// ItemInstance가 자신의 현재 레벨에 맞는 쿨타임을 가져갈 수 있게 함
    /// </summary>
    public virtual float GetCooldownForLevel(int level)
    {
        return 0f; // 쿨타임 없는 아이템을 위한 기본값
    }
}