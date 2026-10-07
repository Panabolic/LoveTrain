using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Revolver", menuName = "Items/Revolver")]
public class Revolver_SO : Item_SO
{
    [Header("리볼버 전용 데이터")]
    public int[] damageByLevel = { 200, 200, 500 };
    public int[] bulletNumByLevel = { 1, 2, 3 };
    public float[] cooldownByLevel = { 10f, 7f, 7f };

    [Header("발사 설정")]
    [Tooltip("연사 시 총알 사이의 간격 (초)")]
    public float timeBetweenShots = 0.2f; // [추가] 0.2초마다 한 발씩

    [Header("탄환")]
    public GameObject BulletPrefab;

    internal override ItemDefinition CreateRuntimeDefinition() => new ItemDefinition(this,
        CompositionDefinition.Attach<Revolver>(this, (logic, user, instance) => logic.Initialize(this), false));

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, damageByLevel.Length - 1);

        return new Dictionary<string, string>
        {
            { "Damage", damageByLevel[index].ToString() },
            { "BulletNum", bulletNumByLevel[index].ToString() },
            { "CoolTime", cooldownByLevel[index].ToString() }
        };
    }

}
