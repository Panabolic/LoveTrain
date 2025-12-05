using UnityEngine;
using System.Collections.Generic;

public class LaserBeamSprite : MonoBehaviour, IRicochetSource
{
    private float damageInterval = 0.1f; // 틱 주기 (Gun에서 받아옴)

    private float damage;
    private float damageTimer;
    private bool isHitting = false;
    private bool isStopping = false;

    [Header("도탄 설정")]
    [SerializeField] private GameObject ricochetPrefab; // 레이저가 도탄되면 나갈 총알
    private int currentBounceDepth = 0;

    // 레이저 도탄 쿨타임 (타겟별로 관리하면 좋지만, 간단히 전역 쿨타임)
    private float ricochetCooldown = 0.2f;
    private float lastRicochetTime = 0f;
    [SerializeField]
    private float ricochetBulletSpeed = 60f;

    private Animator animator;
    private List<Enemy> enemiesInRange = new List<Enemy>();

    public GameObject GetRicochetPrefab() => ricochetPrefab;
    public int GetBounceDepth() => currentBounceDepth;
    public void SetBounceDepth(int depth) => currentBounceDepth = depth;

    // ✨ [추가] 인터페이스 구현
    public float GetDamage() => damage; // 현재 데미지 반환

    // 만약 Inspector에 설정된 기본값을 쓰고 싶다면 별도 변수가 필요하겠지만,
    // 보통은 현재 날아가는 속도를 유지하는 것이 자연스럽습니다.
    public float GetSpeed() => ricochetBulletSpeed;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // Gun의 CurrentStats.fireRate(계산된 틱 주기)를 받아옵니다.
    public void Init(float dmg, float tickRate)
    {
        this.damage = dmg;
        ricochetPrefab.GetComponent<Projectile>().SetDamage(dmg / 2.7f);
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
                // ✨ [핵심] 타격 보고 (쿨타임 체크)
                if (Time.time >= lastRicochetTime + ricochetCooldown)
                {
                    // Player Inventory 찾기 (위 Projectile과 동일)
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        player.GetComponent<Inventory>()?.ProcessHitEvent(enemy.gameObject, this.gameObject);
                        lastRicochetTime = Time.time;
                    }
                }
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