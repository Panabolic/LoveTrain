using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LaserGun", menuName = "Items/LaserGun")]
public class LaserGun_SO : Item_SO
{
    [Header("레이저 건 스탯")]
    public float[] damageByLevel = { 5f, 10f, 15f };
    public GameObject LaserProjectilePrefab;

    // (Item_SO의 instantiatedPrefab 필드를 무기 외형 프리팹으로 사용합니다)
    // (별도 필드 WeaponVisualPrefab을 만들어도 되지만, 기존 필드를 활용하면 깔끔합니다)

    [Tooltip("교체할 거치대 이미지 (선택)")]
    public Sprite HolderSprite;

    // ✨ [핵심] 부모의 기본 생성 로직을 덮어씌웁니다.
    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        // 1. 플레이어(기차)에서 Gun 컴포넌트 찾기
        Gun gun = user.GetComponentInChildren<Gun>();
        if (gun == null)
        {
            Debug.LogError($"[LaserGun_SO] {user.name}에서 Gun을 찾을 수 없습니다!");
            return null;
        }

        // 2. 부모의 InstantiateVisual() 대신, Gun에게 생성을 위임!
        // (instantiatedPrefab에는 'WeaponVisual'과 'LaserGun' 스크립트가 붙은 프리팹이 들어있어야 함)
        GameObject createdWeaponObj = gun.EquipVisual(this.instantiatedPrefab, this.HolderSprite);

        // 3. 생성된 무기에서 로직 스크립트(LaserGun) 초기화
        if (createdWeaponObj != null)
        {
            LaserGun logic = createdWeaponObj.GetComponent<LaserGun>();
            if (logic == null)
            {
                // 혹시 프리팹에 없으면 추가
                logic = createdWeaponObj.AddComponent<LaserGun>();
            }

            logic.Initialize(this, user);
            logic.UpgradeInstItem(instance);
        }

        // 4. 생성된 오브젝트를 리턴 (인벤토리 관리용)
        return createdWeaponObj;
    }

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, damageByLevel.Length - 1);
        return new Dictionary<string, string>
        {
            { "Damage", damageByLevel[index].ToString() }
        };
    }
}