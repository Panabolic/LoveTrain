using UnityEngine;

public class Boss : Enemy
{
    [SerializeField] protected SO_Event killEvent;

    protected override void OnEnable()
    {
        // init Default value
        calibratedMaxHP    = CalculateCalibratedHP();
        currentHP       = calibratedMaxHP;
        isAlive         = true;
        sprite.enabled  = true;

        // ✨ [중요] 재활용될 때 화면 진입 여부 초기화 (다시 화면 밖에서 시작하므로)
        hasEnteredScreen = false;

        // Layer 변경
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        if (PoolManager.instance != null)
        {
            PoolManager.instance.RegisterEnemy(this);
        }
    }

    protected override float CalculateCalibratedHP()
    {
        // 체력 보정 공식
        // 보스몬스터 기본체력 x 스폰시점 플레이어 레벨 x 이벤트 디버프

        // Event Debuff 계산
        float eventDebuff = 1.0f + (PoolManager.instance.eventDebuff / 100.0f);

        return hp * levelManager.CurrentLevel * eventDebuff;
    }
}
