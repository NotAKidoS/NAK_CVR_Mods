using System.Collections.Generic;
using System.Reflection;
using ABI.CCK.Components;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.FixVideoPlayerRenderTextureResolution;

public class FixVideoPlayerRenderTextureResolutionMod : MelonMod
{
    private static MelonLogger.Instance Logger;

    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(FixVideoPlayerRenderTextureResolution));

    private static readonly MelonPreferences_Entry<bool> EntryMatchVideoResolution =
        Category.CreateEntry(
            identifier: "match_video_resolution",
            true,
            display_name: "Match Video Resolution",
            description: "Resize the projection texture so the letterboxed video region renders at native resolution. Disabling restores the worlds texture size.");

    private static readonly MelonPreferences_Entry<int> EntryMaxTextureSize =
        Category.CreateEntry(
            identifier: "max_texture_size",
            8192,
            display_name: "Max Texture Size",
            description: "Upper bound on either projection texture axis. Lower if VRAM is a concern.");

    private static readonly MelonPreferences_Entry<bool> EntryFitFullSideBySide =
        Category.CreateEntry(
            identifier: "fit_full_side_by_side",
            false,
            display_name: "Fit Full Side-By-Side 3D",
            description: "Videos wider than 3:1 (like 3840x1080) are treated as two eyes side by side and fit per eye, so 3D screens are not squished. Disable for ultrawide 2D videos.");

    #endregion Melon Preferences

    // counters oblique-angle mip selection (HMD lens distortion, high fov desktop) blurring the texture at edges?
    private const float MipMapBias = -0.5f;
    private const int MinTextureSize = 16;
    private const float FullSideBySideMinAspect = 3f;

    private static readonly int ScaleProperty = Shader.PropertyToID("_Scale");
    private static readonly AccessTools.FieldRef<CVRVideoPlayer, Material> BlitMaterial =
        AccessTools.FieldRefAccess<CVRVideoPlayer, Material>("_blitMaterial");

    private static readonly Dictionary<CVRVideoPlayer, Vector2Int> DefaultProjectionSizes = new();

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        HarmonyInstance.Patch(
            typeof(CVRVideoPlayer).GetMethod(nameof(CVRVideoPlayer.UpdateAspectRatio),
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(FixVideoPlayerRenderTextureResolutionMod).GetMethod(nameof(OnCVRVideoPlayerUpdateAspectRatio),
                BindingFlags.NonPublic | BindingFlags.Static)),
            postfix: new HarmonyMethod(typeof(FixVideoPlayerRenderTextureResolutionMod).GetMethod(nameof(OnCVRVideoPlayerUpdateAspectRatioPost),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        HarmonyInstance.Patch(
            typeof(CVRVideoPlayer).GetMethod(nameof(CVRVideoPlayer.OnDestroy),
                BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(FixVideoPlayerRenderTextureResolutionMod).GetMethod(nameof(OnCVRVideoPlayerDestroy),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        EntryMatchVideoResolution.OnEntryValueChanged.Subscribe((_, _) => ApplyProjectionTextureResolutionToAll());
        EntryMaxTextureSize.OnEntryValueChanged.Subscribe((_, _) => ApplyProjectionTextureResolutionToAll());
        EntryFitFullSideBySide.OnEntryValueChanged.Subscribe((_, _) => ApplyProjectionTextureResolutionToAll());
    }

    private static void OnCVRVideoPlayerUpdateAspectRatio(CVRVideoPlayer __instance)
        => ApplyProjectionTextureResolution(__instance);

    private static void OnCVRVideoPlayerUpdateAspectRatioPost(CVRVideoPlayer __instance)
    {
        if (!EntryFitFullSideBySide.Value) return;

        RenderTexture projectionTexture = __instance.ProjectionTexture;
        Material blitMaterial = BlitMaterial(__instance);
        var videoInfo = __instance.VideoPlayer?.Info?.VideoMetaData;
        if (projectionTexture == null || blitMaterial == null || videoInfo == null) return;

        int videoWidth = videoInfo.GetVideoWidth();
        int videoHeight = videoInfo.GetVideoHeight();
        if (videoWidth <= 0 || videoHeight <= 0) return;

        float videoAspect = (float)videoWidth / videoHeight;
        if (videoAspect < FullSideBySideMinAspect) return;

        // each half is one eye and the world 3D shader stretches it back across the screen, so fit one eye
        float eyeAspect = videoAspect / 2f;
        float textureAspect = (float)projectionTexture.width / projectionTexture.height;
        Vector4 scale = eyeAspect > textureAspect
            ? new Vector4(1f, textureAspect / eyeAspect)
            : new Vector4(eyeAspect / textureAspect, 1f);

        Vector4 nativeScale = blitMaterial.GetVector(ScaleProperty);
        scale.x *= Mathf.Sign(nativeScale.x);
        scale.y *= Mathf.Sign(nativeScale.y);
        blitMaterial.SetVector(ScaleProperty, scale);
    }

    private static void OnCVRVideoPlayerDestroy(CVRVideoPlayer __instance)
        => DefaultProjectionSizes.Remove(__instance);

    private static void ApplyProjectionTextureResolution(CVRVideoPlayer videoPlayer)
    {
        RenderTexture projectionTexture = videoPlayer.ProjectionTexture;
        if (projectionTexture == null) return;

        if (!DefaultProjectionSizes.TryGetValue(videoPlayer, out Vector2Int defaultSize))
        {
            defaultSize = new Vector2Int(projectionTexture.width, projectionTexture.height);
            DefaultProjectionSizes[videoPlayer] = defaultSize;
        }

        int width;
        int height;
        if (EntryMatchVideoResolution.Value)
        {
            var videoInfo = videoPlayer.VideoPlayer?.Info?.VideoMetaData;
            if (videoInfo == null) return;

            int videoWidth = videoInfo.GetVideoWidth();
            int videoHeight = videoInfo.GetVideoHeight();
            if (videoWidth <= 0 || videoHeight <= 0) return;

            // UpdateAspectRatio fits the video inside the texture and leaves the rest as bars, so the texture has to
            // overshoot by the letterbox amount for the fitted region to land at native resolution. the worlds aspect
            // must be preserved because the screen mesh was built for it
            float screenAspect = (float)defaultSize.x / defaultSize.y;
            if ((float)videoWidth / videoHeight > screenAspect)
            {
                width = videoWidth;
                height = Mathf.RoundToInt(videoWidth / screenAspect);
            }
            else
            {
                height = videoHeight;
                width = Mathf.RoundToInt(videoHeight * screenAspect);
            }

            int maxSize = Mathf.Clamp(EntryMaxTextureSize.Value, MinTextureSize, SystemInfo.maxTextureSize);
            float downscale = Mathf.Min(1f, maxSize / (float)Mathf.Max(width, height));
            width = Mathf.Max(MinTextureSize, (Mathf.RoundToInt(width * downscale) + 1) & ~1);
            height = Mathf.Max(MinTextureSize, (Mathf.RoundToInt(height * downscale) + 1) & ~1);
        }
        else
        {
            width = defaultSize.x;
            height = defaultSize.y;
        }

        Vector2Int previousSize = new(projectionTexture.width, projectionTexture.height);
        bool needsBias = projectionTexture.useMipMap && !Mathf.Approximately(projectionTexture.mipMapBias, MipMapBias);
        if (previousSize.x == width && previousSize.y == height && !needsBias) return;

        projectionTexture.Release();
        projectionTexture.width = width;
        projectionTexture.height = height;
        if (projectionTexture.useMipMap)
        {
            projectionTexture.mipMapBias = MipMapBias;
            projectionTexture.anisoLevel = Mathf.Max(projectionTexture.anisoLevel, 8);
        }
        projectionTexture.Create();

        Logger.Msg($"Changed video player {videoPlayer.name} resolution: {previousSize.x}x{previousSize.y} -> {width}x{height} " +
                   $"(world default {defaultSize.x}x{defaultSize.y})");
    }

    private static void ApplyProjectionTextureResolutionToAll()
    {
        foreach (CVRVideoPlayer videoPlayer in new List<CVRVideoPlayer>(DefaultProjectionSizes.Keys))
        {
            if (videoPlayer.VideoPlayer?.Info?.VideoMetaData == null) continue;
            videoPlayer.UpdateAspectRatio();
        }
    }
}