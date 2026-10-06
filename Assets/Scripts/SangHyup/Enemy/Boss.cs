using UnityEngine;
using System.Collections;

public class Boss : Enemy
{
    [SerializeField] protected SO_Event killEvent;
    [SerializeField] protected GameObject killExplosionEffect;
    [Tooltip("스테이지별 체력. 0 이하 또는 미설정 항목은 기존 hp를 사용합니다.")]
    [SerializeField] private float[] stageHitPoints;
    protected override EnemyRewardKind RewardKind => EnemyRewardKind.Boss;

    // 등장 연출 중인지 체크하는 플래그 (true면 공격 안 함)
    protected bool isEntranceActive = false;

    protected override void OnEnable()
    {
        // init Default value
        calibratedMaxHP = CalculateCalibratedHP();
        currentHP = calibratedMaxHP;
        isAlive = true;
        deathRewardGranted = false;
        if (sprite != null) sprite.enabled = true;

        hasEnteredScreen = false;

        // ✨ [중요] 기본값은 false (연출 없음). 
        // Spawner가 StartEntranceRoutine을 호출해줘야 true가 됨.
        isEntranceActive = false;

        gameObject.layer = LayerMask.NameToLayer("Enemy");

        if (PoolManager.instance != null)
        {
            PoolManager.instance.RegisterEnemy(this);
        }
    }

    protected override float CalculateCalibratedHP()
    {
        float eventDebuff = PoolManager.instance != null
            ? 1.0f + (PoolManager.instance.eventDebuff / 100.0f)
            : 1.0f;
        int stageIndex = StageManager.Instance != null ? StageManager.Instance.CurrentStageIndex : 0;
        float authoredHP = stageHitPoints != null && stageIndex >= 0 && stageIndex < stageHitPoints.Length && stageHitPoints[stageIndex] > 0f
            ? stageHitPoints[stageIndex] : hp;
        return authoredHP * eventDebuff;
    }

    public void StartEntranceRoutine(Vector3 targetPos, float duration)
    {
        StartCoroutine(EntranceMoveRoutine(targetPos, duration));
    }

    protected virtual IEnumerator EntranceMoveRoutine(Vector3 targetPos, float duration)
    {
        isEntranceActive = true; // 패턴 봉인

        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            yield return null;
        }

        transform.position = targetPos;

        isEntranceActive = false; // 봉인 해제
    }
}
