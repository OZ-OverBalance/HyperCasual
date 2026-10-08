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
    }

    private void LoadSavedVolumes()
    {
        float savedBgm = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.8f);
        float savedSfx = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.8f);

        SetBgmVolume(savedBgm);
        SetSfxVolume(savedSfx);
    }

    public void PlaySfx(string assetPath)
    {
        LoadAndPlayAudioClip(sfxSource, assetPath).Forget();
    }

    public void PlayBgm(string assetPath)
    {
        LoadAndPlayAudioClip(bgmSource, assetPath, isLoop: true).Forget();
    }

    private async UniTaskVoid LoadAndPlayAudioClip(AudioSource audioSource, string assetPath, bool isLoop = false)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning($"SoundManager : assetPath 없음.");
            return;
        }

        AudioClip clip = null;

        if (audioClipCache.TryGetValue(assetPath, out var cachedClip))
        {
            clip = cachedClip;
        }
        else
        {
            if (ResourceManager.Inst == null)
            {
                Debug.LogWarning("[SoundManager] ResourceManager.Inst 없음.");
                return;
            }

            clip = await ResourceManager.Inst.LoadAssetAsync<AudioClip>(assetPath);
            if (clip == null)
            {
                Debug.LogWarning($"[SoundManager] '{assetPath}' 클립을 불러오지 못함.");
                return;
            }

            audioClipCache[assetPath] = clip;
        }

        if (isLoop)
        {
            if (audioSource == clip && audioSource.isPlaying)
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
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, bgmSource.volume);
        PlayerPrefs.Save();
    }
}
