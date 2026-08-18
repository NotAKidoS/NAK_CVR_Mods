using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using UnityEngine;

namespace NAK.CleanPlates;

// Desktop fov literally shrinks everything on screen, so the plates have to
// grow back. VR fov wraps more of your vision, nothing changes size.
internal static class FovScale
{
    private const float ReferenceHalfTan = 0.57735026f; // tan(30)
    public static float Current => MetaPort.Instance.isUsingVr
        ? 1f
        : Mathf.Tan(PlayerSetup.Instance.activeCam.fieldOfView * 0.5f * Mathf.Deg2Rad) / ReferenceHalfTan;
}