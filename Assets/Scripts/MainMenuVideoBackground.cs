using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Cách co giãn video nền:
/// - Stretch  : Kéo video phủ đầy màn hình (hiển thị đủ nội dung, hơi biến dạng nếu tỷ lệ khác nhau nhiều)
/// - Cover    : Crop cạnh để phủ đầy màn hình không biến dạng (mất phần mép)
/// - Contain  : Hiển thị toàn bộ video, có thể xuất hiện thanh đen ở 2 bên/trên-dưới
/// </summary>
public enum VideoScaleMode
{
    Stretch,   // Kéo dãn - hiển thị đủ nội dung, lấp đầy màn hình
    Cover,     // Crop mép - lấp đầy màn hình, không biến dạng, mất phần rìa
    Contain    // Giữ nguyên tỷ lệ - hiển thị đủ, có thể có letterbox
}

[RequireComponent(typeof(RawImage))]
[RequireComponent(typeof(VideoPlayer))]
public class MainMenuVideoBackground : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoClip videoClip;

    [Header("Scale Mode")]
    [Tooltip("Stretch: lấp đầy màn hình, hiển thị đủ nội dung (khuyến nghị).\n" +
             "Cover: lấp đầy, không biến dạng, cắt bỏ phần rìa.\n" +
             "Contain: hiển thị toàn bộ video, có thể có viền đen.")]
    public VideoScaleMode scaleMode = VideoScaleMode.Stretch;

    private VideoPlayer videoPlayer;
    private RawImage displayImage;
    private RenderTexture renderTexture;
    private Coroutine playRoutine;
    private bool isVideoReady;

    private void Awake()
    {
        displayImage = GetComponent<RawImage>();
        videoPlayer  = GetComponent<VideoPlayer>();

        displayImage.raycastTarget = false;

        if (videoClip == null)
            videoClip = Resources.Load<VideoClip>("MenuBG");
    }

    private void OnEnable()
    {
        if (videoClip == null)
        {
            Debug.LogError("[VideoBackground] No video clip assigned and Resources/MenuBG not found.");
            return;
        }

        if (!isVideoReady)
            SetupVideoPlayer();

        if (videoPlayer.isPrepared)
            videoPlayer.Play();
        else
            playRoutine = StartCoroutine(PrepareAndPlayRoutine());
    }

    private void OnDisable()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Pause();
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  Setup
    // ────────────────────────────────────────────────────────────────────────────

    private void SetupVideoPlayer()
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping   = true;
        videoPlayer.skipOnDrop  = true;
        videoPlayer.clip        = videoClip;
        videoPlayer.source      = VideoSource.VideoClip;
        videoPlayer.renderMode  = VideoRenderMode.RenderTexture;

        DestroyExistingFitter();
        ForceFillParent();

        // Temporary 1×1; rebuilt after Prepare() when we know real video dimensions.
        CreateRenderTexture(1, 1);

        // Ensure RawImage stretches to fill its parent
        var rt = displayImage.rectTransform;
        rt.anchorMin      = Vector2.zero;
        rt.anchorMax      = Vector2.one;
        rt.offsetMin      = Vector2.zero;
        rt.offsetMax      = Vector2.zero;
        rt.localScale     = Vector3.one;
        rt.anchoredPosition = Vector2.zero;

        // Audio
        var audio = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        audio.playOnAwake             = false;
        videoPlayer.audioOutputMode   = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audio);
    }

    /// <summary>Creates (or recreates) the RenderTexture at the given size.</summary>
    private void CreateRenderTexture(int width, int height)
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }

        renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        renderTexture.Create();

        videoPlayer.targetTexture = renderTexture;
        displayImage.texture      = renderTexture;
        displayImage.uvRect       = new Rect(0, 0, 1, 1);
    }

    private void DestroyExistingFitter()
    {
        var fitter = GetComponent<AspectRatioFitter>();
        if (fitter != null) Destroy(fitter);
    }

    private void ForceFillParent()
    {
        var self = GetComponent<RectTransform>();
        self.anchorMin      = Vector2.zero;
        self.anchorMax      = Vector2.one;
        self.offsetMin      = Vector2.zero;
        self.offsetMax      = Vector2.zero;
        self.localScale     = Vector3.one;
        self.anchoredPosition = Vector2.zero;

        if (transform.parent != null)
        {
            var parentRT = transform.parent.GetComponent<RectTransform>();
            if (parentRT != null)
            {
                parentRT.anchorMin      = Vector2.zero;
                parentRT.anchorMax      = Vector2.one;
                parentRT.offsetMin      = Vector2.zero;
                parentRT.offsetMax      = Vector2.zero;
                parentRT.localScale     = Vector3.one;
                parentRT.anchoredPosition = Vector2.zero;

                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRT);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(self);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  Prepare & Play coroutine
    // ────────────────────────────────────────────────────────────────────────────

    private IEnumerator PrepareAndPlayRoutine()
    {
        bool prepareError = false;
        string errorMsg   = "";

        void OnVideoError(VideoPlayer vp, string msg)    { prepareError = true; errorMsg = msg; }
        void OnVideoPrepared(VideoPlayer vp)             { Debug.Log("[VideoBackground] Prepared OK."); }

        videoPlayer.errorReceived    += OnVideoError;
        videoPlayer.prepareCompleted += OnVideoPrepared;

        videoPlayer.Prepare();

        float timeout = 8f, elapsed = 0f;
        while (!videoPlayer.isPrepared && !prepareError && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        videoPlayer.errorReceived    -= OnVideoError;
        videoPlayer.prepareCompleted -= OnVideoPrepared;

        if (prepareError)
        {
            Debug.LogError($"[VideoBackground] Prepare failed: {errorMsg}");
            playRoutine = null;
            yield break;
        }

        if (!videoPlayer.isPrepared)
        {
            Debug.LogWarning("[VideoBackground] Prepare timed out. Retrying once...");
            videoPlayer.Prepare();
            yield return new WaitForSecondsRealtime(3f);

            if (!videoPlayer.isPrepared)
            {
                Debug.LogError("[VideoBackground] Video not prepared after retry.");
                playRoutine = null;
                yield break;
            }
        }

        isVideoReady = true;

        // Rebuild RenderTexture to actual video dimensions
        uint vw = videoPlayer.width;
        uint vh = videoPlayer.height;
        if (vw > 0 && vh > 0)
        {
            Debug.Log($"[VideoBackground] Video: {vw}x{vh}. Rebuilding RenderTexture.");
            CreateRenderTexture((int)vw, (int)vh);
        }

        displayImage.color = Color.white;

        // Apply chosen scale mode
        ApplyScaleMode();

        videoPlayer.Play();
        playRoutine = null;
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  Scale Mode application
    // ────────────────────────────────────────────────────────────────────────────

    private void ApplyScaleMode()
    {
        if (videoPlayer == null || !videoPlayer.isPrepared) return;

        float videoW = videoPlayer.width;
        float videoH = videoPlayer.height;
        if (videoW <= 0 || videoH <= 0) return;

        // Get display size from RectTransform; fall back to Canvas reference resolution
        float dispW = displayImage.rectTransform.rect.width;
        float dispH = displayImage.rectTransform.rect.height;

        if (dispW <= 0 || dispH <= 0)
        {
            var parentCanvas = displayImage.canvas;
            var scaler = parentCanvas != null ? parentCanvas.GetComponent<CanvasScaler>() : null;
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                dispW = scaler.referenceResolution.x;
                dispH = scaler.referenceResolution.y;
            }
            else
            {
                dispW = 1920f;
                dispH = 1080f;
            }
        }

        float videoAspect = videoW / videoH;
        float dispAspect  = dispW  / dispH;

        Rect uv = new Rect(0, 0, 1, 1); // default = Stretch (full uvRect = no crop)

        switch (scaleMode)
        {
            case VideoScaleMode.Stretch:
                // uvRect (0,0,1,1) → RawImage stretches texture to fill — shows all content
                uv = new Rect(0, 0, 1, 1);
                break;

            case VideoScaleMode.Cover:
                // Crop edges to fill display without distortion
                if (videoAspect > dispAspect)
                {
                    // Video wider → crop left/right
                    float uW = dispAspect / videoAspect;
                    uv = new Rect((1f - uW) * 0.5f, 0f, uW, 1f);
                }
                else if (videoAspect < dispAspect)
                {
                    // Video taller → crop top/bottom
                    float uH = videoAspect / dispAspect;
                    uv = new Rect(0f, (1f - uH) * 0.5f, 1f, uH);
                }
                break;

            case VideoScaleMode.Contain:
                // Show full video; letterbox bars will appear
                // We do this by adjusting the RawImage size via the RectTransform instead of uvRect
                ApplyContainLayout(videoAspect, dispW, dispH);
                return; // layout-based, no uvRect change needed
        }

        Debug.Log($"[VideoBackground] ScaleMode={scaleMode} | video:{videoW}x{videoH} ({videoAspect:F3}) | disp:{dispW}x{dispH} ({dispAspect:F3}) | uvRect:{uv}");
        displayImage.uvRect = uv;
    }

    /// <summary>
    /// Contain mode: resize the RawImage RectTransform to fit inside the canvas
    /// while preserving the video's aspect ratio. Leaves letterbox/pillarbox empty.
    /// </summary>
    private void ApplyContainLayout(float videoAspect, float dispW, float dispH)
    {
        float dispAspect = dispW / dispH;
        float targetW, targetH;

        if (videoAspect > dispAspect)
        {
            // Video wider → fit by width
            targetW = dispW;
            targetH = dispW / videoAspect;
        }
        else
        {
            // Video taller or equal → fit by height
            targetH = dispH;
            targetW = dispH * videoAspect;
        }

        var rt = displayImage.rectTransform;
        rt.anchorMin      = new Vector2(0.5f, 0.5f);
        rt.anchorMax      = new Vector2(0.5f, 0.5f);
        rt.pivot          = new Vector2(0.5f, 0.5f);
        rt.sizeDelta      = new Vector2(targetW, targetH);
        rt.anchoredPosition = Vector2.zero;

        displayImage.uvRect = new Rect(0, 0, 1, 1);
        Debug.Log($"[VideoBackground] Contain → RawImage size set to {targetW}x{targetH}");
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  Cleanup
    // ────────────────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }
    }
}
