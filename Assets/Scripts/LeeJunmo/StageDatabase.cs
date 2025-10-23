using UnityEngine;
using System.Collections.Generic; // List�� ����ϱ� ���� �ʿ�

// [CreateAssetMenu]�� ����ϸ� ������Ʈ â���� ��Ŭ������ �� ������ ���� �� �ֽ��ϴ�.
[CreateAssetMenu(fileName = "StageDatabase", menuName = "Game/Stage Database")]
public class StageDatabase : ScriptableObject
{
    [Header("�������� ������ ���")]
    [Tooltip("���⿡ 'AutoScrollBackground' ��ũ��Ʈ�� ������ �������� �����յ��� ����մϴ�.")]
    public List<GameObject> stagePrefabs;
}