using NUnit.Framework.Interfaces;
using UnityEngine;

public interface IWeaponStrategy
{
    // 무기 초기화 (총알/레이저 프리팹 설정 등)
    void Initialize(Gun gunController, GunStats stats);

    // 매 프레임 호출 (발사 버튼을 누르고 있는지 여부 전달)
    void Process(bool isTriggerHeld);

    // 무기 교체 시 정리 (레이저 끄기 등)
    void Unequip();
}