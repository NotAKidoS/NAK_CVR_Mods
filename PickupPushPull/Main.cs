using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using Valve.VR;

namespace PickupPushPull;

public class PickupPushPull : MelonMod
{
    private static MelonPreferences_Category m_categoryPickupPushPull;
    private static MelonPreferences_Entry<float> m_entryPushPullSpeed;
    private static MelonPreferences_Entry<float> m_entryRotateSpeed;
    private static MelonPreferences_Entry<bool> m_entryEnableRotation;

    //not sure if im gonna implement that switch hell for gamepad or mouse yet...
    private static MelonPreferences_Entry<BindingOptionsVR> m_entryRotateBindsVR;
    private static MelonPreferences_Entry<BindHandVR> m_entryRotateBindHandVR;

    private enum BindHandVR
    {
        LeftHand,
        RightHand
    }
    private enum BindingOptionsVR
    {
        ButtonATouch,
        ButtonBTouch,
        StickTouch,
        TriggerTouch
    }

    public override void OnApplicationStart()
    {
        m_categoryPickupPushPull = MelonPreferences.CreateCategory(nameof(PickupPushPull));
        m_entryPushPullSpeed = m_categoryPickupPushPull.CreateEntry("PushPullSpeed", 1f, description: "Up/down on right joystick for VR. Left bumper + Up/down on right joystick for Gamepad.");
        m_entryRotateSpeed = m_categoryPickupPushPull.CreateEntry<float>("RotateSpeed", 1f);
        m_entryEnableRotation = m_categoryPickupPushPull.CreateEntry<bool>("EnableRotation", false, description: "Hold left trigger in VR or right bumper on Gamepad.");
        m_entryRotateBindHandVR = m_categoryPickupPushPull.CreateEntry("VR Hand", BindHandVR.LeftHand);
        m_entryRotateBindsVR = m_categoryPickupPushPull.CreateEntry("VR Binding", BindingOptionsVR.ButtonATouch);

        m_categoryPickupPushPull.SaveToFile(false);
        m_entryPushPullSpeed.OnValueChangedUntyped += UpdateSettings;
        m_entryRotateSpeed.OnValueChangedUntyped += UpdateSettings;
        m_entryEnableRotation.OnValueChangedUntyped += UpdateSettings;

        UpdateSettings();
    }
    private static void UpdateSettings()
    {
        HarmonyPatches.ppSpeed = m_entryPushPullSpeed.Value;
        HarmonyPatches.rotSpeed = m_entryRotateSpeed.Value;
        HarmonyPatches.enableRot = m_entryEnableRotation.Value;
        HarmonyPatches.rotBindVR = m_entryRotateBindsVR.Value;
        HarmonyPatches.rotHandVR = (SteamVR_Input_Sources)m_entryRotateBindHandVR.Value + 1;
    }

    [HarmonyPatch]
    private class HarmonyPatches
    {
        //UpdateSettings() on app start immediatly overrides these :shrug:
        public static float ppSpeed = m_entryPushPullSpeed.Value;
        public static float rotSpeed = m_entryRotateSpeed.Value;
        public static bool enableRot = m_entryEnableRotation.Value;
        public static BindingOptionsVR rotBindVR = m_entryRotateBindsVR.Value;
        public static SteamVR_Input_Sources rotHandVR = (SteamVR_Input_Sources)m_entryRotateBindHandVR.Value + 1;

        private static float objectPitch = 0f;
        private static float objectYaw = 0f;

        private static bool lockedVRInput = false;
        private static bool lockedFSInput = false;
        private static CursorLockMode savedCursorLockState;

