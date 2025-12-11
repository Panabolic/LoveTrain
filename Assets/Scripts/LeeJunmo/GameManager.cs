using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;

public enum GameState
{
    Start,
    Playing,
    Event,
    Boss,
    Die,
    Pause,
    StageTransition,
    Ending // 엔딩 상태
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnGameStateChanged;

    [Header("Game Settings")]
    [Tooltip("게임 종료(엔딩) 시간 (초) - 기본 15분(900초)")]
    public float maxGameTime = 900f;

    // 외부에서 엔딩 시간 도달 여부 확인용
    public bool IsTimeForEnding => gameTime >= maxGameTime;

    public float gameTime = 0f;

    // ✨ [누락된 부분 복구] 통계 데이터
    public int NormalKillCount { get; private set; }
    public int EliteKillCount { get; private set; }
    public int BossKillCount { get; private set; }
    public int TotalKillCount => NormalKillCount + EliteKillCount + BossKillCount;

    // UI Queue 관련
    private Queue<Action> uiRequestQueue = new Queue<Action>();
    private bool isUIProcessing = false;
    private GameState stateBeforePause;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
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

        // 통계 초기화
        NormalKillCount = 0;
        EliteKillCount = 0;
        BossKillCount = 0;
    }

    private void Update()
    {
        // ✨ 오직 'Playing' 상태일 때만 시간이 흐름
        if (CurrentState == GameState.Playing)
        {
            gameTime += Time.deltaTime;
        }
    }

    // ✨ [누락된 부분 복구] 킬 카운트 집계 함수
    public void AddKillCount(bool isElite)
    {
        if (isElite) EliteKillCount++;
        else NormalKillCount++;
    }

    public void AddBossKillCount()
    {
        BossKillCount++;
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
        Debug.Log($"Game State Changed to: {newState}");
    }

    // --- Boss & Ending Logic ---
    public void BossDied()
    {
        // 1. 엔딩 조건 체크 (시간 도달 시)
        if (gameTime >= maxGameTime)
        {
            Debug.Log("🎉 게임 클리어! 엔딩 시퀀스를 시작합니다.");

            ChangeState(GameState.Ending); // 상태 변경 (시간 정지 유지)

            if (EndingManager.Instance != null)
            {
                EndingManager.Instance.StartEnding();
            }
        }
        // 2. 스테이지 전환
        else
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.StartStageTransitionSequence();
            }
            else
            {
                ChangeState(GameState.Playing);
            }
        }
    }

    // --- Pause & UI Logic ---
    public void PauseGame()
    {
        if (CurrentState == GameState.Playing || CurrentState == GameState.Boss || CurrentState == GameState.Start)
        {
            stateBeforePause = CurrentState;
            ChangeState(GameState.Pause);
            Time.timeScale = 0f;
            Physics2D.simulationMode = SimulationMode2D.Script;
        }
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Pause)
        {
            ChangeState(stateBeforePause);
            Time.timeScale = 1f;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
            ProcessNextUI();
        }
    }

    public void RegisterUIQueue(Action uiAction)
    {
        uiRequestQueue.Enqueue(uiAction);
        if (!isUIProcessing && CurrentState != GameState.Pause) ProcessNextUI();
    }

    private void ProcessNextUI()
    {
        if (CurrentState == GameState.Pause) return;

        if (uiRequestQueue.Count > 0)
        {
            isUIProcessing = true;
            Time.timeScale = 0f;
            Physics2D.simulationMode = SimulationMode2D.Script;
            if (CurrentState != GameState.Event && CurrentState != GameState.Die) ChangeState(GameState.Event);
            uiRequestQueue.Dequeue().Invoke();
        }
        else if (isUIProcessing)
        {
            isUIProcessing = false;
            Time.timeScale = 1f;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
            if (CurrentState == GameState.Event) ChangeState(GameState.Playing);
        }
    }

    public void CloseUI() { ProcessNextUI(); }
    public void StartGame() { if (CurrentState == GameState.Start) ChangeState(GameState.Playing); }
    public void AppearBoss() { if (CurrentState == GameState.Playing) ChangeState(GameState.Boss); }
    public void PlayerDied() { ChangeState(GameState.Die); }
}