using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.SceneManagement; // ✨ 씬 관리를 위해 추가 필수

public enum GameState
{
    Start,
    Playing,
    Event,
    Boss,
    Die,
    Pause,
    StageTransition,
    Ending
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnGameStateChanged;

    [Header("Game Settings")]
    [Tooltip("게임 종료(엔딩) 시간 (초) - 기본 15분(900초)")]
    public float maxGameTime = 900f;

    public bool IsTimeForEnding => gameTime >= maxGameTime;

    public float gameTime = 0f;

    // 통계 데이터
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
        // 초기 시작 시 상태 설정
        InitializeGameData();
    }

    private void Update()
    {
        if (CurrentState == GameState.Playing)
        {
            gameTime += Time.deltaTime;
        }
    }

    // ✨ 데이터 초기화 로직 분리 (Start와 Restart에서 공통 사용)
    private void InitializeGameData()
    {
        gameTime = 0f;
        NormalKillCount = 0;
        EliteKillCount = 0;
        BossKillCount = 0;
        ChangeState(GameState.Start);
    }

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
        if (gameTime >= maxGameTime)
        {
            Debug.Log("🎉 게임 클리어! 엔딩 시퀀스를 시작합니다.");
            ChangeState(GameState.Ending);

            if (EndingManager.Instance != null)
            {
                EndingManager.Instance.StartEnding();
            }
        }
        else
        {
            if (StageManager.Instance != null)
                StageManager.Instance.StartStageTransitionSequence();
            else
                ChangeState(GameState.Playing);
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

    // ✨ [추가] 게임 재시작 (타이틀로 이동)
    public void RestartGame()
    {
        Time.timeScale = 1f; // 시간 정지 해제

        // 데이터 초기화 (시간, 킬수 등) 및 상태를 Start로 변경
        InitializeGameData();

        // 씬 로드
        SceneManager.LoadScene("Start");
    }

    // ✨ [추가] 게임 종료
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}