using UnityEngine;
using System;
using UnityEngine.InputSystem;
using TMPro;

public class TrainLevelManager : MonoBehaviour
{
    // ✨ 경험치 벽 설정을 위한 구조체
    [System.Serializable]
    public struct ExpWall
    {
        [Tooltip("이 레벨에 도달할 때 추가 경험치가 필요합니다.")]
        public int targetLevel;
        [Tooltip("추가될 경험치 양 (Wv)")]
        public int bonusXP;
    }

    [Header("Level Settings")]
    [Tooltip("기본 필요 경험치 (공식의 10에 해당)")]
    [SerializeField] private int baseRequiredXP = 10;

    [Tooltip("레벨 구간마다 증가하는 변수 크기 (기본 2)")]
    [SerializeField] private int levelPeriodStep = 2;

    [Tooltip("특정 레벨 구간의 경험치 벽 설정 (인스펙터에서 추가 가능)")]
    [SerializeField] private ExpWall[] experienceWalls;

    // --- 상태 변수 ---
    public int CurrentLevel { get; private set; } = 1;
    public float TotalExperience { get; private set; }
    public float ExperienceForCurrentLevel { get; private set; }
    public float ExperienceToNextLevel { get; private set; }

    // --- UI 표시용 속성 ---
    public float CurrentLevelDisplayXp => TotalExperience - ExperienceForCurrentLevel;
    public float RequiredLevelDisplayXp => ExperienceToNextLevel - ExperienceForCurrentLevel;
    public float CurrentLevelProgress
    {
        get
        {
            if (RequiredLevelDisplayXp <= 0) return 0;
            return Mathf.Clamp01(CurrentLevelDisplayXp / RequiredLevelDisplayXp);
        }
    }

    public event Action OnExperienceGained;
    public event Action OnLevelUp;

    private void Awake()
    {
        PermanentUpgradeProgress.Reload();
        economy = new RunPartEconomy(PermanentUpgradeProgress.Souls);
        CurrentLevel = 1;
        TotalExperience = 0f;
        ExperienceForCurrentLevel = 0f;
        ExperienceToNextLevel = CalculateRequiredDeltaXP(2);
    }

    void Start()
    {
        GetComponent<Train>()?.SetRunLevel(CurrentLevel);
    }

    [Header("Part creation")]
    [SerializeField, Min(1)] private int initialCreationCost = 30;
    [SerializeField, Min(0)] private int creationCostIncrease = 10;
    public int Flesh => economy.Flesh;
    public int Souls => economy.Souls;
    public int CreatedParts => economy.CreatedParts;
    public int CreationCost => (int)Math.Max(1L, Math.Min(int.MaxValue, (long)initialCreationCost + (long)CreatedParts * Math.Max(0, creationCostIncrease)));
    public int CreationCostIncrease => Math.Max(0, creationCostIncrease);
    public int RerollCost => economy.RerollCost;
    public int FreeRerollsRemaining => economy.FreeRerollsRemaining;
    public bool CanReroll => economy.CanReroll;
    public bool CanPurchaseCreation => Flesh >= CreationCost;
    // Presentation query only; the existing F input and purchase transaction keep ownership.
    public bool CanRequestCreation => CanPurchaseCreation && !creationQueued && !committingCreation &&
        Time.timeScale > 0f && GameManager.Instance != null &&
        (GameManager.Instance.CurrentState == GameState.Playing || GameManager.Instance.CurrentState == GameState.Boss) &&
        Keyboard.current != null && LevelUpUIManager.Instance != null && LevelUpUIManager.Instance.CanShowCreation;
    private RunPartEconomy economy = new RunPartEconomy();
    [SerializeField] private TMP_Text fleshText;
    [SerializeField] private TMP_Text soulText;
    private bool creationQueued;
    private bool committingCreation;
    public event Action OnResourcesChanged;

    private void Update()
    {
        bool combat = GameManager.Instance != null && Time.timeScale > 0f &&
            (GameManager.Instance.CurrentState == GameState.Playing || GameManager.Instance.CurrentState == GameState.Boss);
        if (!combat || creationQueued || Flesh < CreationCost || Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame) return;
        if (LevelUpUIManager.Instance == null) return;
        creationQueued = true;
        GameManager.Instance.RegisterUIQueue(() =>
        {
            creationQueued = false;
            if (this != null && LevelUpUIManager.Instance != null)
                LevelUpUIManager.Instance.ShowCreation(this);
            else if (GameManager.Instance != null) GameManager.Instance.CloseUI();
        });
    }

