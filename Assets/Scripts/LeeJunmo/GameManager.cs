using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;

public enum GameState
{
    Start,
    Playing,
    Event,      // 이벤트/레벨업 UI (시간 정지)
    Boss,
    Die,
    Pause       // ✨ [추가] 옵션 창 등으로 인한 일시 정지
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnGameStateChanged;

    public float gameTime = 0f;

    private Queue<Action> uiRequestQueue = new Queue<Action>();
    private bool isUIProcessing = false;

    // 일시정지 전 상태를 기억하기 위한 변수
    private GameState stateBeforePause;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

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
    // ✨ [추가] 일시정지(Pause) 시스템 - Option UI에서 호출
    // ----------------------------------------------------------------------------

    public void PauseGame()
    {
        // Playing이나 Boss 상태일 때만 일시정지 가능 (Event나 Die 중에는 불가)
        if (CurrentState == GameState.Playing || CurrentState == GameState.Boss)
        {
            stateBeforePause = CurrentState; // 현재 상태 기억
            ChangeState(GameState.Pause);

            Time.timeScale = 0f;
            Physics2D.simulationMode = SimulationMode2D.Script;
        }
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Pause)
        {
            // 1. 원래 상태로 복구
            ChangeState(stateBeforePause);

            Time.timeScale = 1f;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

            // 2. ✨ 일시정지 중에 쌓인 UI 요청(이벤트 등)이 있다면 처리 시작
            ProcessNextUI();
        }
    }

    // ----------------------------------------------------------------------------
    // UI 큐 시스템
    // ----------------------------------------------------------------------------

    public void RegisterUIQueue(Action uiAction)
    {
        uiRequestQueue.Enqueue(uiAction);

        // 현재 처리 중인 UI가 없고, 게임이 '일시정지' 상태가 아닐 때만 실행
        if (!isUIProcessing && CurrentState != GameState.Pause)
        {
            ProcessNextUI();
        }
    }

    private void ProcessNextUI()
    {
        // ✨ 일시정지 상태라면(옵션 창 열림) 큐를 실행하지 않고 대기
        if (CurrentState == GameState.Pause) return;

        if (uiRequestQueue.Count > 0)
        {
            isUIProcessing = true;

            Time.timeScale = 0f;
            Physics2D.simulationMode = SimulationMode2D.Script;

            // 이벤트 상태로 전환 (보스전이었든 플레잉이었든)
            if (CurrentState != GameState.Event && CurrentState != GameState.Die)
            {
                ChangeState(GameState.Event);
            }

            Action action = uiRequestQueue.Dequeue();
            action.Invoke();
        }
        else
        {
            // 큐가 비었으면 UI 종료 처리
            if (isUIProcessing)
            {
                isUIProcessing = false;
                ResumeFromEvent();
            }
        }
    }

    public void CloseUI()
    {
        ProcessNextUI();
    }

    private void ResumeFromEvent()
    {
        Time.timeScale = 1f;
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

        // 이벤트가 끝나면 기본적으로 Playing으로 돌아가되, 
        // 만약 이전 맥락이 Boss였다면 Boss 상태 관리가 필요할 수 있음.
        // 현재 구조상 Boss 상태는 Spawner 등에서 관리하므로 Playing으로 둬도 무방하거나,
        // 필요 시 stateBeforeEvent 등을 도입해야 함. 여기선 Playing으로 복귀.
        if (CurrentState == GameState.Event)
        {
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
    public void AppearBoss() { if (CurrentState == GameState.Playing) ChangeState(GameState.Boss); }
    public void BossDied() { if (CurrentState == GameState.Boss) ChangeState(GameState.Playing); }
    public void PlayerDied() { ChangeState(GameState.Die); }
}