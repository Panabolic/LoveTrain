using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Revolver : MonoBehaviour, IInstantiatedItem
{
    //아이템 SO데이터 저장 변수
    private Revolver_SO itemData;

    private SpriteRenderer spriteRenderer;
    private Animator animator; // [추가]

    [SerializeField]
    private GameObject muzzle;

    // [추가] 발사 간격 (SO에서 가져옴)
    private float timeBetweenShots;

    // 타이머 및 상태 변수
    private float fireTimer = 0f;
    private bool isFiring = false; // [추가] 현재 발사 중인지 확인

    //현재 스탯
    private int currentDamage;
    private int currentBulletNum;
    private float currentCooldown;

    private void Awake()
    {
        // 컴포넌트를 미리 찾아둡니다.
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>(); // [추가]
    }

    /// <summary>
    /// [추가] OnEquip에서 SO가 호출할 초기화 함수
    /// </summary>
    public void Initialize(Revolver_SO so)
    {
        this.itemData = so;
    }

    private void Update()
    {
        if (itemData == null) return;

        // [핵심 변경] 발사 중(isFiring)일 때는 쿨타임 계산을 멈춤!
        if (isFiring) return;

        fireTimer -= Time.deltaTime;

        if (fireTimer <= 0f)
        {
            // 발사 시작! (코루틴 호출)
            StartCoroutine(FireRoutine());
        }
    }
    /// <summary>
    /// 연사 딜레이를 포함한 발사 코루틴
    /// </summary>
    private IEnumerator FireRoutine()
    {
        isFiring = true; // 발사 상태 시작 (Update 멈춤)

        for (int i = 0; i < currentBulletNum; i++)
        {
            // 1. 총알 생성 및 발사
            CreateBullet();

            // 2. 마지막 발이 아니라면 딜레이 대기
            if (i < currentBulletNum - 1)
            {
                yield return new WaitForSeconds(timeBetweenShots);
            }
        }

        // 3. 발사가 모두 끝남!
        isFiring = false; // 발사 상태 해제

        // [핵심] 발사가 '끝난 시점'부터 쿨타임 적용
        fireTimer = currentCooldown;
    }

    private void CreateBullet()
    {
        Vector3 spawnPos = muzzle != null ? muzzle.transform.position : transform.position;

        GameObject bulletObj = Instantiate(itemData.BulletPrefab, spawnPos, Quaternion.identity);
        RevolverBullet bulletScript = bulletObj.GetComponent<RevolverBullet>();

        if (bulletScript != null)
        {
            bulletScript.Init(currentDamage);
            bulletScript.CanHit();
        }
    }

    public void UpgradeInstItem(ItemInstance instance)
    {
        if (itemData == null) return;

        int levelIndex = instance.currentUpgrade - 1;

        // SO 데이터로 이 MonoBehaviour의 스탯을 갱신
        this.currentDamage = itemData.damageByLevel[levelIndex];
        this.currentBulletNum = itemData.bulletNumByLevel[levelIndex];
        this.currentCooldown = itemData.cooldownByLevel[levelIndex];

        // [추가] 발사 간격도 가져옴 (SO에 변수 추가했다고 가정)
        this.timeBetweenShots = itemData.timeBetweenShots;
            
        if (animator != null && itemData.controllersByLevel != null &&
            levelIndex >= 0 && levelIndex < itemData.controllersByLevel.Length)
        {
            RuntimeAnimatorController newController = itemData.controllersByLevel[levelIndex];  

            if (newController != null)
            {
                // 애니메이터 컨트롤러를 교체합니다.
                this.animator.runtimeAnimatorController = newController;
                // (이 컨트롤러의 Entry State가 올바른 스프라이트를 설정해야 함)
                return; // 컨트롤러를 교체했으면 스프라이트 교체는 건너뜀
            }
        }

        // 우선순위 2: (컨트롤러가 없거나 지정 안됐을 때) '정적 스프라이트'가 지정되어 있는가?
        if (spriteRenderer != null && itemData.spritesByLevel != null &&
            levelIndex >= 0 && levelIndex < itemData.spritesByLevel.Length)
        {
            Sprite newSprite = itemData.spritesByLevel[levelIndex];
            if (newSprite != null)
            {
                this.spriteRenderer.sprite = newSprite;
            }
        }

    }
}
