using ABI_RC.Core;
using UnityEngine;

namespace NAK.PlapPlapForAll;

public enum DPSLightType
{
    Invalid, 
    Hole, 
    Ring, 
    Normal,
    Tip
}

public struct DPSOrifice
{
    public DPSLightType type;
    public Light dpsLight;
    public Light normalLight;
    public Transform basis;
}

public struct DPSPenetrator
{
    public Transform penetratorTransform;
    public float length; // _Length // _TPS_PenetratorLength
}

public static class DPS
{
    private static readonly int Length = Shader.PropertyToID("_Length");
    private static readonly int TpsPenetratorLength = Shader.PropertyToID("_TPS_PenetratorLength");

    public static DPSLightType GetOrificeType(Light light)
    {
        int encoded = Mathf.RoundToInt(Mathf.Repeat((light.range * 500f) + 500f, 50f) + 200f);
        return encoded switch
        {
            205 => DPSLightType.Hole, // 0.41
            210 => DPSLightType.Ring, // 0.42
            225 => DPSLightType.Normal, // 0.45
            245 => DPSLightType.Tip, // 0.49
            _ => DPSLightType.Invalid
        };
    }
    
    public static bool ScanForDPS(
        GameObject rootObject, 
        out List<DPSOrifice> dpsOrifices,
        out bool foundPenetrator)
    {
        dpsOrifices = null;
        foundPenetrator = false;
        
        // Scan for DPS
        Light[] allLights = rootObject.GetComponentsInChildren<Light>(true);
        int lightCount = allLights.Length;
        if (lightCount == 0) return false;
        
        // DPS setups are usually like this:
        // - Empty Container
        //   - Light (Type light) (range set to 0.x1 for hole or 0.x2 for ring)
        //   - Light (Normal light) (range set to 0.x5)
        
        for (int i = 0; i < lightCount; i++)
        {
            Light light = allLights[i];
            
            DPSLightType orificeType = GetOrificeType(light);

            if (orificeType == DPSLightType.Tip)
            {
                foundPenetrator = true;
                continue;
            }
            
            if (orificeType is DPSLightType.Hole or DPSLightType.Ring)
            {
                Transform lightTransform = light.transform;
                Transform parent = lightTransform.parent;
                
                // Found a DPS light
                DPSOrifice dpsOrifice = new()
                {
                    type = orificeType,
                    dpsLight = light,
                    normalLight = null,
                    basis = parent // Assume parent is basis
                };
                
                // Try to find normal light sibling
                foreach (Transform sibling in parent)
                {
                    if (sibling == lightTransform) continue;
                    if (sibling.TryGetComponent(out Light siblingLight) 
                        && GetOrificeType(siblingLight) == DPSLightType.Normal)
                    {
                        dpsOrifice.normalLight = siblingLight;
                        break;
                    }
                }
                
                dpsOrifices ??= [];
                dpsOrifices.Add(dpsOrifice);
            }
        }
        
        return dpsOrifices is { Count: > 0 } || foundPenetrator;
    }
    
    public static void AttemptTPSHack(GameObject rootObject)
    {
        Renderer[] allRenderers = rootObject.GetComponentsInChildren<Renderer>(true);
        int count = allRenderers.Length;
        if (count == 0) return;

        SkinnedDickFixRoot fixRoot = rootObject.AddComponent<SkinnedDickFixRoot>();

        for (int i = 0; i < count; i++)
        {
            Renderer render = allRenderers[i];
            if (render.gameObject.CompareTag(CVRTags.InternalObject))
                continue;

            Material mat = render.sharedMaterial;
            if (!mat) continue;

            float length = 0f;
            if (mat.HasProperty(TpsPenetratorLength)) length += mat.GetFloat(TpsPenetratorLength);
            if (mat.HasProperty(Length)) length += mat.GetFloat(Length);
            if (length <= 0f) continue;

            Transform dpsRoot;
            SkinnedMeshRenderer smr = render as SkinnedMeshRenderer;
            if (smr && smr.rootBone)
                dpsRoot = smr.rootBone;
            else
                dpsRoot = render.transform;

            fixRoot.Register(render, dpsRoot, length);

            PlapPlapForAllMod.Logger.Msg(
                $"Added shared DPS penetrator light for mesh '{render.name}' in object '{rootObject.name}'.");
        }
    }
}