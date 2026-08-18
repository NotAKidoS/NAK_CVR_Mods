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

    internal static bool EnsureBuilt()
    {
        if (_built) return true;

        // ReSharper disable twice ShaderLabShaderReferenceNotResolved
        Shader billboardShader = Shader.Find("Alpha Blend Interactive/BillboardFacing");
        Shader billboardTextShader = Shader.Find("Alpha Blend Interactive/TextMeshPro/Mobile/Distance Field-BillboardFacing");

        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        TMP_FontAsset font = fonts.FirstOrDefault(f => f.name == "NotoSans-Regular SDF HUD");
        TMP_FontAsset billboardFont = fonts.FirstOrDefault(f => f.name == "LiberationSans SDF BillboardFacing");

        if (font == null || billboardFont == null || billboardShader == null || billboardTextShader == null)
        {
            if (!_warned)
                CleanPlatesMod.Logger.Warning(
                    $"Game resources missing. Noto: {font != null}, Liberation: {billboardFont != null}, " +
                    $"Billboard: {billboardShader != null}, BillboardText: {billboardTextShader != null}");
            _warned = true;
            return false;
        }

        // Glyph coverage from Noto, billboarding and fade values from Liberation.
        // The client does not shit a billboarding NotoSans font so we need to build our
        // own here. This was a fucking pain in the ass to figure out without editor...
        Material atlas = font.material;
        Material preset = new(billboardFont.material)
        {
            name = font.name + " (Billboard)",
            shader = billboardTextShader,
            renderQueue = PlayerNameplate.RenderQueue
        };

        preset.SetTexture(ShaderUtilities.ID_MainTex, font.atlasTexture);
        preset.SetFloat(ShaderUtilities.ID_GradientScale, atlas.GetFloat(ShaderUtilities.ID_GradientScale));
        preset.SetFloat(ShaderUtilities.ID_TextureWidth, atlas.GetFloat(ShaderUtilities.ID_TextureWidth));
        preset.SetFloat(ShaderUtilities.ID_TextureHeight, atlas.GetFloat(ShaderUtilities.ID_TextureHeight));
        preset.SetFloat(ShaderUtilities.ID_WeightNormal, atlas.GetFloat(ShaderUtilities.ID_WeightNormal));
        preset.SetFloat(ShaderUtilities.ID_WeightBold, atlas.GetFloat(ShaderUtilities.ID_WeightBold));
        preset.SetFloat(ShaderUtilities.ID_ScaleRatio_A, atlas.GetFloat(ShaderUtilities.ID_ScaleRatio_A));
        preset.SetFloat(ShaderUtilities.ID_ScaleRatio_B, atlas.GetFloat(ShaderUtilities.ID_ScaleRatio_B));
        preset.SetFloat(ShaderUtilities.ID_ScaleRatio_C, atlas.GetFloat(ShaderUtilities.ID_ScaleRatio_C));

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