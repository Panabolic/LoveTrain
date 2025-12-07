// 사운드 식별자 (여기만 수정하면 됩니다)
using UnityEngine;

public enum SoundID
{
    None = 0,

    // BGM
    BGM_Title,
    BGM_Battle,
    BGM_Boss,
    BGM_GameOver,

    // UI & System
    UI_Click,
    UI_LevelUp,
    UI_BossWarning,
    UI_Event,

    // Player
    Player_Shoot,
    Player_Hit,
    
    // Item
    Item_BeatingHeart,
    Item_Laser,
    Item_Bible,
    Item_GiantMaw,
    Item_Longinus,
    Item_HealingItem,
    Item_CrownOfThorn,
    Item_GunSlave,
    Item_Missile,
    Item_MIssileBoom,
    Item_MeatGun,

    // Enemy
    Enemy_Hit,
    Enemy_Die,
    Boss_Roar
}

// 사운드 데이터 세팅용 클래스 (인스펙터 노출용)
[System.Serializable]
public class SoundData
{
    public SoundID id;           // 사운드 이름(Enum)
    public AudioClip clip;       // 오디오 파일
    [Range(0f, 1f)] public float volume = 1f; // 개별 볼륨
    [Range(0.1f, 3f)] public float pitch = 1f; // 피치(음낮이)
    public bool loop = false;    // 반복 여부 (BGM 등)
}