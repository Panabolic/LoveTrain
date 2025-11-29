using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    public List<AudioSource> audioSourcePool;
    private Dictionary<string, List<AudioClip>> soundGroups;
    public AudioSource bgmSource;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSoundGroups();
            InitializeAudioSourcePool();
            InitializeBGMSource();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioSourcePool()
    {
        audioSourcePool = new List<AudioSource>();

        for (int i = 0; i < 10; i++)
        {
            GameObject audioSourceObj = new GameObject("PooledAudioSource");
            audioSourceObj.transform.SetParent(transform);
            AudioSource audioSource = audioSourceObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.0f; // 2D 사운드로 설정
            audioSourcePool.Add(audioSource);
        }
    }

    private void InitializeBGMSource()
    {
        GameObject bgmSourceObj = new GameObject("BGMSource");
        bgmSourceObj.transform.SetParent(transform);
        bgmSource = bgmSourceObj.AddComponent<AudioSource>();
        bgmSource.loop = true; // BGM은 반복 재생
        bgmSource.spatialBlend = 0.0f; // 2D 사운드로 설정
    }

    private void LoadSoundGroups()
    {
        soundGroups = new Dictionary<string, List<AudioClip>>();

        // 각 그룹에 사운드 클립을 로드하는 로직
        soundGroups["Keyword"] = new List<AudioClip>(Resources.LoadAll<AudioClip>("Sounds/Keyword"));
        soundGroups["Book"] = new List<AudioClip>(Resources.LoadAll<AudioClip>("Sounds/Book"));
        soundGroups["Debuff"] = new List<AudioClip>(Resources.LoadAll<AudioClip>("Sounds/Debuff"));
        soundGroups["Character"] = new List<AudioClip>(Resources.LoadAll<AudioClip>("Sounds/Character"));
        soundGroups["BGM"] = new List<AudioClip>(Resources.LoadAll<AudioClip>("Sounds/BGM"));
        soundGroups["Shop"] = new List<AudioClip>(Resources.LoadAll<AudioClip>("Sounds/Shop"));
    }

    public void PlaySound(string group, string clipName)
    {
        if (soundGroups.TryGetValue(group, out List<AudioClip> clips))
        {
            AudioClip clip = clips.Find(c => c.name == clipName);
            if (clip != null)
            {
                AudioSource audioSource = GetAudioSource();
                audioSource.clip = clip;
                audioSource.Play();
                StartCoroutine(ReturnAfterPlaying(audioSource, clip.length));
            }
        }
    }

    public void PlayBGM(string clipName)
    {
        if (soundGroups.TryGetValue("BGM", out List<AudioClip> clips))
        {
            AudioClip clip = clips.Find(c => c.name == clipName);
            if (clip != null && bgmSource.clip != clip)
            {
                /*                bgmSource.volume *= 0.5f;*/
                bgmSource.clip = clip;
                bgmSource.Play();
            }
        }
    }

    public void StopBGM()
    {
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void UpdateBGM()
    {
        switch (GameManager.Instance.CurrentState)
        {
            case GameState.Start:
                PlayBGM("타이틀화면");
                break;
            case GameState.Playing:
                PlayBGM("플레이브금");
                break;
            case GameState.Event:
                PlayBGM("게임오버");
                break;
            case GameState.Boss:
                PlayBGM("보스브금");
                break;
            case GameState.Die:
                PlayBGM("사망");
                break;
            default:
                break;
        }
    }

    private AudioSource GetAudioSource()
    {
        foreach (AudioSource audioSource in audioSourcePool)
        {
            if (!audioSource.isPlaying)
            {
                return audioSource;
            }
        }

        // 사용 가능한 오디오 소스가 없으면 새로운 오디오 소스를 생성
        GameObject audioSourceObj = new GameObject("PooledAudioSource");
        audioSourceObj.transform.SetParent(transform);
        AudioSource newAudioSource = audioSourceObj.AddComponent<AudioSource>();
        newAudioSource.playOnAwake = false;
        newAudioSource.spatialBlend = 0.0f; // 2D 사운드로 설정
        audioSourcePool.Add(newAudioSource);
        return newAudioSource;
    }

    private void ReturnAudioSource(AudioSource audioSource)
    {
        audioSource.clip = null;
    }

    private IEnumerator ReturnAfterPlaying(AudioSource audioSource, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnAudioSource(audioSource);
    }

    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume;
    }

    public float GetBGMVolume()
    {
        return bgmSource.volume;
    }

    public void SetSFXVolume(float volume)
    {
        foreach (var audioSource in audioSourcePool)
        {
            audioSource.volume = volume;
        }
    }

    public float GetSFXVolume()
    {
        if (audioSourcePool.Count > 0)
        {
            return audioSourcePool[0].volume;
        }
        return 1.0f;
    }
}

