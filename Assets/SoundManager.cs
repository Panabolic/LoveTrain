using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("사운드 데이터 등록 (드래그 & 드롭)")]
    [SerializeField] private List<SoundData> soundList;

    [Header("설정")]
    [SerializeField] private int sfxPoolSize = 15; // 효과음 동시 재생 한계

    // 빠른 검색을 위한 딕셔너리
    private Dictionary<SoundID, SoundData> soundMap = new Dictionary<SoundID, SoundData>();

    // 오디오 소스 풀링
    private List<AudioSource> sfxSources;
    private AudioSource bgmSource;

    private float masterBgmVolume = 1f;
    private float masterSfxVolume = 1f;
    private float currentBgmClipVolume = 1f;


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
        // 1. 데이터 딕셔너리로 변환 (검색 속도 UP)
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

    // ✨ 이벤트 버스 구독 (액션 연결)
    private void OnEnable()
    {
        SoundEventBus.OnPlaySound += PlaySoundHandler;
    }

    // ✨ 이벤트 버스 구독 해제
    private void OnDisable()
    {
        SoundEventBus.OnPlaySound -= PlaySoundHandler;
    }

    // 실제 사운드 재생 로직
    private void PlaySoundHandler(SoundID id, Vector3 position)
    {
        if (!soundMap.TryGetValue(id, out SoundData data))
        {
            Debug.LogWarning($"[SoundManager] 등록되지 않은 사운드 ID: {id}");
            return;
        }

        // BGM 처리
        if (id.ToString().StartsWith("BGM"))
        {
            PlayBGM(data);
        }
        // SFX 처리
        else
        {
            PlaySFX(data, position);
        }
    }

    // --- [수정] PlayBGM 함수 (볼륨 적용 로직 추가) ---
    private void PlayBGM(SoundData data)
    {
        if (bgmSource.clip == data.clip) return;

        bgmSource.clip = data.clip;
        currentBgmClipVolume = data.volume; // 원래 볼륨 저장
        bgmSource.volume = currentBgmClipVolume * masterBgmVolume; // 마스터 볼륨 반영
        bgmSource.pitch = data.pitch;
        bgmSource.Play();
    }

    // --- [수정] PlaySFX 함수 (볼륨 적용 로직 추가) ---
    private void PlaySFX(SoundData data, Vector3 position)
    {
        AudioSource source = GetAvailableSFXSource();

        source.clip = data.clip;
        source.volume = data.volume * masterSfxVolume; // 마스터 볼륨 반영
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

    // --- [추가] 옵션 창에서 호출할 함수들 ---
    public void SetBGMVolume(float volume)
    {
        masterBgmVolume = volume;
        // 현재 재생 중인 BGM이 있다면 즉시 볼륨 변경
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.volume = currentBgmClipVolume * masterBgmVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        masterSfxVolume = volume;
        // SFX는 보통 짧아서 재생 중인 것까지 즉시 바꾸진 않지만, 필요하면 여기서 순회 가능
    }

    private AudioSource GetAvailableSFXSource()
    {
        // 노는 오디오 소스 찾기
        foreach (var source in sfxSources)
        {
            if (!source.isPlaying) return source;
        }

        // 없으면 제일 오래된 놈(0번) 뺏어오기 (순환)
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
            // Pitch가 0인 경우는 소리가 안 나므로, 초기화되지 않은 상태로 간주
            if (data.pitch == 0f)
            {
                data.pitch = 1f;  // 피치 기본값 복구
                data.volume = 1f; // 볼륨 기본값 복구
            }
        }

    }
    public float GetBGMVolume() => masterBgmVolume;
    public float GetSFXVolume() => masterSfxVolume;
}