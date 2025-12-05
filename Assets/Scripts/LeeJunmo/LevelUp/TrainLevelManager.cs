using UnityEngine;
using System;
using System.Linq;

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
        ExperienceToNextLevel = CalculateRequiredDeltaXP(2);
    }

    public void GainExperience(float amount)
    {
        if (amount <= 0) return;

        TotalExperience += amount;
        OnExperienceGained?.Invoke();

        // 레벨업 조건 충족 시 반복 (한 번에 여러 레벨업 가능)
        while (TotalExperience >= ExperienceToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        CurrentLevel++;

        // 1. 현재 레벨 달성 시점 저장
        ExperienceForCurrentLevel = ExperienceToNextLevel;

        // 2. 다음 레벨 목표치 계산
        int nextLevelDelta = CalculateRequiredDeltaXP(CurrentLevel + 1);

        // 3. 최종 목표 누적 경험치 갱신
        ExperienceToNextLevel = ExperienceForCurrentLevel + nextLevelDelta;

        OnLevelUp?.Invoke();

        // ✨ [핵심 수정] 직접 UI를 띄우지 않고, GameManager 큐에 등록
        // 레벨업이 연속으로 일어나도 큐에 쌓여서 하나씩 처리됨
        GameManager.Instance.RegisterUIQueue(() => LevelUpUIManager.Instance.ShowLevelUpChoices());
    }

    /// <summary>
    /// 목표 레벨 도달에 필요한 경험치량 계산 (공식 적용)
    /// </summary>
    private int CalculateRequiredDeltaXP(int targetLevel)
    {
        // 1. 레벨 구간 변수 (Lp)
        int levelGroupIndex = (targetLevel - 1) / 10;
        int levelPeriodVariable = levelGroupIndex * levelPeriodStep;

        // 2. 벽 변수 (Wv) - 배열에서 검색
        int wallVariable = 0;
        if (experienceWalls != null)
        {
            foreach (var wall in experienceWalls)
            {
                if (wall.targetLevel == targetLevel)
                {
                    wallVariable = wall.bonusXP;
                    break;
                }
            }
        }

        // 3. 최종 공식: (목표 레벨 * (기본값 + 구간변수)) + 벽변수
        return (targetLevel * (baseRequiredXP + levelPeriodVariable)) + wallVariable;
    }
}