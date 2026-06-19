using System.Reflection;
using ABI_RC.Core.Extensions;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.DisableInputDuringFingerTracking;

public class DisableInputDuringFingerTrackingMod : MelonMod
{
    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(DisableInputDuringFingerTracking));
    
    private static readonly MelonPreferences_Entry<Fixes> EntryProbableFixes =
        Category.CreateEntry("probable_fixes", Fixes.Fix1,  
            "Fixes", description: "remove me from ur melonpref one day");

    private enum Fixes
    {   
        Fix1,
        Fix2,
        Fix3
    }
    
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(BodySystem).GetMethod(nameof(BodySystem.CalibrateWithSavedData),
                BindingFlags.Public | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(DisableInputDuringFingerTrackingMod).GetMethod(nameof(CalibrateWithSavedData),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch(
            typeof(BodySystem).GetMethod(nameof(BodySystem.SaveCalibrationData),
                BindingFlags.Public | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(DisableInputDuringFingerTrackingMod).GetMethod(nameof(SaveCalibrationData),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }
    
    private static void CalibrateWithSavedData(BodySystem.CalibrationData data, BodySystem __instance, ref bool __runOriginal)
    {
        __runOriginal = false;
        
        Vector3 scale = IKSystem.Instance._vrPlaySpace.localScale;
        Vector3 position = data.AvatarPosition;
        position.x *= scale.x;
        position.y *= scale.y;
        position.z *= scale.z;
        
        // IKSystem.vrik.transform.SetLocalPositionAndRotation(position, data.AvatarRotation);
        // need to set relative to IKSystem.Instance._vrPlaySpace

        switch (EntryProbableFixes.Value)
        {
            case Fixes.Fix1:
                IKSystem.vrik.transform.SetPositionAndRotation(
                    IKSystem.Instance._vrPlaySpace.TransformPointNoScale(position),
                    IKSystem.Instance._vrPlaySpace.rotation * data.AvatarRotation);
                break;
            case Fixes.Fix2:
                IKSystem.vrik.transform.SetPositionAndRotation(
                    IKSystem.Instance._vrPlaySpace.TransformPointNoScale(position),
                    data.AvatarRotation);
                break;
            // Position relative to playspace position only, ignore playspace rotation.
            case Fixes.Fix3:
                IKSystem.vrik.transform.SetPositionAndRotation(
                    IKSystem.Instance._vrPlaySpace.position + position,
                    data.AvatarRotation);
                break;
        }
            
        List<TrackingPoint> validTrackers = IKSystem.Instance.TrackingSystem.AllTrackingPoints.FindAll(m => 
            m.isActive && 
            m.isValid && 
            m.suggestedRole != TrackingPoint.TrackingRole.Invalid
        );

        foreach (BodySystem.CalibrationPoint calibrationPoint in data.CalibrationPoints)
        {
            foreach (var tracker in validTrackers)
            {
                if (tracker.identifier == calibrationPoint.TrackerSerial)
                {
                    tracker.assignedRole = calibrationPoint.TrackingRole;
                    tracker.position = calibrationPoint.LocalPosition;
                    tracker.rotation = calibrationPoint.LocalRotation;
                    tracker.referenceTransform.localPosition = calibrationPoint.LocalPosition;
                    tracker.referenceTransform.localRotation = calibrationPoint.LocalRotation;
                }
            }
        }
            
        __instance.SetupOffsets(validTrackers);
            
// #if UNITY_EDITOR
//             UnityEditor.EditorApplication.isPaused = true;
// #endif
            
        __instance.Calibrate(false);
    }
    
    private static void SaveCalibrationData(string avatarId, BodySystem __instance, ref bool __runOriginal)
    {
        __runOriginal = false;
        
        // needs to be relative to vr playspace
        Vector3 position = IKSystem.Instance._vrPlaySpace.InverseTransformPoint(IKSystem.vrik.transform.position);
        Quaternion rotation = Quaternion.Inverse(IKSystem.Instance._vrPlaySpace.rotation) * IKSystem.vrik.transform.rotation;
        
        switch (EntryProbableFixes.Value)
        {
            // Load:
            // TransformPointNoScale(position)
            // playspace.rotation * avatarRotation
            case Fixes.Fix1:
            {
                position = IKSystem.Instance._vrPlaySpace.InverseTransformPointNoScale(IKSystem.vrik.transform.position);

                Vector3 scale = IKSystem.Instance._vrPlaySpace.localScale;
                position.x /= scale.x;
                position.y /= scale.y;
                position.z /= scale.z;

                rotation = Quaternion.Inverse(IKSystem.Instance._vrPlaySpace.rotation) *
                           IKSystem.vrik.transform.rotation;
                break;
            }

            // Load:
            // TransformPointNoScale(position)
            // avatarRotation
            case Fixes.Fix2:
            {
                position = IKSystem.Instance._vrPlaySpace.InverseTransformPointNoScale(IKSystem.vrik.transform.position);

                Vector3 scale = IKSystem.Instance._vrPlaySpace.localScale;
                position.x /= scale.x;
                position.y /= scale.y;
                position.z /= scale.z;

                rotation = IKSystem.vrik.transform.rotation;
                break;
            }

            // Load:
            // playspace.position + position
            // avatarRotation
            case Fixes.Fix3:
            {
                position = IKSystem.vrik.transform.position -
                           IKSystem.Instance._vrPlaySpace.position;

                Vector3 scale = IKSystem.Instance._vrPlaySpace.localScale;
                position.x /= scale.x;
                position.y /= scale.y;
                position.z /= scale.z;

                rotation = IKSystem.vrik.transform.rotation;
                break;
            }
        }

        BodySystem.UniversalData = new BodySystem.CalibrationData(
            position,
            rotation,
            avatarId
        );

        List<TrackingPoint> validTrackers = IKSystem.Instance.TrackingSystem.AllTrackingPoints.FindAll(m => 
            m.isActive && 
            m.isValid && 
            m.suggestedRole != TrackingPoint.TrackingRole.Invalid && 
            m.assignedRole != TrackingPoint.TrackingRole.Invalid && 
            m.assignedRole != TrackingPoint.TrackingRole.Generic
        );
            
        BodySystem.UniversalData.CalibrationPoints.Clear();

        foreach (var tracker in validTrackers)
        {
            BodySystem.CalibrationPoint point = new BodySystem.CalibrationPoint(
                tracker.position,
                tracker.rotation,
                tracker.assignedRole,
                tracker.identifier
            );

            BodySystem.UniversalData.CalibrationPoints.Add(point);
        }
            
        // save to universal
        if (BodySystem.enableUniversalCalibration) BodySystem.SavedAvatars[BodySystem.UniversalCalibrationKey] = BodySystem.UniversalData;
        // save to current avatar
        if (BodySystem.enableSaveCalibration) BodySystem.SavedAvatars[avatarId] = BodySystem.UniversalData;
            
        // save to file
        if (BodySystem.enableUniversalCalibration || BodySystem.enableSaveCalibration) __instance.SaveSavedCalibrations();
    }
}