using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.SceneManagement;

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

    // ... (설정 변수들 유지) ...
    public float maxGameTime = 900f;
    public bool IsTimeForEnding => gameTime >= maxGameTime;
    public float gameTime = 0f;

    // 통계 데이터 유지
    public int NormalKillCount { get; private set; }
    public int EliteKillCount { get; private set; }
    public int BossKillCount { get; private set; }
    public int TotalKillCount => NormalKillCount + EliteKillCount + BossKillCount;

    // UI Queue
    private Queue<Action> uiRequestQueue = new Queue<Action>();
    private bool isUIProcessing = false;

    // ✨ 상태 복구용 변수
    private GameState stateBeforePause;
    private GameState stateBeforeEvent;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        DOTween.Init();
        Time.timeScale = 1f;
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
    }

    private void Start() { InitializeGameData(); }

    private void Update()
    {
        if (CurrentState == GameState.Playing) gameTime += Time.deltaTime;
    }

    private void InitializeGameData()
    {
        gameTime = 0f;
        NormalKillCount = 0; EliteKillCount = 0; BossKillCount = 0;
        ChangeState(GameState.Start);
    }

    public void AddKillCount(bool isElite) { if (isElite) EliteKillCount++; else NormalKillCount++; }
    public void AddBossKillCount() { BossKillCount++; }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        // ✨ 상태 변경 시 경고창 UI에게 알림 (일시정지/재개 처리용)
        if (BossWarningLoopUI.Instance != null)
        {
            // Pause나 Event 상태로 가면 경고창 일시정지, 아니면 재개
            bool shouldPauseWarning = (newState == GameState.Pause || newState == GameState.Event);
            BossWarningLoopUI.Instance.SetPauseState(shouldPauseWarning);
        }

        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
        Debug.Log($"Game State Changed to: {newState}");
    }

    // --- Boss & Ending Logic ---
    public void BossDied()
    {
        PoolManager.instance.DespawnAllEnemiesExceptBoss();
        // 1. 엔딩 (시간 다 됨)
        if (gameTime >= maxGameTime)
        {
            // ✨ [핵심 수정] 엔딩 직전, 대기 중인 UI(레벨업 등) 싹 비우기
            uiRequestQueue.Clear();
            isUIProcessing = false;

            Debug.Log("🎉 게임 클리어! 엔딩");
            ChangeState(GameState.Ending);
            if (EndingManager.Instance != null) EndingManager.Instance.StartEnding();
        }
        // 2. 스테이지 전환 (시간 남음)
        else
        {
            // ✨ [핵심 수정]
            // 만약 보상 이벤트(Event)가 진행 중이라면, 'Playing'으로 돌아가는 게 아니라
            // 이벤트 종료 후 'StageTransition' 상태로 가도록 '예약'만 해둠.
            if (CurrentState == GameState.Event)
            {
                stateBeforeEvent = GameState.StageTransition; // UI 닫히면 여기로 감
                Debug.Log("보상 획득 중... 종료 후 스테이지 전환 예정");
            }
            else
            {
                // 이벤트가 없다면 즉시 전환
                if (StageManager.Instance != null)
                    StageManager.Instance.StartStageTransitionSequence();
                else
                    ChangeState(GameState.StageTransition); // 비상용
            }
        }
    }

    // --- Pause Logic ---
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

    // --- UI Logic (Event) ---
    public void RegisterUIQueue(Action uiAction)
    {
        // ✨ [핵심 수정 1] 엔딩 시간이 되었거나 이미 엔딩 상태라면, 새로운 UI(레벨업 등) 요청을 무시함
        if (IsTimeForEnding || CurrentState == GameState.Ending)
        {
            Debug.Log("🚫 엔딩 상황이므로 UI 요청(레벨업 등)을 무시합니다.");
            return;
        }

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

            // ✨ 이벤트 시작 전 상태 저장 (이미 Event면 덮어쓰지 않음)
            if (CurrentState != GameState.Event && CurrentState != GameState.Die && CurrentState != GameState.Ending)
            {
                stateBeforeEvent = CurrentState;
                ChangeState(GameState.Event);
            }

            uiRequestQueue.Dequeue().Invoke();
        }
        else if (isUIProcessing)
        {
            isUIProcessing = false;
            Time.timeScale = 1f;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

            // ✨ 이벤트 종료: 저장해둔 상태로 복구
            if (CurrentState == GameState.Event)
            {
                // 아까 BossDied에서 stateBeforeEvent를 StageTransition으로 바꿨다면?
                // -> 여기서 StageTransition으로 감!
                if (stateBeforeEvent == GameState.StageTransition)
                {
                    if (StageManager.Instance != null)
                        StageManager.Instance.StartStageTransitionSequence();
                    else
                        ChangeState(GameState.StageTransition);
                }
                else
                {
                    ChangeState(stateBeforeEvent);
                }
            }
        }
    }

    public void CloseUI() { ProcessNextUI(); }
    public void StartGame() { if (CurrentState == GameState.Start) ChangeState(GameState.Playing); }
    public void AppearBoss() { if (CurrentState == GameState.Playing) ChangeState(GameState.Boss); }
    public void PlayerDied() { ChangeState(GameState.Die); }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        InitializeGameData();
        SceneManager.LoadScene("Start");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}