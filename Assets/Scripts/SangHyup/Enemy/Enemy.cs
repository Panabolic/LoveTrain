using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Components
    protected SpriteRenderer    sprite;
    protected Collider2D        collision;
    protected Animator          animator;

    [Header("Enemy Specification")]
    [Tooltip("기본 체력")]
    [SerializeField] protected float maxHP;
    [Tooltip("대미지(km/h 단위)")]
    [SerializeField] protected float damage;
    [Tooltip("주는 경험치량")]
    [SerializeField] protected float exp;

    protected float currentHP;
    protected bool  isAlive = true;
    
    protected float deathToDeactive; // 사망에서 비활성화까지 걸리는 시간

    // Target Components
    protected Rigidbody2D targetRigid;
    protected TrainLevelManager levelManager;

    protected virtual void Awake()
    {
        sprite      = GetComponent<SpriteRenderer>();
        collision   = GetComponent<Collider2D>();
        animator    = GetComponent<Animator>();

        // Get Target Components
        targetRigid     = GameObject.FindWithTag("Player").GetComponent<Rigidbody2D>();
        levelManager    = GameObject.FindWithTag("Player").GetComponent<TrainLevelManager>();
    }

    protected virtual void Start()
    {
        // deathToDeactive 값 세팅
        AnimationClip[] animationClips =  animator.runtimeAnimatorController.animationClips;

        foreach (AnimationClip clip in animationClips)
        {
            if (clip.name == "Die") deathToDeactive = clip.length;
        }
    }

    protected virtual void OnEnable()
    {
        // init Default value
        currentHP = maxHP;
        isAlive = true;
        sprite.enabled = true;
        sprite.color = Color.white;

        // Layer 변경
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        if (PoolManager.instance != null)
        {
            PoolManager.instance.RegisterEnemy(this);
        }
    }

    public virtual void TakeDamage(float damageAmount)
    {
        if (!isAlive) return;

        currentHP -= damageAmount;

        if (currentHP <= 0) StartCoroutine(Die());
    }

    protected virtual IEnumerator Die()
    {
        isAlive = false;

        // Layer 변경
        gameObject.layer = LayerMask.NameToLayer("DeadEnemy");

        // 임시 색깔
        sprite.color = Color.red;
        animator.SetTrigger("die");

        levelManager.GainExperience(exp);
        yield return new WaitForSeconds(deathToDeactive);
    }

    public virtual void DespawnWithoutExp()
    {
        // 이미 죽었거나 비활성화된 경우 패스
        if (!gameObject.activeInHierarchy) return;

        isAlive = false;
        StopAllCoroutines(); // 진행 중인 Die 코루틴 등 중단

        // 바로 비활성화 (필요하다면 연출 추가 가능)
        gameObject.SetActive(false);
    }

    public bool GetIsAlive() { return isAlive; }

    protected virtual void OnDisable()
    {
        // PoolManager 출석부에서 제거
        if (PoolManager.instance != null)
        {
            PoolManager.instance.UnregisterEnemy(this);
        }
    }
}
