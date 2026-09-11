using UnityEngine;

[CreateAssetMenu(fileName = "Effect_ModifySpeed", menuName = "Event System/Effects/Modify Speed")]
public class Effect_ModifySpeed : GameEffectSO
{
    // [제거] public float speedChangeAmount; 

    public override string Execute(GameObject target, EffectParameters parameters)
    {
        // [수정] 'parameters'에서 데이터를 꺼내 씀
        float speedChange = parameters.floatValue;

        Train train = target.GetComponent<Train>();
        if (train == null) return null;

        train.ModifySpeed(speedChange);

        if (speedChange > 0)
            return EnglishLocalization.Format("result.speed_recover", "기차의 속도를 {0}만큼 회복했습니다.", speedChange);
        else if (speedChange < 0)
            return EnglishLocalization.Format("result.speed_loss", "기차의 속도가 {0}만큼 감소했습니다.", Mathf.Abs(speedChange));
        return null;
    }
}