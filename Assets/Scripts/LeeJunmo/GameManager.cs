using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.SceneManagement;

public enum GameState
{
    Title,
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

    public float maxGameTime = 900f;
    public bool IsTimeForEnding => gameTime >= maxGameTime;
    public float gameTime = 0f;

    public int NormalKillCount { get; private set; }
    public int EliteKillCount { get; private set; }
    public int BossKillCount { get; private set; }
    public int TotalKillCount => NormalKillCount + EliteKillCount + BossKillCount;

    private Queue<Action> uiRequestQueue = new Queue<Action>();
    private bool isUIProcessing = false;

    private GameState stateBeforePause;
    private GameState stateBeforeEvent;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        DOTween.Init();
        InitSystemSettings(); // 시스템 설정 초기화 분리
    }

    // ✨ [추가] 씬 로드 이벤트 등록
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ✨ [핵심 수정] 씬 로딩이 "완료된 후"에 호출되는 콜백
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeGameData(); // 여기서 상태를 초기화해야 씬 이름을 정확히 가져옴
    }

    private void Start()
    {
        // 첫 실행 시에는 OnSceneLoaded가 Awake 이후 등록되어 실행되지 않을 수 있으므로 수동 호출
        if (CurrentState == default) InitializeGameData();
    }

    private void Update()
    {
        if (CurrentState == GameState.Playing) gameTime += Time.deltaTime;
    }

    // 물리 및 시간 설정 초기화 함수
    private void InitSystemSettings()
    {
        Time.timeScale = 1f;
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate; // ✨ 물리 엔진 복구
    }

    private void InitializeGameData()
    {
        Option.SetOptionInputBlocked(false);
        InitSystemSettings(); // ✨ 데이터 초기화 할 때 시스템 설정도 같이 복구

        gameTime = 0f;
        NormalKillCount = 0; EliteKillCount = 0; BossKillCount = 0;

        // 큐 초기화 (이전 게임 잔여물 제거)
        uiRequestQueue.Clear();
        isUIProcessing = false;

        string currentSceneName = SceneManager.GetActiveScene().name;

        // ✨ 이제 씬 로드 후에 호출되므로 정확한 이름을 가져옵니다.
        if (currentSceneName == "Start")
        {
            ChangeState(GameState.Title);
        }
        else
        {
            Debug.Log($"[GameManager] 인게임 씬({currentSceneName}) 시작. Start 상태로 초기화.");
            ChangeState(GameState.Start);
        }
    }

    // ... (중간 함수들 생략: EnterStartState, AddKillCount, ChangeState, BossDied 등 기존 유지) ...
    // ... (PauseGame, ResumeGame, UI Logic 등 기존 코드 그대로 사용) ...

    public void EnterStartState() { ChangeState(GameState.Start); }
    public void AddKillCount(bool isElite) { if (isElite) EliteKillCount++; else NormalKillCount++; }
    public void AddBossKillCount() { BossKillCount++; }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        if (BossWarningLoopUI.Instance != null)
        {
            bool shouldPauseWarning = (newState == GameState.Pause || newState == GameState.Event);
            BossWarningLoopUI.Instance.SetPauseState(shouldPauseWarning);
        }

        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
        Debug.Log($"Game State Changed to: {newState}");
    }

    // ... (나머지 로직들은 문제 없어 보임) ...
    // BossDied, PauseGame, ResumeGame, RegisterUIQueue, ProcessNextUI 등등...
    // 단, ProcessNextUI 등은 기존 코드 그대로 복붙하시면 됩니다.

    public void BossDied()
    {
        PoolManager.instance.DespawnAllEnemiesExceptBoss();
        if (gameTime >= maxGameTime)
        {
            uiRequestQueue.Clear();
            isUIProcessing = false;
            
            ChangeState(GameState.Ending);
            if (EndingManager.Instance != null) EndingManager.Instance.StartEnding();
        }
        else
        {
            if (CurrentState == GameState.Event)
            {
                stateBeforeEvent = GameState.StageTransition;
            }
            else
            {
                if (StageManager.Instance != null) StageManager.Instance.StartStageTransitionSequence();
                else ChangeState(GameState.StageTransition);
            }
        }
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Title || CurrentState == GameState.Playing || CurrentState == GameState.Boss || CurrentState == GameState.Start)
        {
            stateBeforePause = CurrentState;
            SoundEventBus.Publish(SoundID.UI_Option);
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
            SoundEventBus.Publish(SoundID.UI_Option);
            Time.timeScale = 1f;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
            ProcessNextUI();
        }
    }

    public void RegisterUIQueue(Action uiAction)
    {
        if (IsTimeForEnding || CurrentState == GameState.Ending) return;
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

            if (CurrentState == GameState.Event)
            {
                if (stateBeforeEvent == GameState.StageTransition)
                {
                    if (StageManager.Instance != null) StageManager.Instance.StartStageTransitionSequence();
                    else ChangeState(GameState.StageTransition);
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
        // 1. 모든 트윈 제거 (안전장치)
        DOTween.KillAll();

        // 2. 물리/시간 초기화 (혹시 멈춘 상태였다면 복구)
        InitSystemSettings();

        // 3. 씬 로드 -> 완료되면 OnSceneLoaded가 호출되어 InitializeGameData 실행됨
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
