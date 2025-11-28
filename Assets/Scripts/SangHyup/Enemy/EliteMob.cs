using UnityEngine;

public class EliteMob : Mob
{
    protected override void OnEnable()
    {
        base.OnEnable();

        calibratedHP = hp * (1 + ((GameManager.Instance.gameTime / 60.0f) * (1.0f + hpIncreasePercent) / 100)) * 1.5f * (1.0f + PoolManager.instance.eventDebuffPercent / 100);

        currentHP = calibratedHP;
    }
}