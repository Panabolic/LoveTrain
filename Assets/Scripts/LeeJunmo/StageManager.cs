using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    // 스테이지별 터널 연출 설정을 묶은 클래스
    [System.Serializable]
    public class StageTransitionSetting
    {
        [Header("Tunnel Assets")]
        public GameObject tunnelPrefab;

        [Header("Tunnel Positions (Scene Transforms)")]
        [Tooltip("터널이 생성될 위치 (화면 밖 오른쪽)")]
        public Transform tunnelSpawnPoint;

        [Tooltip("터널이 이동해 멈출 위치 (기차 앞)")]
        public Transform tunnelTargetPoint;

        [Tooltip("기차가 터널 안으로 들어갈 목표 지점")]
        public Transform trainEnterPoint;
    }

    [Header("Stage Config")]
    [Tooltip("배경 프리팹 데이터베이스")]
    [SerializeField] private StageDatabase stageDatabase;

    // 스테이지별 연출 설정 리스트 (0번 인덱스 = 1스테이지 클리어 시 사용)
    [Header("Transition Settings (Per Stage)")]
    [SerializeField] private List<StageTransitionSetting> transitionSettings;
    [SerializeField] private Transform bgParent;

    [Header("Player Reset")]
    [Tooltip("다음 스테이지 시작 시 기차 좌표")]
    [SerializeField] private Vector3 playerResetPosition = new Vector3(0f, -7.6f, 0f);

    [Header("Components")]
    [SerializeField] private Train train;
    [SerializeField] private UIAlphaFader uiFader;

    // 현재 스테이지 인덱스
    public int CurrentStageIndex { get; private set; } = 0;

    // 현재 씬에 생성된 배경 오브젝트 참조
    private GameObject currentStageObject;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // 게임 시작 시 첫 번째 스테이지 로드
        LoadStage(0);
    }

    // -----------------------------------------------------------
    // 스테이지 로드 (배경 교체)
    // -----------------------------------------------------------
    private void LoadStage(int index)
    {
        if (stageDatabase == null || stageDatabase.stagePrefabs == null || stageDatabase.stagePrefabs.Count == 0)
        {
            Debug.LogError("[StageManager] StageDatabase 오류! 데이터가 비어있거나 연결되지 않았습니다.");
            return;
        }

        index = Mathf.Clamp(index, 0, stageDatabase.stagePrefabs.Count - 1);
        CurrentStageIndex = index;

        // 기존 배경 삭제
        if (currentStageObject != null) Destroy(currentStageObject);

        // 새 배경 프리팹 생성
        GameObject prefab = stageDatabase.stagePrefabs[index];
        if (prefab != null)
        {
            currentStageObject = Instantiate(prefab, bgParent);
        }
    }

    // -----------------------------------------------------------
    // ✨ DOTween Sequence를 활용한 스테이지 전환 연출 (핵심)
    // -----------------------------------------------------------
    public void StartStageTransitionSequence()
    {
        // 1. 상태 변경 (모든 조작, 스폰, 아이템 정지)
        if (PoolManager.instance != null) PoolManager.instance.DespawnAllEnemiesExceptBoss();
        GameManager.Instance.ChangeState(GameState.StageTransition);

        Debug.Log($"[StageManager] Stage {CurrentStageIndex + 1} 클리어! 연출 시퀀스 시작.");

        // 현재 스테이지에 맞는 연출 설정 가져오기
        int settingIndex = CurrentStageIndex % transitionSettings.Count;
        StageTransitionSetting setting = transitionSettings[settingIndex];

        // 터널 생성 (화면 밖)
        GameObject tunnel = null;
        if (setting.tunnelPrefab != null && setting.tunnelSpawnPoint != null)
        {
            tunnel = Instantiate(setting.tunnelPrefab, setting.tunnelSpawnPoint.position, Quaternion.identity);
        }

        // ✨ 시퀀스 조립
        Sequence seq = DOTween.Sequence();

        // [Step 1] 2초 대기 (보스 사망 연출 감상)
        seq.AppendInterval(2.0f);

        // [Step 2] 터널 등장 (2초간 이동)
        if (tunnel != null && setting.tunnelTargetPoint != null)
        {
            seq.Append(tunnel.transform.DOMove(setting.tunnelTargetPoint.position, 2.0f).SetEase(Ease.OutQuad));

            // ✨ 터널 도착 직후 배경 스크롤 정지 (콜백)
            seq.AppendCallback(() => {
                if (currentStageObject != null)
                {
                    var bg = currentStageObject.GetComponent<AutoScrollBackground>();
                    if (bg != null) bg.SetScrolling(false);
                }
            });
        }

        // [Step 3] 기차 진입 (1.5초간 터널 속으로 이동)
        if (train != null && setting.trainEnterPoint != null)
        {
            seq.Append(train.transform.DOMove(setting.trainEnterPoint.position, 1.5f).SetEase(Ease.InQuad));
        }

        // [Step 4] 화면 암전 (FadeIn: 검은 화면이 됨)
        if (uiFader != null)
        {
            seq.Append(uiFader.FadeIn(1.0f));
        }

        // [Step 5] 암전 상태에서 데이터 교체 (콜백)
        seq.AppendCallback(() =>
        {
            // 배경 교체 (다음 스테이지 로드)
            NextStageDataUpdate();

            // 기차 위치 리셋 (화면 왼쪽 시작 지점으로)
            if (train != null) train.transform.position = playerResetPosition;

            // 터널 삭제
            if (tunnel != null) Destroy(tunnel);
        });

        // 데이터 교체 후 잠시 대기 (로딩 느낌, 0.5초)
        seq.AppendInterval(0.5f);

        // [Step 6] 화면 밝아짐 (FadeOut: 검은 화면이 사라짐)
        if (uiFader != null)
        {
            seq.Append(uiFader.FadeOut(1.0f));
        }

        // [Step 7] 시퀀스 종료 시 게임 재개
        seq.OnComplete(() =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.gameTime = 0f;
                GameManager.Instance.ChangeState(GameState.Playing);
                Debug.Log("[StageManager] 다음 스테이지 시작! (Game Resumed)");
            }
        });
    }

    // 다음 스테이지 인덱스 계산 및 로드
    private void NextStageDataUpdate()
    {
        if (stageDatabase == null || stageDatabase.stagePrefabs.Count == 0) return;

        int nextIndex = (CurrentStageIndex + 1) % stageDatabase.stagePrefabs.Count;
        LoadStage(nextIndex);

        Debug.Log($"[StageManager] 스테이지 데이터 교체 완료: {nextIndex + 1} 스테이지");
    }
}