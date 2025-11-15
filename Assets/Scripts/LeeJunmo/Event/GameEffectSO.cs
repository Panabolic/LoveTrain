using UnityEngine;

// 모든 '로직 템플릿' SO의 기반이 되는 추상 클래스
public abstract class GameEffectSO : ScriptableObject
{
    /// <summary>
    /// [수정] 이 효과의 실제 로직을 실행하고, '데이터'를 인자로 받습니다.
    /// </summary>
    /// <param name="target">이벤트를 발동시킨 대상</param>
    /// <param name="parameters">인스펙터에서 설정한 '인라인 데이터'</param>
    /// <returns>UI에 표시될 기본 텍스트</returns>
    public abstract string Execute(GameObject target, EffectParameters parameters);
}