using UnityEngine;
using System;
using System.Collections.Generic; // Queue 사용을 위해 추가
using DG.Tweening;

/// <summary>
/// 게임의 전체적인 흐름 상태
/// </summary>
public enum GameState
{
    Start,      // 기차 출발 전
    Playing,    // 게임 플레이 중
    Event,      // 이벤트/UI 팝업 상태 (일시정지)
    Boss,       // 보스전
    Die         // 사망
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnGameStateChanged;

    public float gameTime = 0f;

    // ✨ [핵심] UI 요청을 순차적으로 처리하기 위한 큐
    private Queue<Action> uiRequestQueue = new Queue<Action>();
    private bool isUIProcessing = false; // 현재 UI가 열려있는지 확인

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // DOTween 초기화
        DOTween.Init();

        Time.timeScale = 1f;
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
    }

    private void Start()
    {
        ChangeState(GameState.Start);
        gameTime = 0f;
    }

    private void Update()
    {
        if (CurrentState == GameState.Playing || CurrentState == GameState.Boss)
        {
            gameTime += Time.deltaTime;
        }
    }

    // ----------------------------------------------------------------------------
    // ✨ [핵심 기능] UI 큐 시스템 (레벨업, 이벤트 공용)
    // ----------------------------------------------------------------------------

    /// <summary>
    /// UI를 띄우는 행동(함수)을 큐에 등록합니다.
    /// 예: RegisterUIQueue(() => LevelUpManager.ShowChoice());
    /// </summary>
    public void RegisterUIQueue(Action uiAction)
    {
        uiRequestQueue.Enqueue(uiAction);

        // 현재 열려있는 창이 없다면 바로 다음 작업 실행
        if (!isUIProcessing)
        {
            ProcessNextUI();
        }
    }

    /// <summary>
    /// 큐에서 다음 UI 작업을 꺼내 실행합니다.
    /// </summary>
    private void ProcessNextUI()
    {
        if (uiRequestQueue.Count > 0)
        {
            isUIProcessing = true;

            // 1. 게임 일시 정지 (공통 처리)
            Time.timeScale = 0f;
            Physics2D.simulationMode = SimulationMode2D.Script;

            // 2. 상태 변경 (이벤트 상태로 전환)
            if (CurrentState == GameState.Playing || CurrentState == GameState.Boss)
            {
                ChangeState(GameState.Event);
            }

            // 3. 등록된 UI 함수 실행 (LevelUpManager나 EventManager의 함수가 호출됨)
            Action action = uiRequestQueue.Dequeue();
            action.Invoke();
        }
        else
        {
            // 큐가 비었으면 게임 재개
            isUIProcessing = false;
            ResumeGame();
        }
    }

    /// <summary>
    /// UI 매니저들이 작업(선택 완료, 창 닫기 등)이 끝났을 때 호출하는 함수
    /// </summary>
    public void CloseUI()
    {
        // 현재 창 처리가 끝났으므로 다음 창이 있는지 확인
        ProcessNextUI();
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

        // 이벤트 상태였다면 다시 플레이(또는 보스) 상태로 복귀
        if (CurrentState == GameState.Event)
        {
            // 보스전이었다면 보스 상태로, 아니면 플레잉으로 (간단히 Playing으로 복귀 예시)
            // 실제로는 이전 상태를 저장했다가 복구하는 것이 더 정확할 수 있음
            ChangeState(GameState.Playing);
        }
    }

    // ----------------------------------------------------------------------------

    private void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
        Debug.Log($"Game State Changed to: {newState}");
    }

    public void StartGame()
    {
        if (CurrentState == GameState.Start) ChangeState(GameState.Playing);
    }

    public void AppearBoss()
    {
        if (CurrentState == GameState.Playing) ChangeState(GameState.Boss);
    }
    public void BossDied()
    {
        if (CurrentState == GameState.Boss) ChangeState(GameState.Playing);
    }
    public void PlayerDied()
    {
        ChangeState(GameState.Die);
    }
}