using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Components (Null일 수 있음)
    protected SpriteRenderer sprite;
    protected Collider2D collision;
    protected Animator animator;
    protected Material material;

    [Header("Enemy Specification")]
    [SerializeField] protected float hp;
    [SerializeField] protected float damage;
    [SerializeField] protected float exp;
    [SerializeField] protected float hitEffectDuration = 0.05f;

    protected float calibratedMaxHP;
    protected float currentHP;
    protected bool isAlive = true;

    protected bool hasEnteredScreen = false;
    private Color originalColor;
    protected float deathToDeactive;

    // Target Components
    protected Rigidbody2D targetRigid;
    protected TrainLevelManager levelManager;

    public bool IsTargetable => isAlive && hasEnteredScreen;

    protected virtual void Awake()
    {
        // ✨ [수정] 컴포넌트 가져오기 (없으면 null로 둠)
        sprite = GetComponent<SpriteRenderer>();
        collision = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();

        // ✨ [수정] 스프라이트가 있을 때만 재질 가져오기
        if (sprite != null)
        {
            material = sprite.material;
            originalColor = sprite.color;
        }

        // 플레이어 찾기 (이건 공통이므로 유지)
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            targetRigid = playerObj.GetComponent<Rigidbody2D>();
            levelManager = playerObj.GetComponent<TrainLevelManager>();
        }
    }

    protected virtual void OnEnable()
    {
        currentHP = CalculateCalibratedHP();
        isAlive = true;

        // ✨ [수정] null 체크 추가
        if (sprite != null)
        {
            sprite.enabled = true;
            sprite.color = originalColor;
        }

        hasEnteredScreen = false;
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        if (PoolManager.instance != null)
        {
            PoolManager.instance.RegisterEnemy(this);
        }
    }

    protected virtual void Start()
    {
        // ✨ [수정] null 체크 추가
        if (animator != null)
        {
            AnimationClip[] animationClips = animator.runtimeAnimatorController.animationClips;
            foreach (AnimationClip clip in animationClips)
            {
                if (clip.name == "Die") deathToDeactive = clip.length;
            }
        }
    }

    protected virtual void Update()
    {
        if (!hasEnteredScreen && isAlive)
        {
            CheckScreenEntry();
        }
    }

    private void CheckScreenEntry()
    {
        if (Camera.main == null) return;

        float camHeight = Camera.main.orthographicSize * 2f;
        float camWidth = camHeight * Camera.main.aspect;
        Vector3 camPos = Camera.main.transform.position;
        Bounds camBounds = new Bounds(new Vector3(camPos.x, camPos.y, 0), new Vector3(camWidth, camHeight, 1000f));

        Bounds enemyBounds;

        // ✨ [수정] 있는 컴포넌트로 영역 계산 (Collider 우선 -> Sprite -> 점)
        if (collision != null)
        {
            enemyBounds = collision.bounds;
        }
        else if (sprite != null)
        {
            enemyBounds = sprite.bounds;
        }
        else
        {
            // 둘 다 없으면(예: CreditEnemy 초기화 전) 점 기준으로 체크
            Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
            if (viewPos.x >= 0f && viewPos.x <= 1f && viewPos.y >= 0f && viewPos.y <= 1f)
                hasEnteredScreen = true;
            return;
        }

        if (camBounds.Intersects(enemyBounds))
        {
            hasEnteredScreen = true;
        }
    }

    protected virtual float CalculateCalibratedHP()
    {
        calibratedMaxHP = hp;
        return calibratedMaxHP;
    }

    public virtual void TakeDamage(float damageAmount)
    {
        if (!isAlive || !hasEnteredScreen) return;

        currentHP -= damageAmount;

        // ✨ [수정] HitEffect는 material이 있을 때만 실행
        if (material != null) StartCoroutine(HitEffect());

        SoundEventBus.Publish(SoundID.Enemy_Hit);

        if (currentHP <= 0)
            StartCoroutine(Die());
    }

    protected IEnumerator HitEffect()
    {
        if (material == null) yield break; // 방어 코드

        material.SetInt("_isHit", 1);
        yield return new WaitForSeconds(hitEffectDuration);
        material.SetInt("_isHit", 0);
    }

    protected virtual IEnumerator Die()
    {
        isAlive = false;
        gameObject.layer = LayerMask.NameToLayer("DeadEnemy");

        // ✨ [수정] null 체크 후 실행
        if (sprite != null) sprite.color = Color.red;
        if (animator != null) animator.SetTrigger("die");

        SoundEventBus.Publish(SoundID.Enemy_Die);

        if (levelManager != null) levelManager.GainExperience(exp);

        Inventory inventory = levelManager?.GetComponent<Inventory>();
        if (inventory != null)
        {
            inventory.ProcessKillEvent(this.gameObject);
        }

        yield return new WaitForSeconds(deathToDeactive);
    }

    public virtual void DespawnWithoutExp()
    {
        if (!gameObject.activeInHierarchy) return;
        isAlive = false;
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    public bool GetIsAlive() { return isAlive; }

    protected virtual void OnDisable()
    {
        if (PoolManager.instance != null)
        {
            PoolManager.instance.UnregisterEnemy(this);
        }
    }
}