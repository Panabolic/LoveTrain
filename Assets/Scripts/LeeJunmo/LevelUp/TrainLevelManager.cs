using UnityEngine;
using System;
using System.Linq;
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
    public int CurrentLevel { get; private set; }
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

    void Start()
    {
        CurrentLevel = 1;
        TotalExperience = 0;
        ExperienceForCurrentLevel = 0;

        // 1 -> 2 레벨업 경험치 계산
        ExperienceToNextLevel = float.PositiveInfinity;
    }

    [Header("Part creation")]
    [SerializeField] private int initialCreationCost = 50;
    [SerializeField] private int creationCostIncrease = 25;
    [SerializeField] private int rerollCost = 10;
    public int Flesh => economy.Flesh;
    public int CreatedParts => economy.CreatedParts;
    public int CreationCost => (int)Math.Max(1L, Math.Min(int.MaxValue, (long)initialCreationCost + (long)CreatedParts * Math.Max(0, creationCostIncrease)));
    public int RerollCost => Mathf.Max(0, rerollCost);
    private readonly RunPartEconomy economy = new RunPartEconomy();
    [SerializeField] private TMP_Text fleshText;
    private bool creationQueued;

    private void Update()
    {
        if (fleshText != null) fleshText.text = $"살점 {Flesh}";
        bool combat = GameManager.Instance != null && Time.timeScale > 0f &&
            (GameManager.Instance.CurrentState == GameState.Playing || GameManager.Instance.CurrentState == GameState.Boss);
        if (!combat || GameManager.Instance.IsTimeForEnding || creationQueued || Flesh < CreationCost || Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame) return;
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

    // Legacy enemy reward entry point now grants flesh instead of XP and level-up choices.
    public void GainExperience(float amount)
    {
        if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        economy.AddReward(Mathf.CeilToInt(amount));
        OnExperienceGained?.Invoke();
    }

    public bool TrySpendFlesh(int amount)
    {
        if (!economy.TrySpend(amount)) return false;
        OnExperienceGained?.Invoke();
        return true;
    }

    public void CompleteCreation() { economy.CompleteCreation(); OnExperienceGained?.Invoke(); }

}

public sealed class RunPartEconomy
{
    public int Flesh { get; private set; }
    public int CreatedParts { get; private set; }
    public void AddReward(int amount)
    {
        if (amount <= 0) return;
        Flesh = (int)Math.Min(int.MaxValue, (long)Flesh + amount);
    }
    public bool TrySpend(int amount)
    {
        if (amount < 0 || Flesh < amount) return false;
        Flesh -= amount;
        return true;
    }
    public void CompleteCreation() { if (CreatedParts < int.MaxValue) CreatedParts++; }
}
