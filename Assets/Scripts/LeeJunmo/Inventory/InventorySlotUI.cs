using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Image를 사용하기 위해 필요

// (파일 이름과 클래스 이름이 'InventorySlotUI'로 동일해야 합니다)
public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI 구성요소")]
    // [SerializeField]를 제거하고 private으로 변경
    private Image itemIcon;  // 아이템 아이콘 (첫 번째 자식)
    private Image levelIcon; // 레벨 아이콘 (두 번째 자식)
    private Image cooldownFill;

    [Header("데이터 참조")]
    // [1단계]에서 만든 LevelSpriteAtlas 에셋을 여기에 연결
    [SerializeField] private LevelSpriteAtlas levelAtlas;

    private ItemInstance currentInstance;

    /// <summary>
    /// 스크립트가 활성화될 때 자동으로 컴포넌트를 찾습니다.
    /// </summary>
    private void Awake()
    {
        itemIcon = FindChildImage("ItemIcon") ?? GetChildImage(0);
        levelIcon = FindChildImage("Level") ?? GetChildImage(1);
        cooldownFill = FindChildImage("CoolDownFill") ?? GetChildImage(2);
        SetCooldownFill(0f);

        // 3. (오류 방지) 혹시나 못 찾았을 경우를 대비해 경고
        if (itemIcon == null)
        {
            Debug.LogError($"[InventorySlotUI] '{gameObject.name}'의 첫 번째 자식에서 'itemIcon' (Image)을 찾지 못했습니다.");
        }
        if (levelIcon == null)
        {
            Debug.LogError($"[InventorySlotUI] '{gameObject.name}'의 두 번째 자식에서 'levelIcon' (Image)을 찾지 못했습니다.");
        }
    }

    /// <summary>
    /// 이 슬롯을 특정 아이템 인스턴스로 갱신합니다.
    /// </summary>
    public void UpdateSlot(ItemInstance instance)
    {
        this.currentInstance = instance;

        // (안전 장치) Awake에서 아이콘을 못 찾았으면 오류 방지
        if (itemIcon == null || levelIcon == null)
        {
            RefreshCooldownFill();
            return;
        }

        // 1. '빈 슬롯' 처리
        if (instance == null)
        {
            itemIcon.enabled = false;
            levelIcon.enabled = false;
            RefreshCooldownFill();
            return;
        }

        // 2. '아이템 아이콘' 갱신
        itemIcon.sprite = instance.itemData.iconSprite;
        itemIcon.enabled = true;

        // 3. '레벨 스프라이트' 갱신
        Sprite levelSprite;

        // 3a. 최대 레벨인지 확인
        if (instance.currentUpgrade >= instance.itemData.MaxUpgrade)
        {
            levelSprite = levelAtlas.maxLevelSprite;
        }
        else
        {
            // 3b. 일반 레벨
            levelSprite = levelAtlas.GetSpriteForLevel(instance.currentUpgrade);
        }

        if (levelSprite != null)
        {
            levelIcon.sprite = levelSprite;
            levelIcon.enabled = true;
        }
        else
        {
            levelIcon.enabled = false;
        }

        RefreshCooldownFill();
    }

    private void Update()
    {
        RefreshCooldownFill();
    }

    private Image GetChildImage(int childIndex)
    {
        if (transform.childCount <= childIndex)
        {
            return null;
        }

        return transform.GetChild(childIndex).GetComponent<Image>();
    }

    private Image FindChildImage(string childName)
    {
        Transform child = FindDescendant(transform, childName);
        if (child == null)
        {
            return null;
        }

        return child.GetComponent<Image>();
    }

    private Transform FindDescendant(Transform root, string childName)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform descendant = FindDescendant(child, childName);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }

    private void RefreshCooldownFill()
    {
        float fillAmount = currentInstance != null ? currentInstance.GetCooldownFillAmount() : 0f;
        SetCooldownFill(fillAmount);
    }

    private void SetCooldownFill(float fillAmount)
    {
        if (cooldownFill == null)
        {
            return;
        }

        cooldownFill.fillAmount = Mathf.Clamp01(fillAmount);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentInstance == null) return;

        Item_SO so = currentInstance.itemData;
        int level = currentInstance.currentUpgrade;

        // ... (제목, 레벨 스프라이트, 설명 가져오는 로직은 그대로) ...
        string title = so.LocalizedName;
        Sprite levelSprite = (level >= so.MaxUpgrade) ? levelAtlas.maxLevelSprite : levelAtlas.GetSpriteForLevel(level);
        string content = so.GetFormattedDescription(level);

        // [변경] Show 함수에 'transform.position' (슬롯의 위치) 추가 전달
        if (TooltipSystem.TryGetInstance(out TooltipSystem tooltip))
        {
            tooltip.Show(title, levelSprite, content, transform.position);
        }
    }

    // 마우스 뗐을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentInstance == null) return;

        if (TooltipSystem.TryGetInstance(out TooltipSystem tooltip))
        {
            tooltip.Hide();
        }
    }
}
