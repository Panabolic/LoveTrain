using UnityEngine;

public class Boss : Enemy
{
    [SerializeField] protected SO_Event killEvent;

    protected override float CalculateCalibratedHP()
    {
        // 체력 보정 공식
        // 보스몬스터 기본체력 x 스폰시점 플레이어 레벨 x 이벤트 디버프

        // Event Debuff 계산
        float eventDebuff = 1.0f + (PoolManager.instance.eventDebuff / 100.0f);

        return hp * levelManager.CurrentLevel * eventDebuff;
    }
}
