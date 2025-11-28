using UnityEngine;
using System.Collections.Generic;

public class LaserBeamSprite : MonoBehaviour
{
    private float damageInterval = 0.1f; // 틱 주기 (Gun에서 받아옴)

    private float damage;
    private float damageTimer;
    private bool isHitting = false;
    private bool isStopping = false;

    private Animator animator;
    private List<Enemy> enemiesInRange = new List<Enemy>();

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // Gun의 CurrentStats.fireRate(계산된 틱 주기)를 받아옵니다.
    public void Init(float dmg, float tickRate)
    {
        this.damage = dmg;
        // 0.05초 미만 등 너무 빨라지는 것 방지 (선택 사항)
        this.damageInterval = Mathf.Max(tickRate, 0.02f);
    }

    private void OnEnable()
    {
        damageTimer = 0f;
        isHitting = false;
        isStopping = false;
        enemiesInRange.Clear();
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;

        if (isHitting)
        {
            damageTimer += Time.deltaTime;

            // ✨ [핵심 수정] while문을 사용하여 프레임이 밀려도 틱 횟수 보장
            // 예: 렉이 걸려서 한 프레임에 0.5초가 지났고 주기가 0.1초라면, 5번 데미지 처리
            while (damageTimer >= damageInterval)
            {
                DealDamageToAll();

                // 0으로 초기화하지 않고 주기를 뺌 (남은 시간 보존 -> 정밀한 DPS 유지)
                damageTimer -= damageInterval;
            }
        }
    }

    public void StopFiring()
    {
        if (isStopping || !gameObject.activeSelf) return;

        isStopping = true;
        isHitting = false;

        if (Time.timeScale == 0)
        {
            DeactivateSelf();
            return;
        }

        if (animator != null) animator.SetTrigger("LaserEnd");
        else DeactivateSelf();
    }

    private void DealDamageToAll()
    {
        for (int i = enemiesInRange.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemiesInRange[i];
            if (enemy != null && enemy.gameObject.activeSelf)
            {
                enemy.TakeDamage(damage);
            }
            else
            {
                enemiesInRange.RemoveAt(i);
            }
        }
    }

    // ... (OnTriggerEnter2D, AnimEvent 등 나머지 코드는 그대로 유지) ...
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null && !enemiesInRange.Contains(enemy)) enemiesInRange.Add(enemy);
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null && enemiesInRange.Contains(enemy)) enemiesInRange.Remove(enemy);
    }
    public void AnimEvent_EnableHit() { isHitting = true; }
    public void AnimEvent_Deactivate() { DeactivateSelf(); }
    private void DeactivateSelf() { gameObject.SetActive(false); }
}