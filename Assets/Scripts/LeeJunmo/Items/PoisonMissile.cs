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

    // ✨ [추가] 안전장치: 4초 뒤 자동 폭발
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
        this.lifeTimer = 0f; // 타이머 초기화
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;

        // ✨ [추가] 4초 지나면 강제 폭발 (뱅뱅 도는 현상 방지)
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

        // 가장 가까운 적 찾기 (Transform 뿐만 아니라 Enemy 컴포넌트도 같이 캐싱)
        GameObject bestTargetObj = FindNearestEnemyObj();

        if (bestTargetObj != null)
        {
            target = bestTargetObj.transform;
            targetEnemy = bestTargetObj.GetComponent<Enemy>(); // 생사 확인용 컴포넌트 가져오기
            lastTargetPos = target.position;
        }
        else
        {
            Explode();
        }
    }

    // 기존 FindNearestEnemy를 수정하여 GameObject를 반환 (Enemy 컴포넌트 접근 위해)
    private GameObject FindNearestEnemyObj()
    {
        GameObject bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        if (PoolManager.instance != null)
        {
            foreach (Enemy enemy in PoolManager.instance.activeEnemies)
            {
                // 살아있는 적만 타겟팅 후보로 선정
                if (enemy == null || !enemy.gameObject.activeSelf || !enemy.GetIsAlive()) continue;

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

        // ✨ [핵심 수정] 타겟이 존재하고(Active) && 실제로 살아있을 때(IsAlive)만 추적
        if (target != null && target.gameObject.activeSelf && targetEnemy != null && targetEnemy.GetIsAlive())
        {
            // 타겟 생존: 위치 계속 갱신
            destPos = target.position;
            lastTargetPos = destPos;
        }
        else
        {
            // 타겟 소실(사망 포함): 
            // 더 이상 target.position을 갱신하지 않고, '죽은 시점의 위치(lastTargetPos)'로 고정
            destPos = lastTargetPos;

            // 목적지 근처 도달 시 폭발
            if (Vector3.Distance(transform.position, destPos) < 0.5f)
            {
                Explode();
                return;
            }
        }

        // 회전 (뱅뱅 도는 현상을 줄이기 위해 회전 속도를 조금 높임 10f -> 15f)
        Vector3 dir = destPos - transform.position;
        if (dir != Vector3.zero)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
            // Slerp 속도를 높여서 더 빨리 꺾도록 유도
            transform.rotation = Quaternion.Slerp(transform.rotation, q, 15f * Time.deltaTime);
        }

        // 이동
        transform.Translate(Vector3.up * (moveSpeed * 2.5f) * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Explode();
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
                gasLogic.Initialize(gasDamage, gasTickRate, gasMoveSpeed);
            }
        }

        Destroy(gameObject);
    }
}