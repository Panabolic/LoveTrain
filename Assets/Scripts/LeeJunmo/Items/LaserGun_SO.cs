using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LaserGun", menuName = "Items/LaserGun")]
public class LaserGun_SO : Item_SO
{
    // ✨ [추가] 데미지 비율 설정 (기본값 0.37f 약 1/2.7)
    [Header("밸런스 설정")]
    [Tooltip("기본 총기 데미지 대비 레이저 틱당 데미지 비율 (예: 0.37 = 37%)")]
    public float damageRatio = 0.34f;

    [Header("레이저 세부 설정")]
    public float[] durationByLevel = { 1f, 3f, 5f };
    public float[] tickRateByLevel = { 0.07f, 0.07f, 0.07f };
    public float[] cooldownByLevel = { 0.6f, 0.6f, 0.6f };
    public float[] laserScale = { 1f, 1.5f, 2f };

    public GameObject LaserProjectilePrefab;

    [Tooltip("교체할 거치대 이미지 (선택)")]
    public Sprite HolderSprite;

    internal override ItemDefinition CreateRuntimeDefinition() => new ItemDefinition(this, CompositionDefinition.Weapon(this));

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, durationByLevel.Length - 1);
        return new Dictionary<string, string>
        {
            { "Duration", durationByLevel[index].ToString() },
            { "Cooldown", cooldownByLevel[index].ToString() }
        };
    }

}
