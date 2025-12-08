using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Start,
    Playing,
    Event,
    Boss,
    Die,
    Pause,
    StageTransition // ✨ 스테이지 전환 상태 (모든 조작 차단)
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnGameStateChanged;

    public float gameTime = 0f;

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
    }

    private void Update()
    {
        // Playing이나 Boss 상태일 때만 시간 흐름
        if (CurrentState == GameState.Playing || CurrentState == GameState.Boss)
        {
            gameTime += Time.deltaTime;
        }
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
        Debug.Log($"Game State Changed to: {newState}");
    }

    // --- Boss Logic ---
    public void BossDied()
    {
        if (CurrentState == GameState.Boss)
        {
            // GameManager는 직접 연출하지 않고, StageManager에게 위임
            if (StageManager.Instance != null)
            {
                StageManager.Instance.StartStageTransitionSequence();
            }
            else
            {
                Debug.LogError("StageManager가 없습니다! 바로 Playing으로 전환합니다.");
                ChangeState(GameState.Playing);
            }
        }
    }

    // --- Pause & UI Logic (기존 유지) ---
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

    public void RestartGame()
    {
        // 2. 시간이 멈춰있을 경우를 대비해 시간을 다시 흐르게 설정
        // (일시정지 후 재시작 시 게임이 멈춰있는 버그 방지)
        Time.timeScale = 1;

        // 3. 현재 활성화된 씬의 이름을 가져와서 다시 로드
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        // Debug.Log("게임을 종료합니다...");
        Application.Quit();

        // (에디터에서는 작동 안 함, 빌드된 게임에서만 작동)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void CloseUI() { ProcessNextUI(); }
    public void StartGame() { if (CurrentState == GameState.Start) ChangeState(GameState.Playing); }
    public void AppearBoss() { if (CurrentState == GameState.Playing) ChangeState(GameState.Boss); }
    public void PlayerDied() { ChangeState(GameState.Die); }
}