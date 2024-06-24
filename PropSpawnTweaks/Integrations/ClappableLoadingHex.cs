using NAK.PropSpawnTweaks.Components;
using UnityEngine;

namespace NAK.PropSpawnTweaks.Integrations;

public static class TheClapperIntegration
{
    public static void Init()
    {
        PropSpawnTweaksMod.OnPropPlaceholderCreated += (placeholder) =>
        {
            if (placeholder.TryGetComponent(out PropLoadingHexagon loadingHexagon))
                ClappableLoadingHex.Create(loadingHexagon);
        };
    }
}

public class ClappableLoadingHex : Kafe.TheClapper.Clappable
{
    [SerializeField] private PropLoadingHexagon _loadingHexagon;
    
    public override void OnClapped(Vector3 clappablePosition) 
    {
        if (_loadingHexagon == null) return;
        _loadingHexagon.IsLoadingCanceled = true;
        Kafe.TheClapper.TheClapper.EmitParticles(clappablePosition, new Color(1f, 1f, 0f), 2f);
    }
    
    public static void Create(PropLoadingHexagon loadingHexagon) 
    {
        GameObject target = loadingHexagon.gameObject;
        if (!target.gameObject.TryGetComponent(out ClappableLoadingHex clappableHexagon))
            clappableHexagon = target.gameObject.AddComponent<ClappableLoadingHex>();
        clappableHexagon._loadingHexagon = loadingHexagon;
    }
}