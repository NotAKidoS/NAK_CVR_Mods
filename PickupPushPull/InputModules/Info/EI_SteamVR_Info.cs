using ABI_RC.Core.Savior;
using System.Reflection;

namespace NAK.PickupPushPull.InputModules.Info;

// Stolen from my scrapped Enhanced Input mod
internal class EI_SteamVR_Info
{
    //General Inputs
    internal static readonly FieldInfo im_vrMovementAction = typeof(InputModuleSteamVR).GetField("vrMovementAction", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static readonly FieldInfo im_vrJumpAction = typeof(InputModuleSteamVR).GetField("vrJumpAction", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static readonly FieldInfo im_vrLookAction = typeof(InputModuleSteamVR).GetField("vrLookAction", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static readonly FieldInfo im_vrMuteAction = typeof(InputModuleSteamVR).GetField("vrMuteAction", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static readonly FieldInfo im_vrMenuButton = typeof(InputModuleSteamVR).GetField("vrMenuButton", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static readonly FieldInfo im_vrTriggerValue = typeof(InputModuleSteamVR).GetField("vrTriggerValue", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static readonly FieldInfo im_vrGripValue = typeof(InputModuleSteamVR).GetField("vrGripValue", BindingFlags.NonPublic | BindingFlags.Instance);
    //Vive Controllers
    internal static readonly FieldInfo im_vrTouchPadValue = typeof(InputModuleSteamVR).GetField("vrTouchPadValue", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static readonly FieldInfo im_vrTouchPadClick = typeof(InputModuleSteamVR).GetField("vrTouchPadClick", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static readonly FieldInfo im_vrTouchPadTouch = typeof(InputModuleSteamVR).GetField("vrTouchPadTouch", BindingFlags.NonPublic | BindingFlags.Instance);
    //Knuckles Controllers
    internal static readonly FieldInfo im_steamVrIndexSkeletonLeft = typeof(InputModuleSteamVR).GetField("steamVrIndexSkeletonLeft", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static readonly FieldInfo im_steamVrIndexSkeletonRight = typeof(InputModuleSteamVR).GetField("steamVrIndexSkeletonRight", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static readonly FieldInfo im_steamVrIndexGestureToggle = typeof(InputModuleSteamVR).GetField("steamVrIndexGestureToggle", BindingFlags.NonPublic | BindingFlags.Instance);
    //Touch Controllers
    internal static readonly FieldInfo im_steamVrTriggerTouch = typeof(InputModuleSteamVR).GetField("steamVrTriggerTouch", BindingFlags.Public | BindingFlags.Instance);
    internal static readonly FieldInfo im_steamVrGripTouch = typeof(InputModuleSteamVR).GetField("steamVrGripTouch", BindingFlags.Public | BindingFlags.Instance);
    internal static readonly FieldInfo im_steamVrStickTouch = typeof(InputModuleSteamVR).GetField("steamVrStickTouch", BindingFlags.Public | BindingFlags.Instance);
    internal static readonly FieldInfo im_steamVrButtonATouch = typeof(InputModuleSteamVR).GetField("steamVrButtonATouch", BindingFlags.Public | BindingFlags.Instance);
    internal static readonly FieldInfo im_steamVrButtonBTouch = typeof(InputModuleSteamVR).GetField("steamVrButtonBTouch", BindingFlags.Public | BindingFlags.Instance);
    //SteamVR Specific
    //internal static readonly FieldInfo im_steamVrVibration = typeof(InputModuleSteamVR).GetField("steamVrVibration", BindingFlags.NonPublic | BindingFlags.Instance);
}
