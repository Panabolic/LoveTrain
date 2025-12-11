using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Components
    protected SpriteRenderer sprite;
    protected Collider2D collision;
    protected Animator animator;

    [Header("Enemy Specification")]
    [Tooltip("기본 체력")]
    [SerializeField] protected float hp;
    [Tooltip("대미지(km/h 단위)")]
    [SerializeField] protected float damage;
    [Tooltip("주는 경험치량")]
    [SerializeField] protected float exp;

    protected float calibratedHP;
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
    private void CheckScreenEntry()
    {
        if (Camera.main == null) return;

        // 월드 좌표 -> 뷰포트 좌표 변환 (0,0 ~ 1,1 사이면 화면 안)
        Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);

        // 약간의 여유(0 ~ 1)를 두고 화면 안에 들어왔는지 판단
        if (viewPos.x >= 0f && viewPos.x <= 1f && viewPos.y >= 0f && viewPos.y <= 1f)
        {
            hasEnteredScreen = true;
            // Debug.Log($"{name} 화면 진입! 타겟팅 가능");
        }
    }

    protected virtual float CalculateCalibratedHP()
    {
        calibratedHP = hp;
        return calibratedHP;
    }

    public virtual void TakeDamage(float damageAmount)
    {
        // ✨ [수정] 죽었거나, 아직 화면에 한 번도 안 들어왔으면 데미지 무시
        if (!isAlive || !hasEnteredScreen) return;

        currentHP -= damageAmount;
        SoundEventBus.Publish(SoundID.Enemy_Hit);
        if (currentHP <= 0)
            StartCoroutine(Die());
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