using ABI_RC.Core;
using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.AvatarCloneTest;

public partial class AvatarClone
{
    private static bool CameraRendersPlayerLocalLayer(Camera cam)
        => (cam.cullingMask & (1 << CVRLayers.PlayerLocal)) != 0;

    private static bool CameraRendersPlayerCloneLayer(Camera cam)
        => (cam.cullingMask & (1 << CVRLayers.PlayerClone)) != 0;
    
    private static bool IsUIInternalCamera(Camera cam)
        => cam == PlayerSetup.Instance.activeUiCam;
    
    private static bool IsRendererValid(Renderer renderer)
        => renderer && renderer.gameObject.activeInHierarchy;
}