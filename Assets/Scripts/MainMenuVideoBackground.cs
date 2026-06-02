using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class MainMenuVideoBackground : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public RawImage videoRawImage;
    public Image fallbackImage;
    public VideoClip menuClip;

    private void Awake()
    {
        if (videoPlayer == null || videoRawImage == null || fallbackImage == null)
        {
            Debug.LogWarning("MainMenuVideoBackground has missing references. Fallback image will remain active.");
            ShowFallbackOnly();
            return;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.skipOnDrop = true;

        if (menuClip != null)
        {
            videoPlayer.clip = menuClip;
        }

        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.prepareCompleted += OnPrepareCompleted;

        if (videoPlayer.clip == null)
        {
            Debug.LogWarning("MainMenuVideoBackground: No video clip assigned, using fallback image.");
            ShowFallbackOnly();
            return;
        }

        fallbackImage.gameObject.SetActive(true);
        videoRawImage.gameObject.SetActive(false);
        videoPlayer.Prepare();
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.prepareCompleted -= OnPrepareCompleted;
        }
    }

    private void OnPrepareCompleted(VideoPlayer source)
    {
        if (source == null || source.texture == null)
        {
            ShowFallbackOnly();
            return;
        }

        videoRawImage.texture = source.texture;
        videoRawImage.gameObject.SetActive(true);
        fallbackImage.gameObject.SetActive(false);
        source.Play();
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning($"MainMenuVideoBackground video error: {message}");
        ShowFallbackOnly();
    }

    private void ShowFallbackOnly()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        if (videoRawImage != null)
        {
            videoRawImage.gameObject.SetActive(false);
        }

        if (fallbackImage != null)
        {
            fallbackImage.gameObject.SetActive(true);
        }
    }
}
