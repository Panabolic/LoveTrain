using UnityEngine;
using UnityEngine.UI; // Image를 사용하기 위해 필요
using TMPro;

// (파일 이름과 클래스 이름이 'InventorySlotUI'로 동일해야 합니다)
public class InventorySlotUI : MonoBehaviour
{
    [Header("UI 구성요소")]
    // [SerializeField]를 제거하고 private으로 변경
    private Image itemIcon;  // 아이템 아이콘 (첫 번째 자식)
    private Image levelIcon; // 레벨 아이콘 (두 번째 자식)

    [Header("데이터 참조")]
    // [1단계]에서 만든 LevelSpriteAtlas 에셋을 여기에 연결
    [SerializeField] private LevelSpriteAtlas levelAtlas;

    /// <summary>
    /// 스크립트가 활성화될 때 자동으로 컴포넌트를 찾습니다.
    /// </summary>
    private void Awake()
    {
        // 1. "첫 번째 자식" (인덱스 0)에서 Image 컴포넌트를 찾습니다.
        if (transform.childCount > 0)
        {
            itemIcon = transform.GetChild(0).GetComponent<Image>();
        }

        // 2. "두 번째 자식" (인덱스 1)에서 Image 컴포넌트를 찾습니다.
        if (transform.childCount > 1)
        {
            levelIcon = transform.GetChild(1).GetComponent<Image>();
        }

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
        // (안전 장치) Awake에서 아이콘을 못 찾았으면 오류 방지
        if (itemIcon == null || levelIcon == null)
        {
            return;
        }

        // 1. '빈 슬롯' 처리
        if (instance == null)
        {
            itemIcon.enabled = false;
            levelIcon.enabled = false;
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
    }
}