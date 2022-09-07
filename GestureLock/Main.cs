using ABI_RC.Core.UI;
using ABI_RC.Core.Savior;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using Valve.VR;

namespace GestureLock;

public class GestureLock : MelonMod
{
    private static MelonPreferences_Category m_catagoryGestureLock;
    private static MelonPreferences_Entry<bool> m_entryGestureLock;
    private static MelonPreferences_Entry<BindingOptions> m_entryGestureBind;
    private static MelonPreferences_Entry<BindHand> m_entryGestureHand;

    private enum BindHand
    {
        LeftHand,
        RightHand
    }
    private enum BindingOptions
    {
        ButtonATouch,
        ButtonBTouch,
        StickTouch,
        TriggerTouch
    }

    public override void OnApplicationStart()
    {
        m_catagoryGestureLock = MelonPreferences.CreateCategory(nameof(GestureLock));
        m_entryGestureLock = m_catagoryGestureLock.CreateEntry<bool>("Enabled", true, description: "Double-touch VR binding.");
        m_entryGestureHand = m_catagoryGestureLock.CreateEntry("VR Hand", BindHand.LeftHand);
        m_entryGestureBind = m_catagoryGestureLock.CreateEntry("VR Binding", BindingOptions.StickTouch);
        
        m_catagoryGestureLock.SaveToFile(false);
        m_entryGestureLock.OnValueChangedUntyped += UpdateSettings;
        m_entryGestureHand.OnValueChangedUntyped += UpdateSettings;
        m_entryGestureBind.OnValueChangedUntyped += UpdateSettings;

        UpdateSettings();
    }
    private static void UpdateSettings()
    { 
        HarmonyPatches.enabled = m_entryGestureLock.Value;
        HarmonyPatches.bind = m_entryGestureBind.Value;
        HarmonyPatches.hand = (SteamVR_Input_Sources)m_entryGestureHand.Value+1;
    }

    [HarmonyPatch]
    private class HarmonyPatches
    {
        public static bool enabled = m_entryGestureLock.Value;
        public static BindingOptions bind = m_entryGestureBind.Value;
        public static SteamVR_Input_Sources hand = SteamVR_Input_Sources.LeftHand;

        private static bool isLocked = false;
        private static float oldGestureLeft = 0;
        private static float oldGestureRight = 0;

        private static bool toggleLock = false;
        private static float touchDoubleTimer = 0f;
        private static bool touchArmed = false;

        private static void CheckTouch(bool input)
        {
            if (input)
            {
                if (touchArmed && touchDoubleTimer < 0.25f)
                {
                    touchArmed = false;
                    toggleLock = true;
                    touchDoubleTimer = 1f;
                }
                else
                {
                    touchDoubleTimer = 0f;
                }
                touchArmed = false;
            }
            else
            {
                touchArmed = true;
            }
        }

        //Read VR Buttons
        [HarmonyPostfix]
        [HarmonyPatch(typeof(InputModuleSteamVR), "UpdateInput")]
        private static void AfterUpdateInput(ref SteamVR_Action_Boolean ___steamVrButtonATouch, ref SteamVR_Action_Boolean ___steamVrButtonBTouch, ref SteamVR_Action_Boolean ___steamVrStickTouch, ref SteamVR_Action_Boolean ___steamVrTriggerTouch)
        {
            if (!MetaPort.Instance.isUsingVr || !enabled) return;

            touchDoubleTimer += Time.deltaTime;
            toggleLock = false;

            switch (bind)
            {
                case BindingOptions.ButtonATouch:
                    CheckTouch(___steamVrButtonATouch.GetState(hand));
                    return;
                case BindingOptions.ButtonBTouch:
                    CheckTouch(___steamVrButtonBTouch.GetState(hand));
                    return;
                case BindingOptions.StickTouch:
                    CheckTouch(___steamVrStickTouch.GetState(hand));
                    return;
                case BindingOptions.TriggerTouch:
                    CheckTouch(___steamVrTriggerTouch.GetState(hand));
                    return;
                default: break;
            }
        }

        //Apply GestureLock
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CVRInputManager), "Update")]
        private static void AfterUpdate(ref float ___gestureLeftRaw, ref float ___gestureLeft, ref float ___gestureRightRaw, ref float ___gestureRight)
        {
            if (!MetaPort.Instance.isUsingVr || !enabled) return;

            if (toggleLock)
            {
                isLocked = !isLocked;
                oldGestureLeft = ___gestureLeft;
                oldGestureRight = ___gestureRight;
                MelonLogger.Msg("Gestures " + (isLocked ? "Locked" : "Unlocked"));
                CohtmlHud.Instance.ViewDropTextImmediate("", "Gesture Lock ", "Gestures " + (isLocked ? "Locked" : "Unlocked"));
            }
            if (isLocked)
            {
                ___gestureLeftRaw = oldGestureLeft;
                ___gestureLeft = oldGestureLeft;
                ___gestureRightRaw = oldGestureRight;
                ___gestureRight = oldGestureRight;
            }
        }
    }
}
