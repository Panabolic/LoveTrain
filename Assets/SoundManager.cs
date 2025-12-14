using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // ✨ DOTween 필수

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("사운드 데이터 등록 (드래그 & 드롭)")]
    [SerializeField] private List<SoundData> soundList;

    [Header("설정")]
    [SerializeField] private int sfxPoolSize = 15;        // 일반 효과음 (총알, 피격 등) 동시 재생 한계
    [SerializeField] private int importantSfxPoolSize = 5; // ✨ 중요 효과음 (경고, 보스 등) 동시 재생 한계
    [SerializeField] private float crossFadeDuration = 1.0f; // BGM 전환 시간

    // 빠른 검색을 위한 딕셔너리
    private Dictionary<SoundID, SoundData> soundMap = new Dictionary<SoundID, SoundData>();

    // 오디오 소스 풀링
    private List<AudioSource> sfxSources;           // 일반 풀
    private List<AudioSource> importantSfxSources;  // ✨ 중요 풀
    private AudioSource bgmSource;

    private float masterBgmVolume = 1f;
    private float masterSfxVolume = 1f;
    private float currentBgmClipVolume = 1f; // 현재 재생 중인 클립의 고유 볼륨

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        // 1. 데이터 딕셔너리로 변환
        foreach (var data in soundList)
        {
            if (data.id != SoundID.None)
                soundMap[data.id] = data;
        }

        // 2. BGM 소스 생성
        GameObject bgmObj = new GameObject("BGM_Source");
        bgmObj.transform.SetParent(transform);
        bgmSource = bgmObj.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        // 3. 일반 SFX 풀 생성
        sfxSources = new List<AudioSource>();
        GameObject sfxGroup = new GameObject("SFX_Pool_Normal");
        sfxGroup.transform.SetParent(transform);
        CreatePool(sfxGroup.transform, sfxSources, sfxPoolSize, "SFX_Normal");

        // ✨ 4. 중요 SFX 풀 생성 (VIP 전용)
        importantSfxSources = new List<AudioSource>();
        GameObject importantGroup = new GameObject("SFX_Pool_Important");
        importantGroup.transform.SetParent(transform);
        CreatePool(importantGroup.transform, importantSfxSources, importantSfxPoolSize, "SFX_Important");
    }

    // 풀 생성 헬퍼 함수
    private void CreatePool(Transform parent, List<AudioSource> poolList, int size, string namePrefix)
    {
        for (int i = 0; i < size; i++)
        {
            GameObject go = new GameObject($"{namePrefix}_{i}");
            go.transform.SetParent(parent);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            poolList.Add(source);
        }
    }

    // ✨ 이벤트 버스 및 GameManager 구독
    private void OnEnable()
    {
        SoundEventBus.OnPlaySound += PlaySoundHandler;
    }

    private void Start()
    {
        // GameManager 상태 변화 감지
        if (GameManager.Instance != null)
        {
            // 1. 이벤트 구독
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

            // 2. 게임 시작 시점의 상태를 강제로 한 번 적용
            HandleGameStateChanged(GameManager.Instance.CurrentState);
        }
    }

    private void OnDisable()
    {
        SoundEventBus.OnPlaySound -= PlaySoundHandler;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    // ✨ 게임 상태에 따른 자동 BGM 변경 로직
    private void HandleGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            // [Title]: 타이틀 BGM 재생
            case GameState.Title:
                PlaySoundHandler(SoundID.BGM_Title, Vector3.zero);
                break;

            // [Start]: 게임 시작 버튼 누름 -> BGM 정지 (정적)
            case GameState.Start:
                PlaySoundHandler(SoundID.BGM_Stop, Vector3.zero);
                break;

            // [Playing]: 배틀 BGM 재생
            case GameState.Playing:
                PlaySoundHandler(SoundID.BGM_Battle, Vector3.zero);
                break;

            // [Boss]: 보스 BGM 재생
            case GameState.Boss:
                PlaySoundHandler(SoundID.BGM_Boss, Vector3.zero);
                break;

            // [Die]: 게임 오버 BGM 재생
            case GameState.Die:
                PlaySoundHandler(SoundID.BGM_GameOver, Vector3.zero);
                break;

            // BGM 유지 상태
            case GameState.Event:
            case GameState.Pause:
            case GameState.StageTransition:
                break;

            case GameState.Ending:
                // 필요 시 엔딩 BGM 재생
                break;
        }
    }

    // 실제 사운드 재생 로직 (이벤트 핸들러)
    private void PlaySoundHandler(SoundID id, Vector3 position)
    {
        // BGM 정지 명령 처리
        if (id == SoundID.BGM_Stop)
        {
            StopBGM();
            return;
        }

        if (!soundMap.TryGetValue(id, out SoundData data))
        {
            Debug.LogWarning($"[SoundManager] 등록되지 않은 사운드 ID: {id}");
            return;
        }

        if (id.ToString().StartsWith("BGM"))
        {
            PlayBGM(data);
        }
        else
        {
            PlaySFX(data, position);
        }
    }

    // PlayBGM 함수 (DOTween 적용)
    private void PlayBGM(SoundData data)
    {
        // 이미 같은 곡이 재생 중이면 종료
        if (bgmSource.clip == data.clip && bgmSource.isPlaying) return;

        // 목표 볼륨 계산
        float targetVolume = data.volume * masterBgmVolume;
        currentBgmClipVolume = data.volume;

        // 처음 재생이면 바로 재생
        if (bgmSource.clip == null)
        {
            bgmSource.clip = data.clip;
            bgmSource.volume = 0f;
            bgmSource.pitch = data.pitch;
            bgmSource.Play();
            bgmSource.DOFade(targetVolume, crossFadeDuration).SetUpdate(true);
            return;
        }

        // 1. 페이드 아웃
        bgmSource.DOFade(0f, crossFadeDuration).SetUpdate(true).OnComplete(() =>
        {
            // 2. 곡 교체 및 재생
            bgmSource.clip = data.clip;
            bgmSource.pitch = data.pitch;
            bgmSource.Play();

            // 3. 페이드 인
            bgmSource.DOFade(targetVolume, crossFadeDuration).SetUpdate(true);
        });
    }

    // BGM 정지 메서드
    public void StopBGM()
    {
        if (bgmSource == null || !bgmSource.isPlaying) return;

        bgmSource.DOKill();
        bgmSource.DOFade(0f, crossFadeDuration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                bgmSource.Stop();
                bgmSource.clip = null;
            });
    }

    // ✨ [핵심 수정] PlaySFX 함수: 중요도에 따라 다른 풀 사용
    private void PlaySFX(SoundData data, Vector3 position)
    {
        AudioSource source;

        // 중요 사운드인가? -> 중요 풀 사용
        if (data.isImportant)
        {
            source = GetAvailableSource(importantSfxSources);
        }
        // 일반 사운드인가? -> 일반 풀 사용
        else
        {
            source = GetAvailableSource(sfxSources);
        }

        source.clip = data.clip;
        source.volume = data.volume * masterSfxVolume;
        source.pitch = data.pitch;

        // 중요 사운드라면 루프 설정도 반영 (필요시)
        source.loop = data.loop;

        if (position != Vector3.zero)
        {
            source.spatialBlend = 1.0f;
            source.transform.position = position;
        }
        else
        {
            source.spatialBlend = 0.0f;
        }

        source.Play();
    }

    // ✨ [수정] 소스 가져오는 로직 (일반/중요 풀 공용)
    private AudioSource GetAvailableSource(List<AudioSource> sourcePool)
    {
        // 1. 놀고 있는 소스 찾기
        foreach (var source in sourcePool)
        {
            if (!source.isPlaying) return source;
        }

        // 2. 다 재생 중이면? 가장 오래된 것(리스트 맨 앞)을 뺏어옴
        AudioSource recycleSource = sourcePool[0];
        sourcePool.RemoveAt(0);
        sourcePool.Add(recycleSource); // 맨 뒤로 보냄 (최신 사용됨 처리)

        // 강제 중단 후 사용
        recycleSource.Stop();
        return recycleSource;
    }

    // 옵션 창에서 호출할 함수들
    public void SetBGMVolume(float volume)
    {
        masterBgmVolume = volume;
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.DOKill();
            bgmSource.volume = currentBgmClipVolume * masterBgmVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        masterSfxVolume = volume;
    }

    private void OnValidate()
    {
        if (soundList == null) return;

        foreach (var data in soundList)
        {
            if (data.pitch == 0f)
            {
                data.pitch = 1f;
                data.volume = 1f;
            }
        }
    }

    public void PlayButtonSound()
    {
        SoundEventBus.Publish(SoundID.UI_Click);
    }

    public float GetBGMVolume() => masterBgmVolume;
    public float GetSFXVolume() => masterSfxVolume;
}