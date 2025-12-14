using System.Collections;
using UnityEngine;

public class Tentacle : Enemy
{
    private EyeBoss owner;

    [Header("Base Settings")]
    [SerializeField] private float toDestroy = 0.3f; // 공격 후 소멸 대기 시간

    // 보스에게서 받아올 변수들
    private float attackWaitTime;
    private float animationLength;

    // 초기화 함수: 스폰 직후 값 설정
    public void Setup(EyeBoss owner, float damageAmount, float waitTime)
    {
        this.owner = owner;
        this.damage = damageAmount;
        this.attackWaitTime = waitTime;

        if (this.owner != null)
        {
            this.owner.RegisterTentacle(gameObject);
        }

        // 애니메이션 길이 미리 계산
        if (animator != null)
        {
            // Animator가 활성화된 직후라 0번 레이어 정보를 못 가져올 수 있으므로 체크
            if (animator.runtimeAnimatorController != null)
            {
                // 보통 애니메이터 초기화에 1프레임이 필요할 수 있어 안전하게 고정값 혹은 클립 검색 사용
                // 여기서는 기존 로직을 유지하되 안전장치 추가
                animationLength = 1.0f; // 기본값

                // 실제 클립 길이 탐색 (더 정확함)
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                {
                    if (clip.name.Contains("Attack") || clip.name.Contains("attack"))
                    {
                        animationLength = clip.length;
                        break;
                    }
                }
            }
        }
    }

    // ✨ 보스가 설정을 마친 후 호출하여 공격 시작
    public void BeginAttack()
    {
        StartCoroutine(AttackRoutine());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Train train = collision.GetComponent<Train>();
            if (train != null)
            {
                train.TakeDamage(damage);
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        // 1. 공격 예고 대기 (설정된 시간만큼)
        yield return new WaitForSeconds(attackWaitTime);

        // 2. 공격 애니메이션 실행
        if (animator != null) animator.SetTrigger("attack");

        // 3. 판정 시간 대기 (애니메이션 길이)
        yield return new WaitForSeconds(animationLength);

        // 4. 소멸 대기
        yield return new WaitForSeconds(toDestroy);

        Destroy(gameObject);
    }

    public override void TakeDamage(float damageAmount)
    {
        if (!isAlive) return;

        currentHP -= damageAmount;
        StartCoroutine(HitEffect());

        if (currentHP <= 0)
        {
            Instantiate(killParticle, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }

    protected override float CalculateCalibratedHP()
    {
        if (GameManager.Instance == null || PoolManager.instance == null) return hp;

        int gameTimeMin = (int)(GameManager.Instance.gameTime / 60.0f);
        float increaseRate = PoolManager.instance.hpIncrease / 100.0f;
        float calibratedValue = 1.0f + (gameTimeMin * increaseRate);
        float eventDebuff = 1.0f + (PoolManager.instance.eventDebuff / 100.0f);

        calibratedMaxHP = hp * calibratedValue * eventDebuff;
        return calibratedMaxHP;
    }

    private void OnDestroy()
    {
        if (owner != null)
        {
            owner.UnregisterTentacle(gameObject);
        }
    }

    // 보스에게 총 수명(라이프사이클 시간)을 알려줌
    public float GetTotalDuration()
    {
        // 공격대기 + 애니메이션 + 소멸대기
        // (BeginAttack 호출 시점부터 파괴될 때까지의 시간)
        return attackWaitTime + (animationLength > 0 ? animationLength : 1.0f) + toDestroy;
    }
}