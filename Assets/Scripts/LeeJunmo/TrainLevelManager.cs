using UnityEngine;
using System;

public class TrainLevelManager : MonoBehaviour
{
    [Header("Level Settings")]
    [Tooltip("레벨 1에서 2로 가기 위해 필요한 초기 경험치")]
    [SerializeField] private int initialRequiredExperience = 100;

    [Tooltip("레벨업 시마다 다음 필요 경험치량이 늘어나는 배율 (예: 1.5 = 50%씩 증가)")]
    [SerializeField] private float experienceMultiplier = 1.5f;

    // --- 공개 속성 (다른 스크립트가 읽기용) ---
    public int CurrentLevel { get; private set; }
    public float TotalExperience { get; private set; }
    public float ExperienceForCurrentLevel { get; private set; } // 현재 레벨이 되기 위한 '총' 누적 경험치
    public float ExperienceToNextLevel { get; private set; }   // 다음 레벨이 되기 위한 '총' 누적 경험치

    // --- UI 표시용 편의 속성 ---
    public float CurrentLevelDisplayXp => TotalExperience - ExperienceForCurrentLevel;
    public float RequiredLevelDisplayXp => ExperienceToNextLevel - ExperienceForCurrentLevel;
    public float CurrentLevelProgress
    {
        get
        {
            // 0으로 나누기 방지
            if (RequiredLevelDisplayXp == 0) return 0;
            return (float)CurrentLevelDisplayXp / RequiredLevelDisplayXp;
        }
    }


    // --- 이벤트 ---
    public event Action OnExperienceGained;
    public event Action OnLevelUp;

    void Start()
    {
        CurrentLevel = 1;
        TotalExperience = 0;
        ExperienceForCurrentLevel = 0; // 레벨 1의 시작은 0
        ExperienceToNextLevel = initialRequiredExperience; // 레벨 2 요구치
    }

    /// <summary>
    /// 외부에서 경험치를 추가할 때 호출하는 함수 (예: Mob 스크립트)
    /// </summary>
    public void GainExperience(float amount)
    {
        if (amount <= 0) return;

        TotalExperience += amount;
        OnExperienceGained?.Invoke(); // 경험치 획득 이벤트 발생

        // 레벨업 체크 (여러 번 레벨업할 수도 있으므로 while 사용)
        while (TotalExperience >= ExperienceToNextLevel)
        {
            LevelUp();
        }
    }

    /// <summary>
    /// 실제 레벨업을 처리하는 함수
    /// </summary>
    private void LevelUp()
    {
        CurrentLevel++;

        // ✨ [핵심 수정 1]
        // '현재 레벨 구간의 필요 경험치량' (예: 100)을 미리 계산해서 저장합니다.
        float currentLevelRequiredXp = RequiredLevelDisplayXp; // (e.g., 100 - 0 = 100)

        // [핵심 수정 2]
        // 이제 ExperienceForCurrentLevel을 다음 레벨의 시작점(현재의 목표치)으로 업데이트합니다.
        ExperienceForCurrentLevel = ExperienceToNextLevel; // (e.g., 100)

        // [핵심 수정 3]
        // 다음 레벨에 필요한 경험치 '증가량'을 (이전 갭 * 배율)로 계산합니다.
        float nextLevelRequiredGap = (float)(currentLevelRequiredXp * experienceMultiplier); // (e.g., 100 * 1.5 = 150)

        // [핵심 수정 4]
        // 다음 레벨의 '총' 누적 요구치를 계산합니다.
        ExperienceToNextLevel = ExperienceForCurrentLevel + nextLevelRequiredGap; // (e.g., 100 + 150 = 250)

        OnLevelUp?.Invoke(); // 레벨업 이벤트 발생
        /*        EventManager.Instance.TEstEvent();*/
        LevelUpUIManager.Instance.ShowLevelUpChoices();
    }
}