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
            return EnglishLocalization.Format("result.enemy_hp", "적들의 체력이 강화되었습니다! (+{0}%)", amount);
        }

        return EnglishLocalization.Get("result.error.pool_missing", "오류: PoolManager를 찾을 수 없습니다.");
    }
}