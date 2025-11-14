// LevelUpChoice_UI.cs
using TMPro; // TextMeshPro
using UnityEngine;
using UnityEngine.UI; // Button, Image

public class LevelUpChoiceUI : MonoBehaviour
{
    [Header("UI 구성요소")]
    [SerializeField] private Image itemIcon;  // 아이템 이미지
    [SerializeField] private TextMeshProUGUI itemNameText; // 아이템 이름
    [SerializeField] private TextMeshProUGUI itemDescriptionText; // 아이템 설명
    [SerializeField] private Image levelIcon; // 로마자 레벨 스프라이트

    [Header("데이터 참조")]
    [SerializeField] private LevelSpriteAtlas levelAtlas; // 레벨(int) -> 로마자(Sprite) 변환기

    private Item_SO currentItemSO; // 이 버튼이 보여주는 아이템
    private LevelUpUIManager uiManager; // 부모 매니저

    void Start()
    {
        // 버튼 클릭 시 OnSelectButton() 함수가 호출되도록 연결
        GetComponent<Button>().onClick.AddListener(OnSelectButton);
    }

    /// <summary>
    /// [2단계]의 매니저가 호출하는 함수. UI를 갱신합니다.
    /// </summary>
    public void DisplayChoice(Item_SO itemSO, Inventory playerInventory, LevelUpUIManager manager)
    {
        this.currentItemSO = itemSO;
        this.uiManager = manager;

        // 1. [핵심] 이 아이템의 '표시될 레벨' 계산
        int displayLevel;
        bool isMaxed = false; 
        ItemInstance existingInstance = playerInventory.FindItem(itemSO); // (4단계에서 추가할 함수)

        if (existingInstance != null)
        {
            // 이미 갖고 있으면 -> (현재 레벨 + 1)을 표시
            displayLevel = existingInstance.currentUpgrade + 1;
            if (displayLevel >= itemSO.MaxUpgrade)
            {
                isMaxed = true;
                displayLevel = itemSO.MaxUpgrade; // (표시 레벨 고정)
            }
        }
        else
        {
            // 새 아이템이면 -> 1레벨(I)을 표시
            displayLevel = 1;
        }

        // 2. UI 갱신
        itemIcon.sprite = itemSO.iconSprite;
        itemNameText.text = itemSO.itemName;
        itemDescriptionText.text = itemSO.itemScript;

        // 3. 레벨 스프라이트 갱신
        Sprite levelSprite;
        if (isMaxed)
        {
            levelSprite = levelAtlas.maxLevelSprite;
        }
        else
        {
            levelSprite = levelAtlas.GetSpriteForLevel(displayLevel);
        }

        levelIcon.sprite = levelSprite;
        levelIcon.enabled = true;
    }

    /// <summary>
    /// 이 UI 버튼을 클릭했을 때 호출됩니다.
    /// </summary>
    private void OnSelectButton()
    {
        // [2단계]의 매니저에게 "나 선택됐어!"라고 아이템 정보를 넘겨줌
        uiManager.OnChoiceSelected(currentItemSO);
    }
}