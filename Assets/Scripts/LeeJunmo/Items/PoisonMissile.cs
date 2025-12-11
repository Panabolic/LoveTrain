using UnityEngine;

public class PoisonMissile : MonoBehaviour
{
    // --- 스탯 ---
    private float moveSpeed;
    private float verticalDistance;

    // 독가스 스탯
    private GameObject gasPrefab;
    private float gasDamage;
    private float gasTickRate;
    private float gasMoveSpeed;

    // --- 상태 ---
    private bool isHoming = false;
    private Vector3 startPos;

    private Transform target;       // 타겟 트랜스폼
    private Enemy targetEnemy;      // 타겟의 생사 확인용 스크립트
    private Vector3 lastTargetPos;  // 적의 마지막 위치 저장용

    // 안전장치: 4초 뒤 자동 폭발
    private float lifeTimer = 0f;
    private float maxLifeTime = 4.0f;

    public void Initialize(float speed, float vDist, GameObject gasPrefab, float gDmg, float gTick, float gSpeed)
    {
        this.moveSpeed = speed;
        this.verticalDistance = vDist;
        this.gasPrefab = gasPrefab;
        this.gasDamage = gDmg;
        this.gasTickRate = gTick;
        this.gasMoveSpeed = gSpeed;

        this.startPos = transform.position;
        this.isHoming = false;
        this.lifeTimer = 0f;
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;

        // 4초 지나면 강제 폭발
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifeTime)
        {
            Explode();
            return;
        }

        if (!isHoming)
        {
            // [1단계] 수직 상승
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

            if (Vector3.Distance(startPos, transform.position) >= verticalDistance)
            {
                StartHoming();
            }
        }
        else
        {
            // [2단계] 유도 비행
            MoveTowardsTarget();
        }
    }

    private void StartHoming()
    {
        isHoming = true;

        // 가장 가까운 적 찾기
        GameObject bestTargetObj = FindNearestEnemyObj();

        if (bestTargetObj != null)
        {
            target = bestTargetObj.transform;
            targetEnemy = bestTargetObj.GetComponent<Enemy>();
            lastTargetPos = target.position;
        }
        else
        {
            Explode();
        }
    }

    private GameObject FindNearestEnemyObj()
    {
        GameObject bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        if (PoolManager.instance != null)
        {
            foreach (Enemy enemy in PoolManager.instance.activeEnemies)
            {
                // ✨ [수정] 살아있고(active) && 타겟팅 가능(화면 안)한 적만 찾기
                if (enemy == null || !enemy.gameObject.activeSelf || !enemy.IsTargetable) continue;

                Vector3 directionToTarget = enemy.transform.position - currentPos;
                float dSqrToTarget = directionToTarget.sqrMagnitude;

                if (dSqrToTarget < closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    bestTarget = enemy.gameObject;
                }
            }
        }
        return bestTarget;
    }

    private void MoveTowardsTarget()
    {
        Vector3 destPos;

        // ✨ [수정] 타겟이 존재하고 && 타겟팅 가능(IsTargetable)할 때만 계속 추적
        if (target != null && target.gameObject.activeSelf && targetEnemy != null && targetEnemy.IsTargetable)
        {
            destPos = target.position;
            lastTargetPos = destPos;
        }
        else
        {
            // 타겟 소실(사망 or 화면 밖): 마지막 위치로 이동 후 폭발
            destPos = lastTargetPos;

            if (Vector3.Distance(transform.position, destPos) < 0.5f)
            {
                Explode();
                return;
            }
        }

        // 회전
        Vector3 dir = destPos - transform.position;
        if (dir != Vector3.zero)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, 15f * Time.deltaTime);
        }

        // 이동
        transform.Translate(Vector3.up * (moveSpeed * 2.5f) * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 땅에 닿으면 폭발
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Explode();
        }
        // 적에 닿았을 때
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();

            // ✨ [수정] 적이 화면 안에 있을 때(IsTargetable)만 폭발
            // 화면 밖 적이라면 폭발하지 않고 그냥 통과함
            if (enemy != null && enemy.IsTargetable)
            {
                Explode();
            }
        }
    }

    private void Explode()
    {
        if (gasPrefab != null)
        {
            GameObject gas = Instantiate(gasPrefab, transform.position, Quaternion.identity);
            PoisonGas gasLogic = gas.GetComponent<PoisonGas>();
            if (gasLogic != null)
            {
                SoundEventBus.Publish(SoundID.Item_MissileBoom);
                gasLogic.Initialize(gasDamage, gasTickRate, gasMoveSpeed);
            }
        }

        Destroy(gameObject);
    }
}