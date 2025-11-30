using UnityEngine;

public class LaserSpriteStrategy : IWeaponStrategy
{
    private Gun gun;
    private GameObject laserInstance;
    private LaserBeamSprite laserScript;
    private GameObject laserPrefab;

    // --- 레이저 전용 스탯 ---
    private float maxDuration;
    private float cooldownTime;

    // --- 내부 타이머 ---
    private float currentDurationTimer = 0f;
    private float currentCooldownTimer = 0f;
    private bool isFiring = false;

    public void Initialize(Gun gunController, GunStats stats)
    {
        this.gun = gunController;

        // ✨ [핵심 수정 1] 끊김 방지
        // 이미 레이저 인스턴스가 있고, 프리팹이 변경된 게 아니라면(단순 스탯 변화라면)
        // 오브젝트를 파괴하지 않고 그대로 유지합니다.
        if (laserInstance != null && this.laserPrefab == stats.laserPrefab)
        {
            return;
        }

        this.laserPrefab = stats.laserPrefab;

        // 레이저 오브젝트 생성 (초기화)
        if (laserPrefab != null)
        {
            if (laserInstance != null) Object.Destroy(laserInstance);

            laserInstance = Object.Instantiate(laserPrefab, gun.FirePoint);
            laserInstance.transform.localPosition = Vector3.zero;
            laserInstance.transform.localRotation = Quaternion.identity;

            laserScript = laserInstance.GetComponent<LaserBeamSprite>();
            laserInstance.SetActive(false);
        }
    }

    public void SetLaserStats(float duration, float cooldown)
    {
        this.maxDuration = duration;
        this.cooldownTime = cooldown;
    }

    public void Process(bool isTriggerHeld)
    {
        if (laserInstance == null || laserScript == null) return;

        // 1. 쿨타임 처리
        if (currentCooldownTimer > 0)
        {
            currentCooldownTimer -= Time.deltaTime;
            return;
        }

        // 2. 발사 로직
        if (isTriggerHeld)
        {
            if (!isFiring)
            {
                // 발사 시작 (이때 스탯이 결정됨)
                StartFiring();
            }
            else
            {
                // 발사 중
                currentDurationTimer += Time.deltaTime;

                if (currentDurationTimer >= maxDuration)
                {
                    StopFiringAndStartCooldown();
                }

                // ✨ [핵심 수정 2] 실시간 스탯 갱신 제거
                // 여기 있던 laserScript.Init(...) 호출을 삭제했습니다.
                // 이제 성경 범위에 들어가서 공속이 변해도, 쏘고 있던 레이저는 
                // StartFiring 시점의 스탯을 그대로 유지합니다.
            }
        }
        else
        {
            if (isFiring)
            {
                StopFiringAndStartCooldown();
            }
        }
    }

    private void StartFiring()
    {
        isFiring = true;
        currentDurationTimer = 0f;

        if (!laserInstance.activeSelf)
        {
            laserInstance.SetActive(true);
        }

        // ✨ 발사하는 순간의 스탯을 주입 (이 값이 발사 끝날 때까지 유지됨)
        laserScript.Init(gun.CurrentStats.damage, gun.CurrentStats.fireRate);
    }

    private void StopFiringAndStartCooldown()
    {
        isFiring = false;
        currentDurationTimer = 0f;

        if (Time.timeScale == 0)
        {
            currentCooldownTimer = 0f;
        }
        else
        {
            currentCooldownTimer = cooldownTime;
        }

        if (laserInstance.activeSelf)
        {
            laserScript.StopFiring();
        }
    }

    public void Unequip()
    {
        if (laserInstance != null)
        {
            Object.Destroy(laserInstance);
        }
    }
}