using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.FaceTracking;
using ABI_RC.Systems.InputManagement;
using MelonLoader;
using UnityEngine;

namespace NAK.ControlToUnlockEyes;

public class ControlToUnlockEyesMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(FaceTrackingManager).GetMethod(nameof(FaceTrackingManager.RegisterBuiltinModules), 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance),
            postfix: new HarmonyLib.HarmonyMethod(typeof(ControlToUnlockEyesMod).GetMethod(nameof(OnPostFaceTrackingManagerRegisterBuiltinModules),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
        );
    }
    
    private static void OnPostFaceTrackingManagerRegisterBuiltinModules(FaceTrackingManager __instance) 
        => __instance.RegisterEyeModule(new DefaultEyeModule());

    public class DefaultEyeModule : IEyeTrackingModule
    {
        private const float FixedDistance = 10f;
        private bool _useFixedDistance = true;
        
        private readonly EyeTrackingData _eyeTrackingData = new();
        private bool _dataAvailable;
        private bool _running;

        private ControllerRay _activeRay;
        private Transform _rayDirectionTransform;

        private CVRHand lastInteractHand = CVRHand.Right;

        public bool Start(bool vr)
        {
            _running = true;
            return true;
        }

        public void Stop()
        {
            _running = false;
        }

        public void Update()
        {
            if (!PlayerSetup.Instance)
                return;

            UpdateLastInteractHand();
            UpdateFakedEyeTrackingData();
        }

        private void UpdateLastInteractHand()
        {
            ControllerRay leftRay = PlayerSetup.Instance.vrRayLeft;
            ControllerRay rightRay = PlayerSetup.Instance.vrRayRight;

            if (!MetaPort.Instance.isUsingVr)
            {
                _activeRay = PlayerSetup.Instance.desktopRay;
                _rayDirectionTransform = _activeRay.rayDirectionTransform;
                return;
            }

            bool leftAvailable = IsHandAvailable(leftRay, CVRHand.Left);
            bool rightAvailable = IsHandAvailable(rightRay, CVRHand.Right);

            if (CVRInputManager.Instance.interactLeftDown && leftAvailable)
                lastInteractHand = CVRHand.Left;
            else if (CVRInputManager.Instance.interactRightDown && rightAvailable)
                lastInteractHand = CVRHand.Right;

            _activeRay = GetLastInteractRay();
            _rayDirectionTransform = _activeRay.rayDirectionTransform;
        }

        private void UpdateFakedEyeTrackingData()
        {
            _dataAvailable = _activeRay.CanSelectPlayersAndProps();
            if (!_dataAvailable)
                return;

            _eyeTrackingData.blinking = false;

            Transform ourCameraTransform = PlayerSetup.Instance.activeCam.transform;
            
            Vector3 rayForward = _rayDirectionTransform.forward;
            float rayDistance = _useFixedDistance ? FixedDistance : _activeRay.Hit.distance;
            
            // TODO: dot product check to flip direction if behind camera

            // Convert to camera-local *direction* (normalized) and multiply by selected distance so the gazePoint
            // is on a sphere around the camera rather than mapped to a "square".
            Vector3 localDir = ourCameraTransform.InverseTransformDirection(rayForward).normalized;
            Vector3 localGazePoint = localDir * rayDistance;

            _eyeTrackingData.gazePoint = localGazePoint;
        }

        private ControllerRay GetLastInteractRay()
        {
            if (!MetaPort.Instance.isUsingVr)
                return PlayerSetup.Instance.desktopRay;

            ControllerRay leftRay = PlayerSetup.Instance.vrRayLeft;
            ControllerRay rightRay = PlayerSetup.Instance.vrRayRight;

            if (lastInteractHand == CVRHand.Left && IsHandAvailable(leftRay, CVRHand.Left))
                return leftRay;
            if (lastInteractHand == CVRHand.Right && IsHandAvailable(rightRay, CVRHand.Right))
                return rightRay;

            return GetBestAvailableHand();
        }

        private ControllerRay GetBestAvailableHand()
        {
            if (!MetaPort.Instance.isUsingVr)
                return PlayerSetup.Instance.desktopRay;

            ControllerRay leftRay = PlayerSetup.Instance.vrRayLeft;
            ControllerRay rightRay = PlayerSetup.Instance.vrRayRight;

            bool leftAvailable = IsHandAvailable(leftRay, CVRHand.Left);
            bool rightAvailable = IsHandAvailable(rightRay, CVRHand.Right);

            if (CVRInputManager.Instance.interactLeftDown && leftAvailable)
                return leftRay;
            if (CVRInputManager.Instance.interactRightDown && rightAvailable)
                return rightRay;

            if (lastInteractHand == CVRHand.Left && leftAvailable)
                return leftRay;
            if (lastInteractHand == CVRHand.Right && rightAvailable)
                return rightRay;

            if (rightAvailable) return rightRay;
            if (leftAvailable) return leftRay;

            return rightRay;
        }

        private static bool IsHandAvailable(ControllerRay ray, CVRHand hand)
        {
            if (ray.grabbedObject)
                return false;

            if (CVR_MenuManager.Instance.IsViewShown &&
                CVR_MenuManager.Instance.SelectedQuickMenuHand == hand)
                return false;

            return true;
        }

        public bool IsRunning() => _running;
        public bool IsDataAvailable() => _dataAvailable;
        public EyeTrackingData GetTrackingData() => _eyeTrackingData;
        public string GetModuleName() => "None";
        public string GetModuleShortName() => "None";
    }
}