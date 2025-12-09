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
        // 싱글톤 보호 로직 (중복 생성 방지)
        if (Instance != this) return;
    }

    // ✨ GameManager의 큐에서 호출됨
    public void ShowLevelUpChoices()
    {
        List<Item_SO> availableItems = new List<Item_SO>();

        foreach (Item_SO item in itemDatabase.allItems)
        {
            ItemInstance instance = playerInventory.FindItem(item);

            // 1. 아직 없는 아이템이면 획득 가능
            if (instance == null)
            {
                availableItems.Add(item);
            }
            // 2. 이미 있는 아이템이면 Max 레벨이 아닐 때만 강화 가능
            else
            {
                if (instance.currentUpgrade < item.MaxUpgrade)
                {
                    availableItems.Add(item);
                }
            }
        }

        // ✨ [핵심 수정] 획득/강화 가능한 아이템이 하나도 없으면 스킵
        if (availableItems.Count == 0)
        {
            Debug.Log("모든 아이템이 만렙이거나 획득 불가능하여 레벨업 선택지를 건너뜁니다.");

            // UI를 띄우지 않고 바로 닫기 처리 (게임 시간 재개)
            GameManager.Instance.CloseUI();
            return;
        }

        // --- 기존 로직 (선택지 섞고 표시) ---
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
        else slot.gameObject.SetActive(false); // 아이템이 부족하면 슬롯 끄기
    }

    public void OnChoiceSelected(Item_SO selectedItemSO)
    {
        playerInventory.AcquireItem(selectedItemSO);
        levelUpPanel.SetActive(false);
        GameManager.Instance.CloseUI();
    }
}