    // XP grows this run's stats; the F workbench remains the sole item-creation path.
    public void GainExperience(float amount)
    {
        if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        TotalExperience += amount;
        while (TotalExperience >= ExperienceToNextLevel)
        {
            CurrentLevel++;
            ExperienceForCurrentLevel = ExperienceToNextLevel;
            ExperienceToNextLevel = ExperienceForCurrentLevel + CalculateRequiredDeltaXP(CurrentLevel + 1);
            GetComponent<Train>()?.SetRunLevel(CurrentLevel);
            OnLevelUp?.Invoke();
        }
        OnExperienceGained?.Invoke();
    }

    private int CalculateRequiredDeltaXP(int targetLevel)
    {
        int wallXP = 0;
        if (experienceWalls != null)
        {
            foreach (var wall in experienceWalls)
            {
                if (wall.targetLevel != targetLevel) continue;
                wallXP = wall.bonusXP;
                break;
            }
        }
        return TrainLevelRules.RequiredDeltaXP(targetLevel, baseRequiredXP, levelPeriodStep, wallXP);
    }

    public void AddFlesh(int amount) => AddRewards(amount, 0);
    public void AddSouls(int amount) => AddRewards(0, amount);

    public void AddRewards(int flesh, int souls)
    {
        economy.AddReward(flesh, souls);
        ResourcesChanged();
    }

    public bool TrySpendFlesh(int amount)
    {
        if (committingCreation) return false;
        if (!economy.TrySpend(amount)) return false;
        ResourcesChanged();
        return true;
    }

    public bool TryPurchaseCreation()
    {
        if (committingCreation) return false;
        if (!economy.TryPurchaseCreation(CreationCost)) return false;
        ResourcesChanged();
        return true;
    }

    public bool TryPurchaseCreation(Func<bool> equip)
    {
        if (committingCreation || equip == null || !CanPurchaseCreation) return false;
        int cost = CreationCost;
        committingCreation = true;
        try
        {
            if (!equip()) return false;
            if (!economy.TryPurchaseCreation(cost)) return false;
        }
        finally { committingCreation = false; }
        ResourcesChanged();
        return true;
    }

    public bool TryReroll()
    {
        if (!economy.TryReroll()) return false;
        ResourcesChanged();
        return true;
    }

    // Compatibility for event callers that already paid through TrySpendFlesh.
    public void CompleteCreation()
    {
        if (committingCreation) return;
        economy.CompleteCreation();
        ResourcesChanged();
    }

    private void ResourcesChanged()
    {
        PermanentUpgradeProgress.SetSouls(Souls);
        UpdateResourceLabels();
        OnResourcesChanged?.Invoke();
    }

    private void UpdateResourceLabels()
    {
        if (fleshText != null) fleshText.text = $"살점 {Flesh}";
        if (soulText != null) soulText.text = $"영혼 {Souls}";
    }

    private void OnEnable() => UpdateResourceLabels();
    private void OnApplicationPause(bool paused) { if (paused) PlayerPrefs.Save(); }
    private void OnDestroy() => PlayerPrefs.Save();

}

public static class TrainLevelRules
{
    public static int RequiredDeltaXP(int targetLevel, int baseXP, int periodStep, int wallXP)
    {
        long completedLevels = Math.Max(1L, (long)targetLevel - 1L);
        long periodBonus = completedLevels / 10L * Math.Max(0, periodStep);
        long required = completedLevels * (Math.Max(1, baseXP) + periodBonus) + Math.Max(0, wallXP);
        return (int)Math.Max(1L, Math.Min(int.MaxValue, required));
    }
}

public sealed class RunPartEconomy
{
    public int Flesh { get; private set; }
    public int Souls { get; private set; }
    public int CreatedParts { get; private set; }
    public int FreeRerollsRemaining { get; private set; } = 5;
    private int paidRerolls;
    public int RerollCost => FreeRerollsRemaining > 0 ? 0 : Math.Min(5, paidRerolls + 1);
    public bool CanReroll => FreeRerollsRemaining > 0 || Souls >= RerollCost;

    public RunPartEconomy(int savedSouls = 0) { Souls = Math.Max(0, savedSouls); }

    public void AddReward(int amount, int souls = 0)
    {
        Flesh = (int)Math.Min(int.MaxValue, (long)Flesh + Math.Max(0, amount));
        Souls = (int)Math.Min(int.MaxValue, (long)Souls + Math.Max(0, souls));
    }
    public bool TryPurchaseCreation(int cost)
    {
        if (cost < 1 || !TrySpend(cost)) return false;
        CompleteCreation();
        return true;
    }
    public bool TryReroll()
    {
        if (!CanReroll) return false;
        if (FreeRerollsRemaining > 0) FreeRerollsRemaining--;
        else
        {
            Souls -= RerollCost;
            paidRerolls = Math.Min(4, paidRerolls + 1);
        }
        return true;
    }
    public bool TrySpend(int amount)
    {
        if (amount < 0 || Flesh < amount) return false;
        Flesh -= amount;
        return true;
    }
    public void CompleteCreation() { if (CreatedParts < int.MaxValue) CreatedParts++; }
}
