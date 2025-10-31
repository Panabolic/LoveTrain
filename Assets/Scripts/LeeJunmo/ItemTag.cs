using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Tag", menuName = "Game/Tags")]
public class ItemTag : ScriptableObject
{
    public string tagName;
    public Color tagColor;
}
