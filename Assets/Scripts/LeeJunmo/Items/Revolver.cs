using UnityEngine;
using System.Collections;

public class Revolver : MonoBehaviour, IInstantiatedItem
{
    private Revolver_SO itemData;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    [SerializeField] private GameObject muzzle;

    private int currentDamage;
    private int currentBulletNum;
    private float currentCooldown;
    private float timeBetweenShots;

    private float fireTimer = 0f;
    private bool isFiring = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    public void Initialize(Revolver_SO so)
    {
        this.itemData = so;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Boss) return;

        if (itemData == null || isFiring) return;

        fireTimer -= Time.deltaTime;

        if (fireTimer <= 0f)
        {
            StartCoroutine(FireRoutine());
        }
    }

    private IEnumerator FireRoutine()
    {
        isFiring = true;

        for (int i = 0; i < currentBulletNum; i++)
        {
            CreateBullet();
            if (i < currentBulletNum - 1) yield return new WaitForSeconds(timeBetweenShots);
        }

        isFiring = false;
        fireTimer = currentCooldown;
    }

    private void CreateBullet()
    {
        Vector3 spawnPos = muzzle != null ? muzzle.transform.position : transform.position;

        // 1. 풀에서 가져오기
        GameObject bulletObj = BulletPoolManager.Instance.Spawn(
            itemData.BulletPrefab,
            spawnPos,
            Quaternion.identity
        );

        // 2. [핵심] 생성 직후 '총구의 자식'으로 붙임 (같이 움직이도록)
        if (muzzle != null)
        {
            bulletObj.transform.SetParent(muzzle.transform);
            bulletObj.transform.localPosition = Vector3.zero; // 위치 정렬
            bulletObj.transform.localRotation = Quaternion.identity; // 회전 정렬
        }

        RevolverBullet bulletScript = bulletObj.GetComponent<RevolverBullet>();
        if (bulletScript != null)
        {
            // 3. 데이터 초기화 (CanHit은 호출하지 않음 -> 애니메이션 이벤트로 호출해야 함!)
            bulletScript.Init(currentDamage, itemData.BulletPrefab);
        }
    }

    public void UpgradeInstItem(ItemInstance instance)
    {
        if (itemData == null) return;
        int levelIndex = instance.currentUpgrade - 1;

        this.currentDamage = itemData.damageByLevel[levelIndex];
        this.currentBulletNum = itemData.bulletNumByLevel[levelIndex];
        this.currentCooldown = itemData.cooldownByLevel[levelIndex];
        this.timeBetweenShots = 0.2f;

        if (animator != null && itemData.controllersByLevel != null && levelIndex < itemData.controllersByLevel.Length)
        {
            var controller = itemData.controllersByLevel[levelIndex];
            if (controller != null) { this.animator.runtimeAnimatorController = controller; return; }
        }
        if (spriteRenderer != null && itemData.spritesByLevel != null && levelIndex < itemData.spritesByLevel.Length)
        {
            var sprite = itemData.spritesByLevel[levelIndex];
            if (sprite != null) this.spriteRenderer.sprite = sprite;
        }
    }
}