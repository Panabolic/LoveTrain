using UnityEngine;

[CreateAssetMenu(fileName = "RegenRing", menuName = "Items/RegenRing")]
public class RegenRingSO : Item_SO
{
    public int healAmount;

    public override void OnCooldownComplete(GameObject user)
    {
        
    }
}