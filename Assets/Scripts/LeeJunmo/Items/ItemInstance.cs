// ItemInstance.cs
using UnityEngine;

[System.Serializable]
public class ItemInstance
{
    public Item_SO itemData;
    public int stackCount;
    public float currentCooldown;
    private GameObject instantiatedObject = null; // 실체화된 오브젝트 참조

    // --- 업그레이드를 위한 새 변수 ---
    public int currentUpgrade = 1; // 획득 시 기본 1레벨

    public ItemInstance(Item_SO data)
    {
        itemData = data;
        stackCount = 1;
        currentUpgrade = 1;

        // 생성 시, 1레벨에 맞는 쿨타임으로 초기화
        currentCooldown = itemData.GetCooldownForLevel(currentUpgrade);
    }

    public void HandleEquip(GameObject user)
    {
        // SO의 OnEquip을 호출하고, 그 결과를 저장
        instantiatedObject = itemData.OnEquip(user, this);
    }

    public void Tick(float deltaTime, GameObject user)
    {
        float maxCooldown = itemData.GetCooldownForLevel(currentUpgrade);

        if (maxCooldown > 0f) // 쿨타임이 있는 아이템만
        {
            currentCooldown -= deltaTime;
            if (currentCooldown <= 0f)
            {
                // 훅 호출 시 'this'(ItemInstance 자신)를 넘겨줍니다.
                itemData.OnCooldownComplete(user, this);

                // 쿨타임을 현재 레벨에 맞게 다시 초기화
                currentCooldown = maxCooldown;
            }
        }
    }

    /// <summary>
    /// 아이템을 업그레이드할 때 외부에서 호출할 함수
    /// </summary>
    public void UpgradeLevel()
    {
        // SO의 업그레이드 로직을 호출하여 책임을 위임
        itemData.UpgradeLevel(this);
    }


    public void instantiatedItemUpgrade()
    {
        // 1. 갱신할 프리팹(instantiatedObject)이 없으면 종료
        if (instantiatedObject == null) return;

        // 2. 실체화 로직 아이템인지 확인 (예: Revolver.cs)
        IInstantiatedItem logic = instantiatedObject.GetComponent<IInstantiatedItem>();

        if (logic != null)
        {
            // 3-A.  실체화 로직 아이템이면, 로직 스크립트에게 갱신을 위임
            logic.UpgradeInstItem(this);
        }
        else
        {
            // 3-B. "단순한" 아이템이면(패시브 비주얼), ItemInstance가 직접 갱신
            ApplyVisualUpgrade();
        }
    }


    /// <summary>
    /// [추가] "단순한" 비주얼 프리팹의 스프라이트/애니메이션을 갱신하는 헬퍼 함수
    /// </summary>
    private void ApplyVisualUpgrade()
    {
        int levelIndex = this.currentUpgrade - 1;
        if (levelIndex < 0 || itemData == null) return;

        // 우선순위 1: 애니메이터 컨트롤러 교체
        Animator animator = instantiatedObject.GetComponent<Animator>();
        if (animator != null && itemData.controllersByLevel != null && levelIndex < itemData.controllersByLevel.Length)
        {
            RuntimeAnimatorController newController = itemData.controllersByLevel[levelIndex];
            if (newController != null)
            {
                animator.runtimeAnimatorController = newController;
                return; // 컨트롤러 교체 성공
            }
        }

        // 우선순위 2: (애니메이터가 없거나 실패 시) 정적 스프라이트 교체
        SpriteRenderer spriteRenderer = instantiatedObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && itemData.spritesByLevel != null && levelIndex < itemData.spritesByLevel.Length)
        {
            Sprite newSprite = itemData.spritesByLevel[levelIndex];
            if (newSprite != null)
            {
                spriteRenderer.sprite = newSprite;
            }
        }
    }

}