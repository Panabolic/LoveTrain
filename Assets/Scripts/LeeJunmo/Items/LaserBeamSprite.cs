using UnityEngine;
using System.Collections.Generic;

public class LaserBeamSprite : MonoBehaviour
{
    [Header("설정")]
    public float damageInterval = 0.1f;

    private float damage;
    private float damageTimer;
    private bool isHitting = false; // 실제로 데미지를 줄 수 있는 상태인지 (애니메이션 이벤트로 제어)
    private bool isStopping = false; // 종료 애니메이션이 진행 중인지

    private Animator animator;
    private List<Enemy> enemiesInRange = new List<Enemy>();

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Init(float dmg)
    {
        this.damage = dmg;
    }

    private void OnEnable()
    {
        // 활성화 될 때 변수 초기화
        damageTimer = 0f;
        isHitting = false; // Start 애니메이션 중에는 아직 타격 불가
        isStopping = false;
        enemiesInRange.Clear();

        // (참고: 애니메이터의 Entry -> LaserStart로 자동 전환되도록 설정했다고 가정)
    }

    /// <summary>
    /// 외부(Strategy)에서 발사 중단을 요청할 때 호출
    /// </summary>
    public void StopFiring()
    {
        // 이미 종료 중이거나 꺼져있으면 무시
        if (isStopping || !gameObject.activeSelf) return;

        isStopping = true;
        isHitting = false;

        // ✨ [추가] 시간이 멈춰있다면(이벤트 중) 애니메이션 못 보니까 즉시 종료
        if (Time.timeScale == 0)
        {
            DeactivateSelf();
            return;
        }

        // 정상 속도라면 애니메이션 재생
        if (animator != null)
        {
            animator.SetTrigger("LaserEnd");
        }
        else
        {
            DeactivateSelf();
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;

        // ✨ [핵심] CanHit 이벤트가 들어왔을 때만 데미지 로직 실행
        if (isHitting)
        {
            damageTimer += Time.deltaTime;
            if (damageTimer >= damageInterval)
            {
                DealDamageToAll();
                damageTimer = 0f;
            }
        }
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null && !enemiesInRange.Contains(enemy))
        {
            enemiesInRange.Add(enemy);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null && enemiesInRange.Contains(enemy))
        {
            enemiesInRange.Remove(enemy);
        }
    }

    // ---------------------------------------------------------
    // ✨ [Animation Events] 애니메이션 클립에서 호출될 함수들
    // ---------------------------------------------------------

    // 1. LaserStart 애니메이션의 특정 프레임에서 호출 (타격 시작)
    public void AnimEvent_EnableHit()
    {
        isHitting = true;
    }

    // 2. LaserEnd 애니메이션의 마지막 프레임에서 호출 (오브젝트 비활성화)
    public void AnimEvent_Deactivate()
    {
        DeactivateSelf();
    }

    private void DeactivateSelf()
    {
        gameObject.SetActive(false);
    }
}