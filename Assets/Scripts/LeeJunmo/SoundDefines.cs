// 사운드 식별자 (여기만 수정하면 됩니다)
using UnityEngine;

public enum SoundID
{
    None = 0,

    BGM_Title,
    BGM_Battle,
    BGM_Boss,
    BGM_Ending,
    BGM_Stop,

    UI_Click,
    UI_LevelUp,
    UI_BossWarning,
    UI_Event,
    UI_Option,

    Player_Shoot,
    Player_Hit,
    Player_Dying,
    Player_GameOver,
    
    Item_BeatingHeart,
    Item_Laser,
    Item_Bible,
    Item_GiantMaw,
    Item_Longinus,
    Item_HealingItem,
    Item_CrownOfThorn,
    Item_GunSlave,
    Item_Missile,
    Item_MissileBoom,
    Item_MeatGun,

    Enemy_Hit,
    Enemy_Die,
    Boss_Roar,
    Boss_Die,
    Item_GiantMaw2,
    UI_Typing,
    Boss_TrainBossSpawn,
    Item_LonginusSpawn,
    Boss_TentacleSpawn,
    Boss_TentacleAttack
}

// 사운드 데이터 세팅용 클래스 (인스펙터 노출용)
[System.Serializable]
public class SoundData
{
    public SoundID id;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    public bool loop = false;

    // ✨ [추가] 이 소리가 중요한 소리인지 여부 (체크하면 일반 SFX 풀이 꽉 차도 재생됨)
    public bool isImportant = false;
}