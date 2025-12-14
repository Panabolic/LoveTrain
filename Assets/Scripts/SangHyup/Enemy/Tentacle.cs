using System; // ✨ Action 사용을 위해 필수
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

    // ✨ [추가] 공격 시 실행할 콜백 (보스의 사운드 함수)
    private Action onAttackCallback;

    // ✨ [수정] Setup에서 Action을 받아옵니다.
    public void Setup(EyeBoss owner, float damageAmount, float waitTime, Action onAttackCallback)
    {
        this.owner = owner;
        this.damage = damageAmount;
        this.attackWaitTime = waitTime;

        // 보스가 전달해준 사운드 재생 함수 저장
        this.onAttackCallback = onAttackCallback;

        if (this.owner != null)
        {
            this.owner.RegisterTentacle(gameObject);
        }

        // 애니메이션 길이 미리 계산
        if (animator != null)
        {
            if (animator.runtimeAnimatorController != null)
            {
                animationLength = 1.0f; // 기본값

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
        // 1. 공격 예고 대기
        yield return new WaitForSeconds(attackWaitTime);

        // 2. 공격 애니메이션 실행
        if (animator != null) animator.SetTrigger("attack");

        // ✨ [핵심] 보스에게 "나 공격했음!" 하고 알림
        // 보스는 이 신호를 받고 중복 체크 후 소리를 한 번만 재생함.
        // 만약 촉수가 죽어서 이 라인에 도달 못하면 자연스럽게 소리도 안 남.
        onAttackCallback?.Invoke();

        // 3. 판정 시간 대기
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
        SoundEventBus.Publish(SoundID.Enemy_Hit);

        if (currentHP <= 0)
        {
            Instantiate(killParticle, transform.position, Quaternion.identity);
            SoundEventBus.Publish(SoundID.Enemy_Die);
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

    public float GetTotalDuration()
    {
        return attackWaitTime + (animationLength > 0 ? animationLength : 1.0f) + toDestroy;
    }
}