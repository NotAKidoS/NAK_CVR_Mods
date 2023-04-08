using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using HarmonyLib;
using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.Melons.KneeFix.HarmonyPatches;

internal static class BodySystemPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BodySystem), "SetupOffsets")]
    private static void Postfix_BodySystem_SetupOffsets(List<TrackingPoint> trackingPoints)
    {
        //redo offsets for knees as native is too far from pivot
        foreach (TrackingPoint trackingPoint in trackingPoints)
        {
            Transform parent = null;
            if (trackingPoint.assignedRole == TrackingPoint.TrackingRole.LeftKnee)
            {
                parent = IKSystem.vrik.references.leftCalf;
            }
            else if (trackingPoint.assignedRole == TrackingPoint.TrackingRole.RightKnee)
            {
                parent = IKSystem.vrik.references.rightCalf;
            }

            if (parent != null)
            {
                trackingPoint.offsetTransform.parent = parent;
                trackingPoint.offsetTransform.localPosition = Vector3.zero;
                trackingPoint.offsetTransform.localRotation = Quaternion.identity;
                trackingPoint.offsetTransform.parent = trackingPoint.referenceTransform;

                Vector3 b = IKSystem.vrik.references.root.forward * 0.5f;
                trackingPoint.offsetTransform.position += b;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BodySystem), "Update")]
    private static void Postfix_BodySystem_Update()
    {
        // FBT needs avatar root to follow head
        if (IKSystem.vrik != null)
            IKSystem.vrik.solver.spine.maxRootAngle = BodySystem.isCalibratedAsFullBody ? 0f : 180f;
    }
}

internal static class VRIKPatches
{
    /**
        Leg solver uses virtual bone calf and foot, plus world tracked knee position for normal maths.
        This breaks as you playspace up, because calf and foot position aren't offset yet in solve order.
    **/

    [HarmonyPrefix]
    [HarmonyPatch(typeof(IKSolverVR.Leg), "ApplyOffsets")]
    private static bool Prefix_IKSolverVR_Leg_ApplyOffsets(ref IKSolverVR.Leg __instance)
    {
        //This is the second part of the above fix, preventing the solver from calculating a bad bendNormal
        //when it doesn't need to. The knee tracker should dictate the bendNormal completely.

        if (__instance.usingKneeTracker)
        {
            __instance.ApplyPositionOffset(__instance.footPositionOffset, 1f);
            __instance.ApplyRotationOffset(__instance.footRotationOffset, 1f);
            Quaternion quaternion = Quaternion.FromToRotation(__instance.footPosition - __instance.position, __instance.footPosition + __instance.heelPositionOffset - __instance.position);
            __instance.footPosition = __instance.position + quaternion * (__instance.footPosition - __instance.position);
            __instance.footRotation = quaternion * __instance.footRotation;
            return false;
        }

        // run full method like normal otherwise
        float num = __instance.bendGoalWeight;
        __instance.bendGoalWeight = 0f;
        __instance.ApplyOffsetsOld();
        __instance.bendGoalWeight = num;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(IKSolverVR.Leg), "Solve")]
    private static void Prefix_IKSolverVR_Leg_Solve(ref IKSolverVR.Leg __instance)
    {
        //Turns out VRIK applies bend goal maths before root is offset in solving process.
        //We will apply ourselves before then to fix it.
        if (__instance.usingKneeTracker)
            __instance.ApplyBendGoal();
    }
}