using System.Collections.Generic;
using UnityEngine;

// 이 코드는 프로젝트 어디에서든 EventDatabase 에셋 파일을 만들 수 있게 해줍니다.
[CreateAssetMenu(fileName = "EventDatabase", menuName = "Scriptable Object/Event Database")]
public class EventDatabase : ScriptableObject
{
    // [1] 모든 이벤트를 저장하는 리스트
    [SerializeField]
    private List<SO_Event> eventList = new List<SO_Event>();

    /// <summary>
    /// [2] 목록에서 무작위 이벤트를 하나 반환합니다.
    /// </summary>
    /// <returns>랜덤으로 선택된 SO_Event. 목록이 비어있으면 null을 반환합니다.</returns>
    public SO_Event GetRandomEvent()
    {
        if (eventList == null || eventList.Count == 0)
        {
            Debug.LogWarning("EventDatabase에 이벤트가 비어있습니다!");
            return null;
        }

        int randomIndex = Random.Range(0, eventList.Count);
        return eventList[randomIndex];
    }

    /// <summary>
    /// [3] 지정된 인덱스(순번)의 이벤트를 반환합니다.
    /// </summary>
    /// <param name="index">가져올 이벤트의 인덱스</param>
    /// <returns>해당 인덱스의 SO_Event. 인덱스가 범위를 벗어나면 null을 반환합니다.</returns>
    public SO_Event GetEvent(int index)
    {
        if (eventList == null || index < 0 || index >= eventList.Count)
        {
            Debug.LogError($"잘못된 이벤트 인덱스({index})를 요청했습니다.");
            return null;
        }

        return eventList[index];
    }

    /// <summary>
    /// 전체 이벤트 목록의 개수를 반환합니다.
    /// </summary>
    public int GetEventCount()
    {
        if (eventList == null) return 0;
        return eventList.Count;
    }
}