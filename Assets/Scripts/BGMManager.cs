using System.Collections;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("BGMManager");
                _instance = go.AddComponent<BGMManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    private static BGMManager _instance;

    private AudioSource _audioSource;
    private Coroutine _fadeCoroutine;
    private string _currentClipName;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure AudioSource defaults for BGM
        _audioSource.playOnAwake = false;
        _audioSource.loop = true;
        _audioSource.spatialBlend = 0f; // 2D Sound
        
        // Read initial volume setting
        float savedVolume = PlayerPrefs.GetFloat("VolumeKey", 1f);
        _audioSource.volume = savedVolume;
    }

    private void Update()
    {
        // Keep volume synchronized with AudioListener.volume or saved volume
        float targetVolume = PlayerPrefs.GetFloat("VolumeKey", 1f);
        if (_fadeCoroutine == null && !Mathf.Approximately(_audioSource.volume, targetVolume))
        {
            _audioSource.volume = targetVolume;
        }
    }

    public void PlayMusic(string clipPath, bool loop = true, float fadeDuration = 0.5f)
    {
        if (_currentClipName == clipPath) return; // Already playing

        _currentClipName = clipPath;
        AudioClip clip = Resources.Load<AudioClip>(clipPath);
        if (clip == null)
        {
            Debug.LogError($"[BGMManager] Failed to load BGM clip at Resources/{clipPath}");
            StopMusic(fadeDuration);
            return;
        }

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        _fadeCoroutine = StartCoroutine(FadeToMusicRoutine(clip, loop, fadeDuration));
    }

    public void StopMusic(float fadeDuration = 0.5f)
    {
        _currentClipName = null;
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _fadeCoroutine = StartCoroutine(FadeOutRoutine(fadeDuration));
    }

    private IEnumerator FadeToMusicRoutine(AudioClip newClip, bool loop, float duration)
    {
        float targetVolume = PlayerPrefs.GetFloat("VolumeKey", 1f);
        
        // 1. Fade out current music if playing
        if (_audioSource.isPlaying && _audioSource.volume > 0f)
        {
            float startVol = _audioSource.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _audioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }
        }

        _audioSource.Stop();
        _audioSource.clip = newClip;
        _audioSource.loop = loop;
        _audioSource.volume = 0f;
        _audioSource.Play();

        // 2. Fade in new music
        if (duration > 0.01f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _audioSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
                yield return null;
            }
        }
        
        _audioSource.volume = targetVolume;
        _fadeCoroutine = null;
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        if (_audioSource.isPlaying && _audioSource.volume > 0f)
        {
            float startVol = _audioSource.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _audioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }
        }

        _audioSource.Stop();
        _audioSource.clip = null;
        _fadeCoroutine = null;
    }
}
