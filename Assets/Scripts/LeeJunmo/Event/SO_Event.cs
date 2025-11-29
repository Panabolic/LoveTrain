using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Event System/Events", order = int.MaxValue)]
public class SO_Event : ScriptableObject
{
    [System.Serializable]
    public struct Selection
    {
        [TextArea(2, 2)]
        public string selectionText;
        [TextArea(2, 2)]
        public string selectionUnderText;

        [Tooltip("이 선택지를 골랐을 때 발동할 실제 이벤트 로직 (GameEventSO)")]
        public GameEventSO eventToTrigger; // [핵심 연결고리]
    }

    public string EventTitle;
    [TextArea(25, 25)]
    public string EventText;
    public List<Selection> Selections;
}