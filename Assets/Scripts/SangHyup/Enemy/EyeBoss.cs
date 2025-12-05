using System.Collections;
using UnityEngine;

public class EyeBoss : Boss
{
    [Header("Eye Boss Specification")]
    [SerializeField] private float waitTimeBetweenPatterns      = 2.0f;
    [Range(0.1f, 1.0f)]
    [SerializeField] private float instantKillPatternThreshold  = 0.2f; // 체력 비율 임계값
    [SerializeField] private float patternWaitTime              = 7.0f;

    [Header("Reference")]
    [SerializeField] private GameObject     Tentacle;
    [SerializeField] private Transform[]    leftTentaclePoints;
    [SerializeField] private Transform[]    rightTentaclePoints;
    [SerializeField] private Transform[]    middleTentaclePoints;

    private bool canAttack = true;

    private void Update()
    {
        if (!isAlive || !canAttack) return;

        int patternIndex = Random.Range(0, 2);

        if (patternIndex == 0)
            StartCoroutine(AttackPattern1());
        if (patternIndex == 1)
            StartCoroutine(AttackPattern2());
    }

    public override void TakeDamage(float damageAmount)
    {
        base.TakeDamage(damageAmount);

        // 체력 비율 계산
        float currentHpRatio = currentHP / calibratedHP;

        // 체력 비율이 임계값 이하로 떨어지면 즉사 패턴 발동
        if (currentHpRatio <= instantKillPatternThreshold)
            damage = 10000f;
    }

    private IEnumerator AttackPattern1()
    {
        int leftOrRight = Random.Range(0, 2);

        switch(leftOrRight)
        {
            case 0:
                // 왼쪽 공격 패턴
                canAttack = false;

                foreach (Transform spawnPoint in leftTentaclePoints)
                {
                    Instantiate(Tentacle, spawnPoint.position, Quaternion.identity);
                }

                yield return new WaitForSeconds(patternWaitTime);

                canAttack = true;
                break;
            case 1:
                // 오른쪽 공격 패턴
                canAttack = false;

                foreach (Transform spawnPoint in rightTentaclePoints)
                {
                    Instantiate(Tentacle, spawnPoint.position, Quaternion.identity);
                }

                yield return new WaitForSeconds(patternWaitTime);

                canAttack = true;
                break;
        }

        yield return null;

    }

    private IEnumerator AttackPattern2()
    {
        // 왼쪽 공격 패턴
        canAttack = false;

        foreach (Transform spawnPoint in middleTentaclePoints)
        {
            Instantiate(Tentacle, spawnPoint.position, Quaternion.identity);
        }

        yield return new WaitForSeconds(patternWaitTime);

        canAttack = true;

        yield return null;
    }
}
