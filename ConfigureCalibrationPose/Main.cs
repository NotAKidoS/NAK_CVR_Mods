using System.Reflection;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.InputManagement;
using ABI_RC.Systems.Movement;
using HarmonyLib;
using MelonLoader;
using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.ConfigureCalibrationPose;

public class ConfigureCalibrationPoseMod : MelonMod
{
    #region Enums

    private enum CalibrationPose
    {
        TPose,
        APose,
        IKPose,
        BikePose,
        RacushSit,
        CCKSitting,
        CCKCrouch,
        CCKProne,
    }

    #endregion Enums
    
    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(ConfigureCalibrationPose));

    private static readonly MelonPreferences_Entry<CalibrationPose> EntryCalibrationPose =
        Category.CreateEntry("calibration_pose", CalibrationPose.APose, display_name: "Calibration Pose", 
            description: "What pose to use for FBT calibration.");
    
    #endregion Melon Preferences
    
    #region Melon Events
    
    public override void OnInitializeMelon()
    {
        #region BodySystem Patches

        HarmonyInstance.Patch(
            typeof(BodySystem).GetMethod(nameof(BodySystem.MuscleUpdate),
                BindingFlags.Public | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(ConfigureCalibrationPoseMod).GetMethod(nameof(OnPreBodySystemMuscleUpdate),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        #endregion BodySystem Patches
    }
    
    #endregion Melon Events

    #region Harmony Patches

    private static bool OnPreBodySystemMuscleUpdate(ref float[] muscles)
    {
        PlayerSetup playerSetup = PlayerSetup.Instance;
        IKSystem ikSystem = IKSystem.Instance;
        ref HumanPose humanPose = ref ikSystem._humanPose;
        
        if (BodySystem.isCalibrating)
        {
            switch (EntryCalibrationPose.Value)
            {
                default:
                case CalibrationPose.TPose:
                    for (int i = 0; i < MusclePoses.TPoseMuscles.Length; i++)
                        ikSystem.ApplyMuscleValue((MuscleIndex) i, MusclePoses.TPoseMuscles[i], ref muscles);
                    break;
                case CalibrationPose.APose:
                    for (int i = 0; i < MusclePoses.APoseMuscles.Length; i++)
                        ikSystem.ApplyMuscleValue((MuscleIndex) i, MusclePoses.APoseMuscles[i], ref muscles);
                    break;
                case CalibrationPose.IKPose:
                    for (int i = 0; i < MusclePoses.IKPoseMuscles.Length; i++)
                        ikSystem.ApplyMuscleValue((MuscleIndex) i, MusclePoses.IKPoseMuscles[i], ref muscles);
                    break;
                case CalibrationPose.BikePose:
                    for (int i = 0; i < MusclePoses.TPoseMuscles.Length; i++)
                        ikSystem.ApplyMuscleValue((MuscleIndex) i, 0f, ref muscles);
                    break;
                case CalibrationPose.CCKSitting:
                    for (int i = 0; i < CCKSittingMuscles.Length; i++)
                        ikSystem.ApplyMuscleValue((MuscleIndex) i, CCKSittingMuscles[i], ref muscles);
                    break;
                case CalibrationPose.CCKCrouch:
                    for (int i = 0; i < CCKCrouchMuscles.Length; i++)
                        ikSystem.ApplyMuscleValue((MuscleIndex) i, CCKCrouchMuscles[i], ref muscles);
                    break;
                case CalibrationPose.CCKProne:
                    for (int i = 0; i < CCKProneMuscles.Length; i++)
                        ikSystem.ApplyMuscleValue((MuscleIndex) i, CCKProneMuscles[i], ref muscles);
                    break;
                case CalibrationPose.RacushSit:
                    for (int i = 0; i < RacushSitMuscles.Length; i++)
                        ikSystem.ApplyMuscleValue((MuscleIndex) i, RacushSitMuscles[i], ref muscles);
                    break;
            }
            
            humanPose.bodyPosition = Vector3.up;
            humanPose.bodyRotation = Quaternion.identity;
        }
        else if (BodySystem.isCalibratedAsFullBody && BodySystem.TrackingPositionWeight > 0f)
        {
            BetterBetterCharacterController characterController = playerSetup.CharacterController;
            
            bool isRunning = characterController.IsMoving();
            bool isGrounded = characterController.IsGrounded();
            bool isFlying = characterController.IsFlying();
            bool isSwimming = characterController.IsSwimming();

            if ((BodySystem.PlayRunningAnimationInFullBody
                 && (isRunning || !isGrounded && !isFlying && !isSwimming)))
            {
                ikSystem.applyOriginalHipPosition = true;
                ikSystem.applyOriginalHipRotation = true;
             
                IKSolverVR solver = IKSystem.vrik.solver;
                BodySystem.SetPelvisWeight(solver.spine, 0f);
                BodySystem.SetLegWeight(solver.leftLeg, 0f);
                BodySystem.SetLegWeight(solver.rightLeg, 0f);
            }
            else
            {
                ikSystem.applyOriginalHipPosition = true;
                ikSystem.applyOriginalHipRotation = false;
                humanPose.bodyRotation = Quaternion.identity;
            }
        }

        return false; // dont run original
    }
    
    #endregion Harmony Patches

    #region Custom Pose Arrays

    private static readonly float[] CCKSittingMuscles =
    [
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        -0.8000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        -0.8000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        -1.0000f,
        0.0000f,
        -0.3000f,
        0.3000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        -1.0000f,
        0.0000f,
        -0.3000f,
        0.3000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f
    ];
    
    private static readonly float[] CCKCrouchMuscles =
    [
        -1.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.5000f,
        0.0000f,
        0.0000f,
        0.5000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        -0.6279f,
        0.0000f,
        0.0000f,
        -0.8095f,
        0.0000f,
        -1.0091f,
        0.0000f,
        0.0000f,
        -0.4126f,
        0.0013f,
        -0.0860f,
        -0.9331f,
        -0.0869f,
        -1.3586f,
        0.1791f,
        0.0000f,
        0.0000f,
        0.0000f,
        -0.1998f,
        -0.2300f,
        0.1189f,
        0.3479f,
        0.1364f,
        -0.3737f,
        0.0069f,
        0.0000f,
        0.0000f,
        -0.1994f,
        -0.2301f,
        0.0267f,
        0.7532f,
        0.1922f,
        0.0009f,
        -0.0005f,
        -1.4747f,
        -0.0443f,
        -0.3347f,
        -0.3062f,
        -0.7596f,
        -1.2067f,
        -0.7329f,
        -0.7329f,
        -0.5984f,
        -2.7162f,
        -0.7439f,
        -0.7439f,
        -0.5812f,
        1.8528f,
        -0.7520f,
        -0.7520f,
        -0.7242f,
        0.5912f,
        -0.7632f,
        -0.7632f,
        -1.4747f,
        -0.0443f,
        -0.3347f,
        -0.3062f,
        -0.7596f,
        -1.2067f,
        -0.7329f,
        -0.7329f,
        -0.5984f,
        -2.7162f,
        -0.7439f,
        -0.7439f,
        -0.5812f,
        1.8528f,
        -0.7520f,
        0.8104f,
        -0.7242f,
        0.5912f,
        -0.7632f,
        0.8105f
    ];
    
    private static readonly float[] CCKProneMuscles =
    [
        0.6604f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.7083f,
        0.0000f,
        0.0000f,
        0.7083f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.2444f,
        -0.0554f,
        -0.8192f,
        0.9301f,
        0.5034f,
        1.0274f,
        -0.1198f,
        0.5849f,
        0.2360f,
        -0.0837f,
        -1.1803f,
        0.9676f,
        0.7390f,
        0.9944f,
        -0.1717f,
        0.5849f,
        0.0000f,
        0.0000f,
        0.2823f,
        -0.6297f,
        0.3200f,
        -0.3376f,
        0.0714f,
        0.9260f,
        -1.5768f,
        0.0000f,
        0.0000f,
        0.1561f,
        -0.6712f,
        0.2997f,
        -0.3392f,
        0.0247f,
        0.7672f,
        -1.5269f,
        -1.1422f,
        0.0392f,
        0.6457f,
        0.0000f,
        0.6185f,
        -0.5393f,
        0.8104f,
        0.8104f,
        0.6223f,
        -0.8225f,
        0.8104f,
        0.8104f,
        0.6218f,
        -0.3961f,
        0.8104f,
        0.8104f,
        0.6160f,
        -0.3721f,
        0.8105f,
        0.8105f,
        -1.1422f,
        0.0392f,
        0.6457f,
        0.0000f,
        0.6185f,
        -0.5393f,
        0.8104f,
        0.8104f,
        0.6223f,
        -0.8226f,
        0.8104f,
        0.8104f,
        0.6218f,
        -0.3961f,
        0.8104f,
        0.8104f,
        0.6160f,
        -0.3721f,
        0.8105f,
        0.8105f
    ];
    
    private static readonly float[] RacushSitMuscles =
    [
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        -0.7500f,
        -0.0002f,
        0.1599f,
        -0.1500f,
        0.1000f,
        0.1300f,
        -0.0001f,
        0.0000f,
        -0.7500f,
        -0.0002f,
        0.1599f,
        -0.1500f,
        0.1000f,
        0.1300f,
        -0.0001f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.3927f,
        0.3114f,
        0.0805f,
        0.9650f,
        -0.0536f,
        0.0024f,
        0.0005f,
        0.0000f,
        0.0000f,
        0.3928f,
        0.3114f,
        0.0805f,
        0.9650f,
        -0.0536f,
        0.0024f,
        0.0005f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f,
        0.0000f
    ];
    
    #endregion Custom Pose Arrays
}