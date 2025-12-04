using UnityEngine;

[System.Serializable] // 인스펙터에 보이도록
public class EffectParameters
{
    [Tooltip("Item Num, Upgrade Num")]
    public int intValue;

    [Tooltip("MonsterEnum?")]
    public int intValue2;

    [Tooltip("Speed, %, etc..")]
    public float floatValue;

    [Tooltip("Duration, etc..")]
    public float floatValue2;

    [Tooltip("Item SO")]
    public ScriptableObject soReference; // Item_SO

    [Tooltip("MonsterPrefab??")]
    public GameObject prefabReference;

    [Tooltip("FlyMob or GroundMob")]
    public bool boolValue;
}