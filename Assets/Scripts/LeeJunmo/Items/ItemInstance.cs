using UnityEngine;

[System.Serializable]
public class ItemInstance
{
    public Item_SO itemData;
    public float currentCooldown;
    public float maxCooldown;
    private GameObject instantiatedObject = null; // 실체화된 오브젝트

    public int currentUpgrade = 1;

    public ItemInstance(Item_SO data)
    {
        itemData = data;
        currentUpgrade = 1;
        // 처음 생성 시 쿨타임 적용 (게임 시작 시 바로 발사 안 되게 하려면 값 유지, 바로 쏘려면 0)
        currentCooldown = 0f;
    }

    public void HandleEquip(GameObject user)
    {
        instantiatedObject = itemData.OnEquip(user, this);
    }

    // ✨ [수정됨] 쿨타임 로직
    public void Tick(float deltaTime, GameObject user)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Boss
            && GameManager.Instance.CurrentState != GameState.Ending) return;

        // 쿨타임이 없는 아이템(패시브 등)은 무시
        float levelMaxCooldown = itemData.GetCooldownForLevel(currentUpgrade);
        if (levelMaxCooldown <= 0f) return;

        // 1. 수동 모드 대기 상태(float.MaxValue)가 아니라면 시간 감소
        // (float.MaxValue인 경우는 장판이 깔려있는 상태이므로 시간을 줄이지 않음)
        if (currentCooldown < float.MaxValue && currentCooldown > 0f)
        {
            currentCooldown -= deltaTime;
        }

        // 2. ✨ [수정] 쿨타임 감소 후(혹은 처음부터 0일 때) 즉시 체크
        if (currentCooldown <= 0f)
        {
            // 쿨타임 보정 (음수로 내려가는 것 방지)
            currentCooldown = 0f;

            // A. 효과 발동 (장판 생성, 데미지 처리 등)
            itemData.OnCooldownComplete(user, this);

            // B. 쿨타임 재설정 분기
            if (itemData.IsManualCooldown)
            {
                // [수동 모드] (예: 성서)
                // 장판이 사라질 때까지 대기하기 위해 무한대 값으로 설정
                currentCooldown = float.MaxValue;
            }
            else
            {
                // [자동 모드] (예: 심장)
                // 즉시 다음 쿨타임 적용
                currentCooldown = levelMaxCooldown;
            }
        }
    }

    // ✨ [추가됨] 외부(장판)에서 호출하여 쿨타임을 강제로 시작시키는 메서드
    public void StartCooldownManual(float cooldownTime)
    {
        this.maxCooldown = cooldownTime;
        this.currentCooldown = cooldownTime; // 여기서 값을 설정하면 Tick이 다시 돌기 시작함
        // Debug.Log($"[ItemInstance] 수동 쿨타임 시작: {cooldownTime}초");
    }

    public void UpgradeLevel()
    {
        itemData.UpgradeLevel(this);
    }

    public void instantiatedItemUpgrade()
    {
        if (instantiatedObject == null) return;

        IInstantiatedItem logic = instantiatedObject.GetComponent<IInstantiatedItem>();
        if (logic != null) logic.UpgradeInstItem(this);
        else ApplyVisualUpgrade();
    }

    private void ApplyVisualUpgrade()
    {
        int levelIndex = this.currentUpgrade - 1;
        if (levelIndex < 0 || itemData == null) return;

        // 애니메이터 교체 시도
        Animator animator = instantiatedObject.GetComponent<Animator>();
        if (animator != null && itemData.controllersByLevel != null && levelIndex < itemData.controllersByLevel.Length)
        {
            RuntimeAnimatorController newController = itemData.controllersByLevel[levelIndex];
            if (newController != null) { animator.runtimeAnimatorController = newController; return; }
        }

        // 스프라이트 교체 시도
        SpriteRenderer spriteRenderer = instantiatedObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && itemData.spritesByLevel != null && levelIndex < itemData.spritesByLevel.Length)
        {
            Sprite newSprite = itemData.spritesByLevel[levelIndex];
            if (newSprite != null) spriteRenderer.sprite = newSprite;
        }
    }
}