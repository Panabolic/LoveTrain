using UnityEngine;
using System.Collections.Generic;
using System.Linq; // .Sum()을 사용하기 위해

[CreateAssetMenu(fileName = "New Event Select", menuName = "Event System/Event Select")]
public class GameEventSO : ScriptableObject
{
    [Header("Read me")]
    [TextArea] public string description;

    [Header("이벤트 뽑기 그룹")]
    [Tooltip("여기에 포함된 '모든 그룹'을 각각 한 번씩 실행합니다. 각 그룹에서는 '단 하나'만 뽑힙니다.")]
    public List<EventRollGroup> rollGroups;

    /// <summary>
    /// 모든 '그룹'을 순회하며 '가중치 뽑기'를 실행하고, '결과 텍스트 목록'을 반환합니다.
    /// </summary>
    public List<string> Trigger(GameObject target)
    {
        List<string> results = new List<string>();

        foreach (EventRollGroup group in rollGroups)
        {
            float totalWeight = group.outcomes.Sum(o => o.weight);
            if (totalWeight <= 0) continue;

            float roll = Random.Range(0f, totalWeight);
            WeightedEventOutcome chosenOutcome = null;

            foreach (var outcome in group.outcomes)
            {
                roll -= outcome.weight;
                if (roll <= 0f)
                {
                    chosenOutcome = outcome;
                    break;
                }
            }

            // 3. 선택된 효과를 실행
            if (chosenOutcome != null && chosenOutcome.outputSettings != null)
            {
                string defaultText = null;

                // 3a. '로직'이 할당되어 있다면 실행
                if (chosenOutcome.effectLogic != null)
                {
                    // --- [핵심 수정] ---
                    // '로직'에게 '인라인 데이터'를 전달하여 실행
                    defaultText = chosenOutcome.effectLogic.Execute(target, chosenOutcome.parameters);
                    // --- [수정 끝] ---
                }

                var outputSettings = chosenOutcome.outputSettings;
                string specialText = outputSettings.specialText;

                // 3b. 텍스트 조합 (로직이 없어도 특수 텍스트는 출력 가능)
                if (outputSettings.order == EventResultOutput.OutputOrder.DefaultFirst)
                {
                    if (outputSettings.includeDefaultText && !string.IsNullOrEmpty(defaultText)) results.Add(defaultText);
                    if (!string.IsNullOrEmpty(specialText)) results.Add(specialText);
                }
                else // SpecialFirst
                {
                    if (!string.IsNullOrEmpty(specialText)) results.Add(specialText);
                    if (outputSettings.includeDefaultText && !string.IsNullOrEmpty(defaultText)) results.Add(defaultText);
                }
            }
        }

        return results;
    }
}