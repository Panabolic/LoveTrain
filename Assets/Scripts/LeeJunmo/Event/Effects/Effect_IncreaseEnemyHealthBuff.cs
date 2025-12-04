using UnityEngine;

[CreateAssetMenu(fileName = "Effect_IncreaseEnemyHealthBuff", menuName = "Event System/Effects/Enemy/Increase Health Buff")]
public class Effect_IncreaseEnemyHealthBuff : GameEffectSO
{
    public override string Execute(GameObject target, EffectParameters parameters)
    {
        // intValue: 증가시킬 체력 퍼센트 (예: 20 -> 20% 증가)
        int amount = parameters.intValue;

        if (PoolManager.instance != null)
        {
            PoolManager.instance.eventDebuff += amount;
            return $"적들의 체력이 강화되었습니다! (+{amount}%)";
        }

        return "오류: PoolManager를 찾을 수 없습니다.";
    }
}