using UnityEngine;
using System.Collections.Generic;
using System.Linq;
    
[CreateAssetMenu(fileName = "Effect_AcquireRandomItem", menuName = "Event System/Effects/Acquire Random Item")]
public class Effect_AcquireRandomItem : GameEffectSO
{
    [Header("이 로직에 필요한 에셋")]
    [Tooltip("전체 아이템 목록이 담긴 ItemDatabase 에셋")]
    public ItemDatabase itemDatabase; // (이 '로직 템플릿 에셋'에 연결 필요!)

    public override string Execute(GameObject target, EffectParameters parameters)
    {
        var wallet = target.GetComponent<TrainLevelManager>();
        if (wallet == null) return "Flesh reward unavailable.";
        int reward = 50 * Mathf.Max(1, parameters.intValue);
        wallet.AddFlesh(reward);
        return $"Flesh +{reward}";
    }
}
