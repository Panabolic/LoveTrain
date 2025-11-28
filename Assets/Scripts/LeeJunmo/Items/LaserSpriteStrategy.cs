using UnityEngine;

public class LaserSpriteStrategy : IWeaponStrategy
{
    private Gun gun;
    private GameObject laserInstance;
    private LaserBeamSprite laserScript;
    private GameObject laserPrefab;

    private float maxDuration;
    private float cooldownTime;

    private float currentDurationTimer = 0f;
    private float currentCooldownTimer = 0f;
    private bool isFiring = false;

    public void Initialize(Gun gunController, GunStats stats)
    {
        this.gun = gunController;
        this.laserPrefab = stats.laserPrefab;

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

        // 쿨타임 처리
        if (currentCooldownTimer > 0)
        {
            currentCooldownTimer -= Time.deltaTime;
            return;
        }

        // 발사 로직
        if (isTriggerHeld)
        {
            if (!isFiring)
            {
                StartFiring();
            }
            else
            {
                currentDurationTimer += Time.deltaTime;

                if (currentDurationTimer >= maxDuration)
                {
                    StopFiringAndStartCooldown();
                }
                else
                {
                    // ✨ [중요] 매 프레임 갱신된 스탯(공속 버프 적용된 값)을 전달
                    // gun.CurrentStats.fireRate는 Gun.cs에서 계산된 '최종 틱 주기'입니다.
                    laserScript.Init(gun.CurrentStats.damage, gun.CurrentStats.fireRate);
                }
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
        // 발사 시작 시 스탯 적용
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
        if (laserInstance != null) Object.Destroy(laserInstance);
    }
}