        //uses code from https://github.com/ljoonal/CVR-Plugins/tree/main/RotateIt
        //GPL-3.0 license - Thank you ljoonal for being smart brain :plead:

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CVRPickupObject), "Update")]
        public static void GrabbedObjectPatch(ref CVRPickupObject __instance)
        {
            // Need to only run when the object is grabbed by the local player
            if (!__instance.IsGrabbedByMe()) return;

            Quaternion originalRotation = __instance.transform.rotation;
            Transform referenceTransform = __instance._controllerRay.transform;

            __instance.transform.RotateAround(__instance.transform.position, referenceTransform.right, objectPitch * Time.deltaTime);
            __instance.transform.RotateAround(__instance.transform.position, referenceTransform.up, objectYaw * Time.deltaTime);

            // Add the new difference between the og rotation and our newly added rotation the the stored offset.
            __instance.initialRotationalOffset *= Quaternion.Inverse(__instance.transform.rotation) * originalRotation;
        }

        //Reset object rotation input each frame
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CVRInputManager), "Update")]
        private static void BeforeUpdate()
        {
            objectPitch = 0f;
            objectYaw = 0f;
        }

        //Gamepad & Desktop Input Patch
        [HarmonyPostfix]
        [HarmonyPatch(typeof(InputModuleGamepad), "UpdateInput")]
        private static void AfterUpdateInput(ref bool ___enableGamepadInput)
        {

            bool button1 = Input.GetButton("Controller Left Button") || Input.GetKey(KeyCode.Mouse4) || Input.GetKey(KeyCode.Mouse3);
            bool button2 = Input.GetButton("Controller Right Button") || Input.GetKey(KeyCode.Mouse3);

            if (button1)
            {
                if (!lockedFSInput)
                {
                    lockedFSInput = true;
                    savedCursorLockState = Cursor.lockState;
                    Cursor.lockState = CursorLockMode.None;
                    PlayerSetup.Instance._movementSystem.disableCameraControl = true;
                }
                if (button2 && enableRot)
                {
                    objectPitch += rotSpeed * CVRInputManager.Instance.rawLookVector.y * -1;
                    objectYaw += rotSpeed * CVRInputManager.Instance.rawLookVector.x;
                }
                else
                {
                    CVRInputManager.Instance.objectPushPull += CVRInputManager.Instance.rawLookVector.y * ppSpeed * Time.deltaTime;
                }
            }
            else if (lockedFSInput)
            {
                lockedFSInput = false;
                Cursor.lockState = savedCursorLockState;
                PlayerSetup.Instance._movementSystem.disableCameraControl = false;
            }
        }

        //VR Input Patch
        [HarmonyPostfix]
        [HarmonyPatch(typeof(InputModuleSteamVR), "UpdateInput")]
        private static void AfterUpdateInputprivate(ref SteamVR_Action_Boolean ___steamVrButtonATouch, ref SteamVR_Action_Boolean ___steamVrButtonBTouch, ref SteamVR_Action_Boolean ___steamVrStickTouch, ref SteamVR_Action_Boolean ___steamVrTriggerTouch)
        {
            if (!MetaPort.Instance.isUsingVr) return;

            bool button = false;

            //not really sure this is optimal, i dont know all the cool c# tricks yet
            switch (rotBindVR)
            {
                case BindingOptionsVR.ButtonATouch:
                    button = ___steamVrButtonATouch.GetState(rotHandVR);
                    return;
                case BindingOptionsVR.ButtonBTouch:
                    button = ___steamVrButtonBTouch.GetState(rotHandVR);
                    return;
                case BindingOptionsVR.StickTouch:
                    button = ___steamVrStickTouch.GetState(rotHandVR);
                    return;
                case BindingOptionsVR.TriggerTouch:
                    button = ___steamVrTriggerTouch.GetState(rotHandVR);
                    return;
                default: break;
            }

            if (button && enableRot)
            {
                if (!lockedVRInput)
                {
                    lockedVRInput = true;
                    PlayerSetup.Instance._movementSystem.canRot = false;
                    PlayerSetup.Instance._movementSystem.disableCameraControl = true;
                }
                objectPitch += rotSpeed * (CVRInputManager.Instance.floatDirection / 2f * -1);
                objectYaw += rotSpeed * CVRInputManager.Instance.rawLookVector.x;
                return;
            }
            else if (lockedVRInput)
            {
                lockedVRInput = false;
                PlayerSetup.Instance._movementSystem.canRot = true;
                PlayerSetup.Instance._movementSystem.disableCameraControl = false;
            }

            CVRInputManager.Instance.objectPushPull += CVRInputManager.Instance.floatDirection * ppSpeed * Time.deltaTime;
        }
    }
}
