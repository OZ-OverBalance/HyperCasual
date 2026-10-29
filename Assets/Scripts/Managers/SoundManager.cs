using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : SingletonBase<SoundManager>
{
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    private readonly Dictionary<string, AudioClip> audioClipCache = new Dictionary<string, AudioClip>();

    private const string BGM_VOLUME_KEY = "BgmVolume";
    private const string SFX_VOLUME_KEY = "SfxVolume";

    public float SfxVolume => sfxSource.volume;
    public float BgmVolume => bgmSource.volume;

    protected override void Awake()
    {
        base.Awake();
        LoadSavedVolumes();
        AudioListener.volume = 0f;
    }

    private void LoadSavedVolumes()
    {
        float savedBgm = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.8f);
        float savedSfx = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.8f);

        SetBgmVolume(savedBgm);
        SetSfxVolume(savedSfx);
    }

    // 2D SFX 재생 (UI 클릭음)
    public void PlaySfx(string assetPath)
    {
        LoadAndPlayAudioClip(sfxSource, assetPath).Forget();
    }

    // 3D 특정 좌표 SFX
    public void PlaySfxAtPoint(string assetPath, Vector3 position, float volumeScale = 1.0f)
    {
        LoadAndPlayAudioClipAtPoint(assetPath, position, volumeScale).Forget();
    }

    // 장애물/오브젝트 3D 재생
    public void PlaySfxOnSource(AudioSource targetSource, string assetPath)
    {
        LoadAndPlayOnSource(targetSource, assetPath).Forget();
    }

    public void PlayBgm(string assetPath)
    {
        LoadAndPlayAudioClip(bgmSource, assetPath, isLoop: true).Forget();
    }

    // 2D 사운드 로드 재생
    private async UniTaskVoid LoadAndPlayAudioClip(AudioSource audioSource, string assetPath, bool isLoop = false)
    {
        AudioClip clip = await GetOrLoadAudioClipAsync(assetPath);
        if (clip == null) return;

        if (isLoop)
        {
            if (audioSource.clip == clip && audioSource.isPlaying)
            {
                return;
            }

            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.Play();
        }
        else
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // 지정 위치 3D 로드 재생
    private async UniTaskVoid LoadAndPlayAudioClipAtPoint(string assetPath, Vector3 position, float volumeScale)
    {
        AudioClip clip = await GetOrLoadAudioClipAsync(assetPath);
        if (clip == null) return;

        AudioSource.PlayClipAtPoint(clip, position, SfxVolume * volumeScale);
    }

    // 특정 타켓 로드 및 재생
    private async UniTaskVoid LoadAndPlayOnSource(AudioSource targetSource, string assetPath)
    {
        if (targetSource == null) return;

        AudioClip clip = await GetOrLoadAudioClipAsync(assetPath);
        if (clip == null) return;

        targetSource.PlayOneShot(clip, SfxVolume);
    }

    // 공통 비동기 로딩
    private async UniTask<AudioClip> GetOrLoadAudioClipAsync(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning("[SoundManager] assetPath 없음.");
            return null;
        }

        if (audioClipCache.TryGetValue(assetPath, out var cachedClip))
        {
            return cachedClip;
        }

        if (ResourceManager.Inst == null)
        {
            Debug.LogWarning("[SoundManager] ResourceManager.Inst 없음.");
            return null;
        }

        AudioClip clip = await ResourceManager.Inst.LoadAssetAsync<AudioClip>(assetPath);
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] '{assetPath}' 클립을 불러오지 못함.");
            return null;
        }

        audioClipCache[assetPath] = clip;
        return clip;
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void StopSFX()
    {
        sfxSource.Stop();
    }

    public void SetBgmVolume(float volume)
    {
        bgmSource.volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmSource.volume);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float volume)
    {
        sfxSource.volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxSource.volume);
        PlayerPrefs.Save();
    }
}
