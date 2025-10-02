using System.Collections.Generic;
using UnityEngine;

// �� �ڵ�� ������Ʈ ��𿡼��� EventDatabase ���� ������ ���� �� �ְ� ���ݴϴ�.
[CreateAssetMenu(fileName = "EventDatabase", menuName = "Scriptable Object/Event Database")]
public class EventDatabase : ScriptableObject
{
    // [1] ��� �̺�Ʈ�� �����ϴ� ����Ʈ
    [SerializeField]
    private List<SO_Event> eventList = new List<SO_Event>();

    /// <summary>
    /// [2] ��Ͽ��� ������ �̺�Ʈ�� �ϳ� ��ȯ�մϴ�.
    /// </summary>
    /// <returns>�������� ���õ� SO_Event. ����� ��������� null�� ��ȯ�մϴ�.</returns>
    public SO_Event GetRandomEvent()
    {
        if (eventList == null || eventList.Count == 0)
        {
            Debug.LogWarning("EventDatabase�� �̺�Ʈ�� ����ֽ��ϴ�!");
            return null;
        }

        int randomIndex = Random.Range(0, eventList.Count);
        return eventList[randomIndex];
    }

    /// <summary>
    /// [3] ������ �ε���(����)�� �̺�Ʈ�� ��ȯ�մϴ�.
    /// </summary>
    /// <param name="index">������ �̺�Ʈ�� �ε���</param>
    /// <returns>�ش� �ε����� SO_Event. �ε����� ������ ����� null�� ��ȯ�մϴ�.</returns>
    public SO_Event GetEvent(int index)
    {
        if (eventList == null || index < 0 || index >= eventList.Count)
        {
            Debug.LogError($"�߸��� �̺�Ʈ �ε���({index})�� ��û�߽��ϴ�.");
            return null;
        }

        return eventList[index];
    }

    /// <summary>
    /// ��ü �̺�Ʈ ����� ������ ��ȯ�մϴ�.
    /// </summary>
    public int GetEventCount()
    {
        if (eventList == null) return 0;
        return eventList.Count;
    }
}