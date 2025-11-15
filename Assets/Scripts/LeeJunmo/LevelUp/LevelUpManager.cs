// LevelUpUIManager.cs
using System.Collections.Generic; // List 사용
using UnityEngine;
using System.Linq;

public class LevelUpUIManager : MonoBehaviour
{
    public static LevelUpUIManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject levelUpPanel; // 선택지 3개를 담는 부모 UI
    [SerializeField] private LevelUpChoiceUI choiceSlot1; // 1번 선택지 버튼
    [SerializeField] private LevelUpChoiceUI choiceSlot2; // 2번 선택지 버튼
    [SerializeField] private LevelUpChoiceUI choiceSlot3; // 3번 선택지 버튼

    [Header("데이터")]
    [SerializeField] private ItemDatabase itemDatabase; // [1단계]에서 만든 에셋
    [SerializeField] private Inventory playerInventory; // 플레이어의 Inventory

    private void Awake()
    {
        Instance = this;
        if (Instance == null)
        {
            Instantiate(this);
        }
    }

    // (예시) 플레이어가 레벨 업하면 이 함수가 호출되어야 함
    public void ShowLevelUpChoices()
    {
        // 1. 게임 멈춤
        Time.timeScale = 0f;

        List<Item_SO> availableItems = new List<Item_SO>();

        foreach (Item_SO item in itemDatabase.allItems)
        {
            ItemInstance instance = playerInventory.FindItem(item);

            if (instance == null)
            {
                // 2a. 획득하지 않은 아이템 -> 무조건 후보에 추가
                availableItems.Add(item);
            }
            else
            {
                // 2b. 획득한 아이템 -> 최대 레벨이 *아닌* 것만 후보에 추가
                if (instance.currentUpgrade < item.MaxUpgrade)
                {
                    availableItems.Add(item);
                }
                // (만약 instance.currentUpgrade >= item.MaxUpgrade 이면
                //  리스트에 추가되지 않으므로, 선택지에 절대 나오지 않음)
            }
        }

        // 3. '획득 가능한' 리스트(availableItems)에서 3개를 랜덤으로 뽑습니다.
        // (간단한 예시: 리스트를 섞어서 3개 뽑기)
        System.Random rng = new System.Random();
        List<Item_SO> randomChoices = availableItems.OrderBy(x => rng.Next()).Take(3).ToList();

        // 4. 각 UI 슬롯에 "이 아이템을 표시해!"라고 명령
        choiceSlot1.gameObject.SetActive(true);
        choiceSlot2.gameObject.SetActive(true);
        choiceSlot3.gameObject.SetActive(true);

        // 4. 첫 번째 슬롯 처리 (데이터가 있는지 확인)
        Item_SO choice1 = randomChoices.ElementAtOrDefault(0);
        if (choice1 != null)
        {
            choiceSlot1.DisplayChoice(choice1, playerInventory, this);
        }
        else
        {
            // 4a. 1번 슬롯 데이터가 없으면 '끈다' (0개 뽑힘)
            choiceSlot1.gameObject.SetActive(false);
        }

        // 5. 두 번째 슬롯 처리
        Item_SO choice2 = randomChoices.ElementAtOrDefault(1);
        if (choice2 != null)
        {
            choiceSlot2.DisplayChoice(choice2, playerInventory, this);
        }
        else
        {
            // 5a. 2번 슬롯 데이터가 없으면 '끈다'
            choiceSlot2.gameObject.SetActive(false);
        }

        // 6. 세 번째 슬롯 처리
        Item_SO choice3 = randomChoices.ElementAtOrDefault(2);
        if (choice3 != null)
        {
            choiceSlot3.DisplayChoice(choice3, playerInventory, this);
        }
        else
        {
            // 6a. 3번 슬롯 데이터가 없으면 '끈다'
            choiceSlot3.gameObject.SetActive(false);
        }

        // 4. 패널 활성화
        levelUpPanel.SetActive(true);
    }

    /// <summary>
    /// [3단계]의 선택지 버튼(LevelUpChoice_UI)이 호출할 함수
    /// </summary>
    public void OnChoiceSelected(Item_SO selectedItemSO)
    {
        // 1. [핵심] 인벤토리에 아이템 획득/업그레이드 요청
        playerInventory.AcquireItem(selectedItemSO);

        // 2. UI 숨기기
        levelUpPanel.SetActive(false);

        // 3. 게임 재개
        Time.timeScale = 1f;
    }
}