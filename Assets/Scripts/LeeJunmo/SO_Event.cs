using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[CreateAssetMenu(menuName = "Scriptable Object/Events", order = int.MaxValue)]
public class SO_Event : ScriptableObject 
{
    [System.Serializable]
    public struct Selection
    {
        [TextArea(2, 2)]
        public string selectionText;
        [TextArea(2, 2)]
        public string selectionUnderText; 
        [TextArea(2, 2)]
        public string selectionEndText;
        //æ∆¿Ã≈€
        public int addSpeed;
    }

    public Sprite EventSprite;
    public string EventTitle;
    [TextArea(25,25)]
    public string EventText;
    public List<Selection> Selections;
  
}
