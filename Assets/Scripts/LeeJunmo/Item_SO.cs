using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Game/Items")]
public class Item_SO : ScriptableObject
{
    [Header("아이템 이름")]
    public string itemName;
    [Header("태그")]
    public List<ItemTag> tags;
    [Header("업그레이드 최대 횟수")]
    public int MaxUpgrade;
    [Header("아이템 설명")]
    public string itemScript;

}
