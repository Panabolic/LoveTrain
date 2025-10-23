using UnityEngine;
using System.Collections.Generic; // List를 사용하기 위해 필요

// [CreateAssetMenu]를 사용하면 프로젝트 창에서 우클릭으로 이 에셋을 만들 수 있습니다.
[CreateAssetMenu(fileName = "StageDatabase", menuName = "Game/Stage Database")]
public class StageDatabase : ScriptableObject
{
    [Header("스테이지 프리팹 목록")]
    [Tooltip("여기에 'AutoScrollBackground' 스크립트를 포함한 스테이지 프리팹들을 등록합니다.")]
    public List<GameObject> stagePrefabs;
}