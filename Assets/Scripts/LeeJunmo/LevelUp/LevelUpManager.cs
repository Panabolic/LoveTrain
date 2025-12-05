// LevelUpUIManager.cs (파일명 LevelUpManager.cs)
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LevelUpUIManager : MonoBehaviour
{
    public static LevelUpUIManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private LevelUpChoiceUI choiceSlot1;
    [SerializeField] private LevelUpChoiceUI choiceSlot2;
    [SerializeField] private LevelUpChoiceUI choiceSlot3;

    [Header("데이터")]
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private Inventory playerInventory;

    private void Awake()
    {
        Instance = this;
        if (Instance == null) Instantiate(this);
    }

    // ✨ GameManager의 큐에서 호출됨 (시간 정지는 GameManager가 이미 처리함)
    public void ShowLevelUpChoices()
    {
        List<Item_SO> availableItems = new List<Item_SO>();

        foreach (Item_SO item in itemDatabase.allItems)
        {
            ItemInstance instance = playerInventory.FindItem(item);
            if (instance == null)
            {
                availableItems.Add(item);
            }
            else
            {
                if (instance.currentUpgrade < item.MaxUpgrade)
                {
                    availableItems.Add(item);
                }
            }
        }

        System.Random rng = new System.Random();
        List<Item_SO> randomChoices = availableItems.OrderBy(x => rng.Next()).Take(3).ToList();

        choiceSlot1.gameObject.SetActive(true);
        choiceSlot2.gameObject.SetActive(true);
        choiceSlot3.gameObject.SetActive(true);

        SetupSlot(choiceSlot1, randomChoices.ElementAtOrDefault(0));
        SetupSlot(choiceSlot2, randomChoices.ElementAtOrDefault(1));
        SetupSlot(choiceSlot3, randomChoices.ElementAtOrDefault(2));

        levelUpPanel.SetActive(true);
    }

    private void SetupSlot(LevelUpChoiceUI slot, Item_SO item)
    {
        if (item != null) slot.DisplayChoice(item, playerInventory, this);
        else slot.gameObject.SetActive(false);
    }

    public void OnChoiceSelected(Item_SO selectedItemSO)
    {
        // 1. 아이템 획득
        playerInventory.AcquireItem(selectedItemSO);

        // 2. UI 숨기기
        levelUpPanel.SetActive(false);

        // 3. ✨ [핵심] GameManager에게 알림 (시간 재개 or 다음 창 띄우기)
        GameManager.Instance.CloseUI();
    }
}