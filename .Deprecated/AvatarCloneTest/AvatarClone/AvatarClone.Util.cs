using ABI_RC.Core.Player;
using MagicaCloth;
using MagicaCloth2;
using UnityEngine;

namespace NAK.AvatarCloneTest;

public partial class AvatarClone
{
    #region Utilities
    
    private static bool IsRendererActive(Renderer renderer)
        => renderer && renderer.enabled && renderer.gameObject.activeInHierarchy;
        
    private static bool CameraRendersPlayerLocalLayer(Camera cam)
        => (cam.cullingMask & (1 << LOCAL_LAYER)) != 0;

    private static bool CameraRendersPlayerCloneLayer(Camera cam)
        => (cam.cullingMask & (1 << CLONE_LAYER)) != 0;

    private static bool IsUIInternalCamera(Camera cam)
#if !UNITY_EDITOR
        => cam == PlayerSetup.Instance.activeUiCam;
#else
        => cam.gameObject.layer == 15;
#endif
    
    #endregion Utilities

    #region Magica Cloth Support
    
    private void SetupMagicaClothSupport()
    {
        var magicaCloths1 = GetComponentsInChildren<BaseCloth>(true);
        foreach (BaseCloth magicaCloth1 in magicaCloths1) 
            magicaCloth1.SetCullingMode(PhysicsTeam.TeamCullingMode.Off);
        
        var magicaCloths2 = base.GetComponentsInChildren<MagicaCloth2.MagicaCloth>(true);
        foreach (MagicaCloth2.MagicaCloth magicaCloth2 in magicaCloths2)
            magicaCloth2.serializeData.cullingSettings.cameraCullingMode = CullingSettings.CameraCullingMode.AnimatorLinkage;
    }
    
    public void OnMagicaClothMeshSwapped(Renderer render, Mesh newMesh)
    {
        switch (render)
        {
            case MeshRenderer mesh:
            {
                int index = _meshRenderers.IndexOf(mesh);
                if (index != -1) _meshCloneFilters[index].sharedMesh = newMesh;
                break;
            }
            case SkinnedMeshRenderer skinned:
            {
                int index = _skinnedRenderers.IndexOf(skinned);
                if (index != -1)
                {
                    // Copy the mesh
                    _skinnedClones[index].sharedMesh = newMesh;
                
                    // Copy appended bones if count is different
                    var cloneBones = _skinnedClones[index].bones; // alloc
                    var sourceBones = skinned.bones; // alloc
                    
                    int cloneBoneCount = cloneBones.Length;
                    int sourceBoneCount = sourceBones.Length;
                    if (cloneBoneCount != sourceBoneCount)
                    {
                        // Copy the new bones only
                        Array.Resize(ref cloneBones, sourceBoneCount);
                        for (int i = cloneBoneCount; i < sourceBoneCount; i++) cloneBones[i] = sourceBones[i];
                        _skinnedClones[index].bones = cloneBones;
                    }
                }
                break;
            }
        }
    }
    
    #endregion Magica Cloth Support
}