using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // ✨ DOTween 필수

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("사운드 데이터 등록 (드래그 & 드롭)")]
    [SerializeField] private List<SoundData> soundList;

    [Header("설정")]
    [SerializeField] private int sfxPoolSize = 15; // 효과음 동시 재생 한계
    [SerializeField] private float crossFadeDuration = 1.0f; // ✨ BGM 전환 시간

    // 빠른 검색을 위한 딕셔너리
    private Dictionary<SoundID, SoundData> soundMap = new Dictionary<SoundID, SoundData>();

    // 오디오 소스 풀링
    private List<AudioSource> sfxSources;
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

        // 3. SFX 풀 생성
        sfxSources = new List<AudioSource>();
        GameObject sfxGroup = new GameObject("SFX_Pool");
        sfxGroup.transform.SetParent(transform);

        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject go = new GameObject($"SFX_{i}");
            go.transform.SetParent(sfxGroup.transform);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            sfxSources.Add(source);
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

            // ✨ [핵심 수정] 2. 게임 시작 시점의 상태를 강제로 한 번 적용!
            // (이미 Title 상태라서 이벤트가 발생 안 했거나, 놓쳤을 경우를 대비)
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

            // ✨ [핵심] BGM을 유지해야 하는 상태들
            // 여기에 포함된 상태가 되면, SoundManager는 아무런 명령도 내리지 않습니다.
            // 따라서 이전에 틀어져 있던 BGM(Playing이든 Boss든)이 계속 이어집니다.
            case GameState.Event:           // 레벨업, 보상 창
            case GameState.Pause:           // 일시정지
            case GameState.StageTransition: // 스테이지 이동 중
                // 아무것도 안 함 (break) -> 기존 BGM 유지됨
                break;

            case GameState.Ending:
                // 엔딩 BGM이 있다면 재생, 없다면 Stop
                // PlaySoundHandler(SoundID.BGM_Ending, Vector3.zero);
                break;
        }
    }

    // 실제 사운드 재생 로직 (이벤트 핸들러)
    private void PlaySoundHandler(SoundID id, Vector3 position)
    {
        // ✨ [추가] BGM 정지 명령 처리
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

    // ✨ [핵심 수정] PlayBGM 함수 (DOTween 적용)
    private void PlayBGM(SoundData data)
    {
        // 이미 같은 곡이 재생 중이면 볼륨이나 피치만 갱신하고 종료
        if (bgmSource.clip == data.clip && bgmSource.isPlaying) return;

        // 목표 볼륨 계산
        float targetVolume = data.volume * masterBgmVolume;
        currentBgmClipVolume = data.volume; // 클립 고유 볼륨 기억

        // 처음 재생(클립 없음)이면 바로 재생
        if (bgmSource.clip == null)
        {
            bgmSource.clip = data.clip;
            bgmSource.volume = 0f; // 0에서 시작
            bgmSource.pitch = data.pitch;
            bgmSource.Play();
            bgmSource.DOFade(targetVolume, crossFadeDuration).SetUpdate(true);
            return;
        }

        // 1. 페이드 아웃 (기존 곡 줄이기)
        bgmSource.DOFade(0f, crossFadeDuration).SetUpdate(true).OnComplete(() =>
        {
            // 2. 곡 교체 및 재생
            bgmSource.clip = data.clip;
            bgmSource.pitch = data.pitch;
            bgmSource.Play();

            // 3. 페이드 인 (새 곡 키우기)
            bgmSource.DOFade(targetVolume, crossFadeDuration).SetUpdate(true);
        });
    }

    // ✨ [추가] BGM 정지 메서드 (외부에서 직접 호출 가능)
    public void StopBGM()
    {
        // 재생 중이 아니면 무시
        if (bgmSource == null || !bgmSource.isPlaying) return;

        // 기존 트윈 충돌 방지
        bgmSource.DOKill();

        // 부드럽게 볼륨 0으로 줄이고 정지
        bgmSource.DOFade(0f, crossFadeDuration)
            .SetUpdate(true) // TimeScale 무시
            .OnComplete(() =>
            {
                bgmSource.Stop();
                bgmSource.clip = null; // 클립 비우기 (선택사항)
            });
    }

    // PlaySFX 함수 (기존 로직 유지)
    private void PlaySFX(SoundData data, Vector3 position)
    {
        AudioSource source = GetAvailableSFXSource();

        source.clip = data.clip;
        source.volume = data.volume * masterSfxVolume;
        source.pitch = data.pitch;

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

    // 옵션 창에서 호출할 함수들
    public void SetBGMVolume(float volume)
    {
        masterBgmVolume = volume;
        if (bgmSource != null && bgmSource.isPlaying)
        {
            // 페이드 중일 수도 있으니 DOKill하고 즉시 적용하거나, 
            // 현재 진행중인 트윈이 없다면 바로 적용
            bgmSource.DOKill();
            bgmSource.volume = currentBgmClipVolume * masterBgmVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        masterSfxVolume = volume;
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (var source in sfxSources)
        {
            if (!source.isPlaying) return source;
        }

        AudioSource recycleSource = sfxSources[0];
        sfxSources.RemoveAt(0);
        sfxSources.Add(recycleSource);
        return recycleSource;
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