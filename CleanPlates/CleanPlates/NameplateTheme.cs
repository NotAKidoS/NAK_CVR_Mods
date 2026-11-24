using ABI_RC.Core.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NAK.CleanPlates;

internal static class NameplateTheme
{
    private static readonly int FadeStart = Shader.PropertyToID("_FadeStartDistance");
    private static readonly int FadeEnd = Shader.PropertyToID("_FadeEndDistance");
    private static readonly int FirstPersonScaleVr = Shader.PropertyToID("_FirstPersonLocalNameplateScaleVr");
    private static readonly int FirstPersonScaleDesktop = Shader.PropertyToID("_FirstPersonLocalNameplateScaleDesktop");
    private static readonly int IsLocalPlayer = Shader.PropertyToID("_IsLocalPlayer");

    private static bool _built;
    private static bool _warned;
    private static TMP_FontAsset _font;
    private static Material _textMaterial;
    private static Material _graphicMaterial;
    private static Material _localTextMaterial;
    private static Material _localGraphicMaterial;

    internal static TMP_FontAsset Font => EnsureBuilt() ? _font : null;

    internal static Material TextMaterialFor(bool local)
        => EnsureBuilt() ? local ? _localTextMaterial : _textMaterial : null;

    internal static Material GraphicMaterialFor(bool local)
        => EnsureBuilt() ? local ? _localGraphicMaterial : _graphicMaterial : null;

    // Game nameplate text themes every plate
    internal static TMP_Text Source;

    internal static bool EnsureBuilt()
    {
        if (_built) return true;

        // ReSharper disable once ShaderLabShaderReferenceNotResolved
        Shader billboardShader = Shader.Find("Alpha Blend Interactive/BillboardFacing");
        TMP_FontAsset font = Source ? Source.font : null;
        Material nativeText = Source ? Source.fontSharedMaterial : null;

        if (font == null || nativeText == null || billboardShader == null)
        {
            if (!_warned)
                CleanPlatesMod.Logger.Warning(
                    $"Game resources missing. Font: {font != null}, Text: {nativeText != null}, Billboard: {billboardShader != null}");
            _warned = true;
            return false;
        }

        Material preset = new(nativeText)
        {
            name = font.name + " (CleanPlates)",
            renderQueue = PlayerNameplate.RenderQueue
        };

        Material graphicMaterial = new(billboardShader)
        {
            name = "CleanPlates Billboard",
            renderQueue = PlayerNameplate.RenderQueue
        };

        // The near fade is off on the shared materials, the manager hides plates
        // outright once you are close enough for them to be in the way. MakeLocal
        // puts the first person values back for the local player.
        KillFade(preset);
        KillFade(graphicMaterial);

        _font = font;
        _textMaterial = preset;
        _graphicMaterial = graphicMaterial;
        _localTextMaterial = MakeLocal(preset, "Local Text");
        _localGraphicMaterial = MakeLocal(graphicMaterial, "Local Billboard");
        _built = true;
        
        // Patch the prefabs.
        Apply(CleanPlatesMod.CleanPlatesPrefab.transform);
        Apply(CleanPlatesMod.CleanPlatesSimplePrefab.transform);
        Apply(CleanPlatesMod.CleanPlatesMiniPrefab.transform);
        return true;
    }
    
    public static void Apply(Transform root, bool local = false)
    {
        if (!EnsureBuilt()) return;

        Material text = local ? _localTextMaterial : _textMaterial;
        Material graphic = local ? _localGraphicMaterial : _graphicMaterial;

        foreach (Graphic component in root.GetComponentsInChildren<Graphic>(true))
        {
            if (component is TMP_Text tmp)
            {
                tmp.font = _font;
                tmp.fontSharedMaterial = text;
                continue;
            }
            component.material = graphic;
        }
    }

    private static void KillFade(Material material)
    {
        if (material.HasProperty(FadeStart)) material.SetFloat(FadeStart, 0f);
        if (material.HasProperty(FadeEnd)) material.SetFloat(FadeEnd, 0f);
    }

    private static Material MakeLocal(Material source, string name)
    {
        Material material = new(source) { name = source.name + " (" + name + ")" };
        if (material.HasProperty(FadeStart)) material.SetFloat(FadeStart, PlayerNameplate.LocalPlayerFadeStart);
        if (material.HasProperty(FadeEnd)) material.SetFloat(FadeEnd, PlayerNameplate.LocalPlayerFadeEnd);
        if (material.HasProperty(FirstPersonScaleVr)) material.SetFloat(FirstPersonScaleVr, PlayerNameplate.FirstPersonLocalScaleVr);
        if (material.HasProperty(FirstPersonScaleDesktop)) material.SetFloat(FirstPersonScaleDesktop, PlayerNameplate.FirstPersonLocalScaleDesktop);
        if (material.HasProperty(IsLocalPlayer)) material.SetFloat(IsLocalPlayer, 1f);
        return material;
    }
}