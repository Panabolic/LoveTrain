using NUnit.Framework.Interfaces;
using UnityEngine;

public class LaserSpriteStrategy : IWeaponStrategy
{
    private Gun gun;
    private GameObject laserInstance; // 생성된 레이저 오브젝트
    private LaserBeamSprite laserScript; // 레이저 로직 스크립트

    private GameObject laserPrefab;

    public void Initialize(Gun gunController, GunStats stats)
    {
        this.gun = gunController;
        this.laserPrefab = stats.laserPrefab;

        // 1. 총구(FirePoint)의 '자식'으로 레이저 생성
        if (laserPrefab != null)
        {
            laserInstance = Object.Instantiate(laserPrefab, gun.firePoint);
            laserInstance.transform.localPosition = Vector3.zero;
            laserInstance.transform.localRotation = Quaternion.identity;

            laserScript = laserInstance.GetComponent<LaserBeamSprite>();

            // 처음엔 꺼둠
            laserInstance.SetActive(false);
        }
    }

    public void Process(bool isTriggerHeld)
    {
        if (laserInstance == null) return;

        if (isTriggerHeld)
        {
            // [누르고 있음] 레이저 켜기
            if (!laserInstance.activeSelf)
            {
                laserInstance.SetActive(true);

                // 켤 때마다 최신 데미지 업데이트
                if (laserScript != null)
                {
                    laserScript.Init(gun.CurrentStats.damage);
                }
            }
        }
        else
        {
            // [뗐음] 레이저 끄기
            if (laserInstance.activeSelf)
            {
                laserInstance.SetActive(false);
            }
        }
    }

    public void Unequip()
    {
        // 무기 교체 시 생성해둔 레이저 삭제
        if (laserInstance != null)
        {
            Object.Destroy(laserInstance);
        }
    }
}