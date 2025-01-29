using MagicaCloth;
using MagicaCloth2;
using UnityEngine;

namespace NAK.AvatarCloneTest;

public partial class AvatarClone
{
    #region Magica Cloth Support

    private void SetupMagicaClothSupport()
    {
        var magicaCloths1 = GetComponentsInChildren<MagicaRenderDeformer>(true);
        foreach (MagicaRenderDeformer magicaCloth in magicaCloths1)
        {
            // Get the renderer on the same object
            Renderer renderer = magicaCloth.gameObject.GetComponent<Renderer>();
            SetRendererNeedsAdditionalChecks(renderer, true);
        }
        
        var magicaCloths2 = GetComponentsInChildren<MagicaCloth2.MagicaCloth>(true);
        foreach (MagicaCloth2.MagicaCloth magicaCloth in magicaCloths2)
        {
            if (magicaCloth.serializeData.clothType != ClothProcess.ClothType.MeshCloth)
                continue; // Only matters for cloth physics
            
            // Set the affected renderers as requiring extra checks
            var renderers = magicaCloth.serializeData.sourceRenderers;
            foreach (Renderer renderer in renderers) SetRendererNeedsAdditionalChecks(renderer, true);
        }
    }

    #endregion Magica Cloth Support
}