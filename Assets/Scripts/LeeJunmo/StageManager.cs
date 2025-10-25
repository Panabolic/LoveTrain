using UnityEngine;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("참조")]
    [SerializeField] private StageDatabase backgroundDatabase;
    [SerializeField] private Train train; // AutoScrollBackground에 연결할 Train 참조
    [SerializeField] private Transform backgroundParent; // 배경 프리팹이 생성될 부모

    [Header("시작 설정")]
    [SerializeField] private int startingBackgroundIndex = 0; // 시작 시 로드할 배경 인덱스

    // --- 내부 변수 ---
    private GameObject currentBackgroundInstance; // 현재 로드된 배경 프리팹 인스턴스
    private int currentBackgroundIndex;

    private void Awake()
    {
        // 간단한 싱글톤
        if (Instance != null && Instance != this) { Destroy(gameObject); } else { Instance = this; }
    }

    private void Start()
    {
        if (backgroundDatabase == null || backgroundDatabase.stagePrefabs.Count == 0)
        {
            Debug.LogError("StageBackgroundDatabase가 없거나 비어있습니다!", this);
            this.enabled = false;
            return;
        }

        // 게임 시작 시 첫 배경 로드
        currentBackgroundIndex = startingBackgroundIndex;
        LoadBackgroundByIndex(currentBackgroundIndex);

        // (선택) GameManager 구독하여 Playing 상태일 때만 스크롤 활성화 등...
        // if (GameManager.Instance != null) { ... }
    }

    /// <summary>
    /// 인덱스를 사용하여 특정 배경 프리팹을 로드합니다.
    /// </summary>
    public void LoadBackgroundByIndex(int index)
    {
        if (index < 0 || index >= backgroundDatabase.stagePrefabs.Count)
        {
            Debug.LogError($"잘못된 배경 인덱스입니다: {index}");
            return;
        }

        LoadBackground(backgroundDatabase.stagePrefabs[index]);
        currentBackgroundIndex = index; // 현재 인덱스 기록
    }

    /// <summary>
    /// 프리팹 자체를 받아 배경을 로드하는 핵심 함수
    /// </summary>
    private void LoadBackground(GameObject bgPrefab)
    {
        // 1. 기존 배경 인스턴스가 있으면 파괴
        if (currentBackgroundInstance != null)
        {
            Destroy(currentBackgroundInstance);
        }

        // 2. 새 배경 프리팹 인스턴스화
        if (bgPrefab != null)
        {
            currentBackgroundInstance = Instantiate(bgPrefab, backgroundParent);

            // 3. 프리팹 내 AutoScrollBackground 스크립트에 Train 참조 연결
            AutoScrollBackground bgScroll = currentBackgroundInstance.GetComponent<AutoScrollBackground>();
            if (bgScroll != null && train != null)
            {
                bgScroll.train = this.train;
                // (선택) 초기 스크롤 상태 설정 (GameManager 연동 시)
                // bgScroll.enabled = (GameManager.Instance?.CurrentState == GameState.Playing);
            }
            else if (bgScroll == null)
            {
                Debug.LogError($"배경 프리팹 '{bgPrefab.name}'에 AutoScrollBackground.cs가 없습니다!", bgPrefab);
            }
        }
        else
        {
            Debug.LogError("로드할 배경 프리팹이 null입니다!");
        }
    }

    /// <summary>
    /// 다음 순서의 배경을 로드합니다. (마지막이면 처음으로)
    /// </summary>
    public void LoadNextBackground()
    {
        int nextIndex = (currentBackgroundIndex + 1) % backgroundDatabase.stagePrefabs.Count;
        LoadBackgroundByIndex(nextIndex);
    }

    /// <summary>
    /// 랜덤 배경을 로드합니다. (현재 배경 제외)
    /// </summary>
    public void LoadRandomBackground()
    {
        if (backgroundDatabase.stagePrefabs.Count <= 1)
        {
            LoadBackgroundByIndex(0); // 배경이 하나뿐임
            return;
        }

        int randomIndex;
        do
        {
            randomIndex = Random.Range(0, backgroundDatabase.stagePrefabs.Count);
        } while (randomIndex == currentBackgroundIndex); // 현재와 다른 인덱스가 나올 때까지 반복

        LoadBackgroundByIndex(randomIndex);
    }
}