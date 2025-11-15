using UnityEngine;

[System.Serializable]
public class WeightedEventOutcome
{
    [Tooltip("이 결과가 선택될 '가중치' (확률 %와 동일)")]
    public float weight = 10f;

    // --- [핵심 수정] ---
    [Tooltip("실행할 '로직' (GameEffectSO 템플릿 에셋)")]
    public GameEffectSO effectLogic;

    [Tooltip("이 로직에 전달할 '데이터' (인라인 설정)")]
    public EffectParameters parameters;
    // --- [수정 끝] ---

    [Tooltip("이 결과가 선택되었을 때의 텍스트 출력 설정")]
    public EventResultOutput outputSettings;
}