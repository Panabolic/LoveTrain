using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Components
    protected SpriteRenderer    sprite;
    protected Collider2D        collision;
    protected Animator          animator;
    protected Material          material;

    [Header("Enemy Specification")]
    [Tooltip("기본 체력")]
    [SerializeField] protected float hp;
    [Tooltip("대미지(km/h 단위)")]
    [SerializeField] protected float damage;
    [Tooltip("주는 경험치량")]
    [SerializeField] protected float exp;
    [Tooltip("피격 이펙트 지속시간")]
    [SerializeField] protected float hitEffectDuration = 0.05f;

    protected float calibratedMaxHP;
    protected float currentHP;
    protected bool isAlive = true;

    // ✨ [추가] 화면 진입 여부 체크 변수
    protected bool hasEnteredScreen = false;

    private Color originalColor;

    protected float deathToDeactive;

    // Target Components
    protected Rigidbody2D targetRigid;
    protected TrainLevelManager levelManager;

    // ✨ [추가] 외부에서 타겟팅 가능한지 확인하는 프로퍼티
    // (살아있고 + 화면에 들어온 적만 타겟팅 가능)
    public bool IsTargetable => isAlive && hasEnteredScreen;

    protected virtual void Awake()
    {
        // Get Components
        sprite = GetComponent<SpriteRenderer>();
        collision = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        material = sprite.material;

        // variable init
        originalColor = sprite.color;

        // Get Target Components
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            targetRigid = playerObj.GetComponent<Rigidbody2D>();
            levelManager = playerObj.GetComponent<TrainLevelManager>();
        }
    }

    protected virtual void OnEnable()
    {
        // init Default value
        currentHP = CalculateCalibratedHP();
        isAlive = true;
        sprite.enabled = true;
        sprite.color = originalColor;

        // ✨ [중요] 재활용될 때 화면 진입 여부 초기화 (다시 화면 밖에서 시작하므로)
        hasEnteredScreen = false;

        // Layer 변경
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        if (PoolManager.instance != null)
        {
            PoolManager.instance.RegisterEnemy(this);
        }
    }

    protected virtual void Start()
    {
        if (animator != null)
        {
            AnimationClip[] animationClips = animator.runtimeAnimatorController.animationClips;
            foreach (AnimationClip clip in animationClips)
            {
                if (clip.name == "Die") deathToDeactive = clip.length;
            }
        }
    }

    // ✨ [추가] 매 프레임 화면에 들어왔는지 체크
    protected virtual void Update()
    {
        // 아직 화면에 안 들어왔고, 살아있다면 체크
        if (!hasEnteredScreen && isAlive)
        {
            CheckScreenEntry();
        }
    }

    // ✨ 화면 진입 확인 로직
    // ✨ [수정] 화면 진입 확인 로직 (Pivot 점 기준 -> 영역(Bounds) 겹침 기준)
    private void CheckScreenEntry()
    {
        if (Camera.main == null) return;

        // 1. 카메라의 월드 공간 영역(Bounds) 계산
        float camHeight = Camera.main.orthographicSize * 2f;
        float camWidth = camHeight * Camera.main.aspect;
        Vector3 camPos = Camera.main.transform.position;

        // 중심은 카메라 위치, 크기는 화면 크기 (Z축은 2D라 넉넉하게 잡음)
        Bounds camBounds = new Bounds(new Vector3(camPos.x, camPos.y, 0), new Vector3(camWidth, camHeight, 1000f));

        // 2. 적의 영역(Collider 또는 Sprite) 가져오기
        Bounds enemyBounds;

        // 우선순위: 콜라이더 > 스프라이트 > 점
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
            // 둘 다 없으면 기존 방식(점) 사용 (예외 처리)
            Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
            if (viewPos.x >= 0f && viewPos.x <= 1f && viewPos.y >= 0f && viewPos.y <= 1f)
                hasEnteredScreen = true;
            return;
        }

        // 3. 교차 검사 (조금이라도 겹치면 True)
        if (camBounds.Intersects(enemyBounds))
        {
            hasEnteredScreen = true;
            // Debug.Log($"{name} 발가락이라도 화면에 들어옴! 타겟팅 가능");
        }
    }

    protected virtual float CalculateCalibratedHP()
    {
        calibratedMaxHP = hp;
        return calibratedMaxHP;
    }

    public virtual void TakeDamage(float damageAmount)
    {
        // ✨ [수정] 죽었거나, 아직 화면에 한 번도 안 들어왔으면 데미지 무시
        if (!isAlive || !hasEnteredScreen) return;

        currentHP -= damageAmount;

        StartCoroutine(HitEffect());

        SoundEventBus.Publish(SoundID.Enemy_Hit);

        if (currentHP <= 0)
            StartCoroutine(Die());
    }

    protected IEnumerator HitEffect()
    {
        material.SetInt("_isHit", 1);

        yield return new WaitForSeconds(hitEffectDuration);

        material.SetInt("_isHit", 0);
    }
 
    protected virtual IEnumerator Die()
    {
        isAlive = false;

        // Layer 변경
        gameObject.layer = LayerMask.NameToLayer("DeadEnemy");

        // 임시 색깔
        sprite.color = Color.red;
        animator.SetTrigger("die");
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