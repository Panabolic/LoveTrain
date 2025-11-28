using UnityEngine;

public class LaserSpriteStrategy : IWeaponStrategy
{
    private Gun gun;
    private GameObject laserInstance;
    private LaserBeamSprite laserScript;

    private GameObject laserPrefab;

    public void Initialize(Gun gunController, GunStats stats)
    {
        this.gun = gunController;
        this.laserPrefab = stats.laserPrefab;

        if (laserPrefab != null)
        {
            // 레이저 생성
            laserInstance = Object.Instantiate(laserPrefab, gun.FirePoint);
            laserInstance.transform.localPosition = Vector3.zero;
            laserInstance.transform.localRotation = Quaternion.identity;

            laserScript = laserInstance.GetComponent<LaserBeamSprite>();

            laserInstance.SetActive(false);
        }
    }

    public void Process(bool isTriggerHeld)
    {
        if (laserInstance == null || laserScript == null) return;

        if (isTriggerHeld)
        {
            // [누르고 있음] 
            // 꺼져있다면 켜서 발사 시작 (Start 애니메이션 재생됨)
            if (!laserInstance.activeSelf)
            {
                laserInstance.SetActive(true);
            }

            // 데미지 갱신 (켜져있는 동안 계속)
            laserScript.Init(gun.CurrentStats.damage);
        }
        else
        {
            // [뗐음]
            // ✨ 즉시 끄지 않고, 종료 신호를 보냄
            if (laserInstance.activeSelf)
            {
                laserScript.StopFiring();
            }
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