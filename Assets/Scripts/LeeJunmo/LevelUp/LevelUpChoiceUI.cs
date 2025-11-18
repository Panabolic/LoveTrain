// LevelUpChoice_UI.cs
using UnityEngine;
using UnityEngine.UI; // Button, Image
using TMPro; // TextMeshPro

public class LevelUpChoiceUI : MonoBehaviour
{
    [Header("UI (Common)")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    [Header("UI (New Item)")]
    [Tooltip("새 아이템일 때 켤 그룹 (이 안에 'NEW' 스프라이트가 있어야 함)")]
    [SerializeField] private GameObject newLevelUI;
    // 'newLevelSprite' 필드는 제거됨

    [Header("UI (Upgrade Item)")]
    [Tooltip("업그레이드일 때 켤 그룹")]
    [SerializeField] private GameObject upgradeLevelGroup;
    [Tooltip("upgradeLevelGroup 안의 '현재 레벨' 스프라이트")]
    [SerializeField] private Image currentLevelSprite;
    [Tooltip("upgradeLevelGroup 안의 '화살표' 이미지")]
    [SerializeField] private Image arrowIcon; // '화살표 스프라이트'가 적용될 곳
    [Tooltip("upgradeLevelGroup 안의 '다음 레벨' 스프라이트")]
    [SerializeField] private Image nextLevelSprite;

    [Header("Data Reference")]
    [SerializeField] private LevelSpriteAtlas levelAtlas; // 로마자/MAX 스프라이트 DB

    private Item_SO currentItemSO;
    private LevelUpUIManager uiManager;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnSelectButton);
    }

    public void DisplayChoice(Item_SO itemSO, Inventory playerInventory, LevelUpUIManager manager)
    {
        if (itemSO == null)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);

        this.currentItemSO = itemSO;
        this.uiManager = manager;

        // 1. 공통 정보 갱신 (아이콘, 이름, 설명)
        itemIcon.sprite = itemSO.iconSprite;
        itemNameText.text = itemSO.itemName;
        itemDescriptionText.text = itemSO.itemSimpleScript;

        // 2. 이 아이템을 이미 가졌는지 확인
        ItemInstance existingInstance = playerInventory.FindItem(itemSO);

        if (existingInstance == null)
        {
            // --- 3A. 새 아이템일 경우 ---
            newLevelUI.SetActive(true);      // "NEW" 그룹 켜기
            upgradeLevelGroup.SetActive(false); // "업그레이드" 그룹 끄기
        }
        else
        {
            // --- 3B. 업그레이드일 경우 ---
            newLevelUI.SetActive(false);     // "NEW" 그룹 끄기
            upgradeLevelGroup.SetActive(true);  // "업그레이드" 그룹 켜기

            int currentLevel = existingInstance.currentUpgrade;
            int nextLevel = currentLevel + 1;

            // '현재 레벨' 스프라이트 설정 (예: I, II...)
            currentLevelSprite.sprite = levelAtlas.GetSpriteForLevel(currentLevel);

            // '다음 레벨' 스프라이트 설정 (예: II, III, MAX...)
            if (nextLevel >= itemSO.MaxUpgrade)
            {
                nextLevelSprite.sprite = levelAtlas.maxLevelSprite;
            }
            else
            {
                nextLevelSprite.sprite = levelAtlas.GetSpriteForLevel(nextLevel);
            }
        }
    }

    private void OnSelectButton()
    {
        uiManager.OnChoiceSelected(currentItemSO);
    }
}