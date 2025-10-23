using UnityEngine;
using System;
using DG.Tweening; // ✨ 1. DOTween 네임스페이스를 추가합니다.

/// <summary>
/// 게임의 전체적인 흐름 상태
/// </summary>
public enum GameState
{
    Start,   // 기차 출발 전, StartObject 조준 상태
    Playing, // 게임 플레이 중 (배경 스크롤, 몬스터 스폰)
    Event,   // 이벤트 발생으로 일시 정지된 상태 (EventManager가 제어)
    Die      // 플레이어 사망
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    /// <summary>
    /// 게임 상태가 변경될 때마다 호출되는 이벤트입니다.
    /// </summary>
    public event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        // 간단한 싱글톤 패턴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return; // ✨ 중복 실행 방지
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ✨ [핵심 수정] 2. DOTween "워밍업"
        // 게임 시작 시 DOTween을 미리 초기화합니다.
        // 이것은 DOFade, SetUpdate 같은 함수들을 미리 JIT 컴파일하여,
        // UI 페이드 인 시점에 렉(stutter)이 걸리는 것을 방지합니다.
        DOTween.Init();

        // --- 기존 코드 (시작 시 항상 정상 상태로 보장) ---
        Time.timeScale = 1f;
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
    }

    private void Start()
    {
        // 게임은 항상 'Start' 상태에서 시작
        ChangeState(GameState.Start);
    }

    /// <summary>
    /// 게임 상태를 변경하고, 이벤트를 방송(Broadcast)하는 중앙 함수
    /// </summary>
    private void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState); // 상태 변경을 구독자들에게 알림

        Debug.Log($"Game State Changed to: {newState}");
    }

    // --- 다른 스크립트에서 호출할 공용 함수들 ---

    /// <summary>
    /// StartObject가 맞았을 때 호출되어 게임을 'Playing' 상태로 만듭니다.
    /// </summary>
    public void StartGame()
    {
        if (CurrentState == GameState.Start)
        {
            ChangeState(GameState.Playing);
        }
    }

    /// <summary>
    /// EventManager가 이벤트를 시작할 때 호출합니다.
    /// </summary>
    public void TriggerEvent()
    {
        // 게임 플레이 중에만 이벤트가 발생할 수 있음
        if (CurrentState == GameState.Playing)
        {
            ChangeState(GameState.Event);
        }
    }

    /// <summary>
    /// EventManager가 이벤트를 종료할 때 호출합니다.
    /// </summary>
    public void EndEvent()
    {
        if (CurrentState == GameState.Event)
        {
            ChangeState(GameState.Playing);
        }
    }

    /// <summary>
    /// 플레이어(기차)가 파괴되었을 때 호출합니다.
    /// </summary>
    public void PlayerDied()
    {
        ChangeState(GameState.Die);
    }
}