using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpUIManager : MonoBehaviour
{
    public static LevelUpUIManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private LevelUpChoiceUI choiceSlot1;
    [SerializeField] private LevelUpChoiceUI choiceSlot2;
    [SerializeField] private LevelUpChoiceUI choiceSlot3;
    [SerializeField] private RectTransform levelUpTitleText;

    [Header("데이터")]
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private Inventory playerInventory;

    [Header("Animation")]
    [SerializeField] private float titleIntroDuration = 0.18f;
    [SerializeField] private float titleHoldDuration = 0.12f;
    [SerializeField] private float titleSettleDuration = 0.2f;
    [SerializeField] private float choiceIntroDuration = 0.18f;
    [SerializeField] private float choiceStaggerDelay = 0.06f;
    [SerializeField] private Vector2 titleIntroOffset = new Vector2(0f, -360f);
    [SerializeField] private Vector2 choiceIntroOffset = new Vector2(0f, -220f);

    private readonly List<LevelUpChoiceUI> choiceSlots = new List<LevelUpChoiceUI>();
    private readonly List<RectTransform> choiceRects = new List<RectTransform>();
    private readonly List<Vector2> choiceOriginalPositions = new List<Vector2>();
    private readonly List<bool> choiceShouldReveal = new List<bool>();

    private RectTransform titleTextRect;
    private Vector2 titleOriginalPosition;
    private bool hasCachedLayout;
    private bool isRevealing;
    private Sequence revealSequence;

    private void Awake()
    {
        Instance = this;
        // 싱글톤 보호 로직 (중복 생성 방지)
        if (Instance != this) return;

        CacheChoiceSlots();
        CacheRuntimeReferences();
        ResetLevelUpUIState();
    }

    private void OnDisable()
    {
        KillRevealSequence();
        isRevealing = false;
    }

    // ✨ GameManager의 큐에서 호출됨
    public void ShowLevelUpChoices()
    {
        ResetLevelUpUIState();

        List<Item_SO> availableItems = new List<Item_SO>();

        SoundEventBus.Publish(SoundID.UI_LevelUp);
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
            ResetLevelUpUIState();
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

        CacheRuntimeReferences();
        SetChoiceInteractable(false);

        if (titleTextRect == null)
        {
            Debug.LogWarning("[LevelUpUIManager] levelUpTitleText가 연결되지 않아 레벨업 순차 연출을 건너뜁니다.");
            ResetAnimatedElementsToOriginal();
            SetChoiceInteractable(true);
            return;
        }

        PlayRevealSequence();
    }

    private void SetupSlot(LevelUpChoiceUI slot, Item_SO item)
    {
        if (item != null) slot.DisplayChoice(item, playerInventory, this);
        else slot.gameObject.SetActive(false); // 아이템이 부족하면 슬롯 끄기
    }

    public void OnChoiceSelected(Item_SO selectedItemSO)
    {
        if (isRevealing) return;

        KillRevealSequence();
        ResetAnimatedElementsToOriginal();
        playerInventory.AcquireItem(selectedItemSO);
        ResetLevelUpUIState();
        GameManager.Instance.CloseUI();
    }

    private void CacheChoiceSlots()
    {
        choiceSlots.Clear();
        choiceSlots.Add(choiceSlot1);
        choiceSlots.Add(choiceSlot2);
        choiceSlots.Add(choiceSlot3);
    }

    private void CacheRuntimeReferences()
    {
        if (levelUpPanel == null) return;

        titleTextRect = levelUpTitleText;
        CacheChoiceRects();

        if (!hasCachedLayout)
        {
            CacheOriginalLayout();
        }
    }

    private void CacheChoiceRects()
    {
        choiceRects.Clear();
        foreach (LevelUpChoiceUI slot in choiceSlots)
        {
            choiceRects.Add(slot != null ? slot.GetComponent<RectTransform>() : null);
        }
    }

    private void CacheOriginalLayout()
    {
        if (titleTextRect != null)
        {
            titleOriginalPosition = titleTextRect.anchoredPosition;
        }

        choiceOriginalPositions.Clear();
        foreach (RectTransform choiceRect in choiceRects)
        {
            choiceOriginalPositions.Add(choiceRect != null ? choiceRect.anchoredPosition : Vector2.zero);
        }

        hasCachedLayout = true;
    }

    private void PlayRevealSequence()
    {
        isRevealing = true;

        Vector2 titleCenterPosition = Vector2.zero;
        titleTextRect.gameObject.SetActive(true);
        titleTextRect.anchoredPosition = titleCenterPosition + titleIntroOffset;

        choiceShouldReveal.Clear();
        for (int i = 0; i < choiceSlots.Count; i++)
        {
            LevelUpChoiceUI slot = choiceSlots[i];
            RectTransform choiceRect = GetChoiceRect(i);
            bool shouldReveal = slot != null && choiceRect != null && slot.gameObject.activeSelf;
            choiceShouldReveal.Add(shouldReveal);
            if (!shouldReveal) continue;

            choiceRect.anchoredPosition = GetChoiceOriginalPosition(i) + choiceIntroOffset;
            slot.gameObject.SetActive(false);
        }

        revealSequence = DOTween.Sequence().SetUpdate(true);
        revealSequence.Append(titleTextRect.DOAnchorPos(titleCenterPosition, titleIntroDuration).SetEase(Ease.OutCubic));
        revealSequence.AppendInterval(titleHoldDuration);
        revealSequence.Append(titleTextRect.DOAnchorPos(titleOriginalPosition, titleSettleDuration).SetEase(Ease.OutBack));

        for (int i = 0; i < choiceSlots.Count; i++)
        {
            LevelUpChoiceUI slot = choiceSlots[i];
            RectTransform choiceRect = GetChoiceRect(i);
            if (slot == null || choiceRect == null || !GetChoiceShouldReveal(i)) continue;

            Vector2 targetPosition = GetChoiceOriginalPosition(i);
            revealSequence.AppendCallback(() => slot.gameObject.SetActive(true));
            revealSequence.Append(choiceRect.DOAnchorPos(targetPosition, choiceIntroDuration).SetEase(Ease.OutBack));

            if (choiceStaggerDelay > 0f)
            {
                revealSequence.AppendInterval(choiceStaggerDelay);
            }
        }

        revealSequence.OnComplete(() =>
        {
            isRevealing = false;
            revealSequence = null;
            SetChoiceInteractable(true);
        });
    }

    private void ResetAnimatedElementsToOriginal()
    {
        if (titleTextRect != null)
        {
            titleTextRect.anchoredPosition = titleOriginalPosition;
        }

        for (int i = 0; i < choiceSlots.Count; i++)
        {
            RectTransform choiceRect = GetChoiceRect(i);
            if (choiceRect != null)
            {
                choiceRect.anchoredPosition = GetChoiceOriginalPosition(i);
            }
        }
    }

    private void SetChoiceInteractable(bool interactable)
    {
        foreach (LevelUpChoiceUI slot in choiceSlots)
        {
            if (slot == null) continue;

            Button button = slot.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = interactable && slot.gameObject.activeInHierarchy;
            }
        }
    }

    private void SetChoiceSlotsActive(bool active)
    {
        foreach (LevelUpChoiceUI slot in choiceSlots)
        {
            if (slot != null)
            {
                slot.gameObject.SetActive(active);
            }
        }
    }

    private void ResetLevelUpUIState()
    {
        KillRevealSequence();
        isRevealing = false;
        choiceShouldReveal.Clear();

        CacheRuntimeReferences();
        ResetAnimatedElementsToOriginal();
        SetChoiceInteractable(false);
        SetChoiceSlotsActive(false);

        if (titleTextRect != null)
        {
            titleTextRect.gameObject.SetActive(true);
        }

        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }
    }

    private RectTransform GetChoiceRect(int index)
    {
        return index >= 0 && index < choiceRects.Count ? choiceRects[index] : null;
    }

    private Vector2 GetChoiceOriginalPosition(int index)
    {
        return index >= 0 && index < choiceOriginalPositions.Count ? choiceOriginalPositions[index] : Vector2.zero;
    }

    private bool GetChoiceShouldReveal(int index)
    {
        return index >= 0 && index < choiceShouldReveal.Count && choiceShouldReveal[index];
    }

    private void KillRevealSequence()
    {
        if (revealSequence != null && revealSequence.IsActive())
        {
            revealSequence.Kill();
        }

        revealSequence = null;
    }
}
