using UnityEngine;
using System.Collections.Generic;

[System.Serializable] // SO가 아닌 순수 C# 클래스
public class EventRollGroup
{
    [Tooltip("Read me")]
    public string description;

    [Tooltip("이 그룹에서 '단 하나'만 뽑힐 가중치 목록")]
    public List<WeightedEventOutcome> outcomes;
}