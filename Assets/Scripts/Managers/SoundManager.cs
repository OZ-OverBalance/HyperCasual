using Cysharp.Threading.Tasks;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class SoundManager : SingletonBase<SoundManager>
{
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    public float SfxVolume => sfxSource.volume;
    public float BgmVolume => bgmSource.volume;

    protected override void Awake()
    {
        base.Awake();
    }

    public void PlaySfx(string assetPath)
    {
        LoadAndPlayAudioClip(sfxSource, assetPath).Forget();
    }

    public void PlayBgm(string assetPath)
    {
        LoadAndPlayAudioClip(bgmSource, assetPath).Forget();
    }

    private static async UniTaskVoid LoadAndPlayAudioClip(AudioSource audioSource, string assetPath, bool isLoop = false)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning($"SoundManager : '{assetPath}'없음.");
            return;
        }

        if (ResourceManager.Inst == null)
        {
            Debug.LogWarning($"SoundManager : ResourceManager.Inst 없음.");
            return;
        }

        AudioClip clip = await ResourceManager.Inst.LoadAssetAsync<AudioClip>(assetPath);
        if (clip == null)
        {
            Debug.LogWarning($"SoundManager : '{assetPath}' 클립을 불러오지 못함.");
            return;
        }

        if (isLoop)
        {
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
    }

    public void SetSfxVolume(float volume)
    {
        sfxSource.volume = Mathf.Clamp01(volume);
    }
}
