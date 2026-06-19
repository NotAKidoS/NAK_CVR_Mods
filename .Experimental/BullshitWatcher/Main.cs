using System.Reflection;
using System.Runtime.CompilerServices;
using ABI_RC.Core.Player;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NAK.BullshitWatcher;

public class BullshitWatcherMod : MelonMod
{
    private const float MaxAllowedValueTop = 3.402823E+7f;
    private const float MaxAllowedValueBottom = -3.402823E+7f;
    private const float MinAvatarHeight = 0.005f;

    public override void OnInitializeMelon()
    {
        PatchSetterVector3(typeof(Transform), nameof(Transform.position));
        PatchSetterVector3(typeof(Transform), nameof(Transform.localPosition));
        PatchSetterVector3(typeof(Transform), nameof(Transform.localScale));
        PatchSetterVector3(typeof(Transform), nameof(Transform.right));
        PatchSetterVector3(typeof(Transform), nameof(Transform.up));
        PatchSetterVector3(typeof(Transform), nameof(Transform.forward));
        PatchSetterQuaternion(typeof(Transform), nameof(Transform.rotation));
        PatchSetterQuaternion(typeof(Transform), nameof(Transform.localRotation));

        PatchMethod(typeof(Transform), nameof(Transform.SetPositionAndRotation),
            nameof(OnTransformSetPositionAndRotation),
            typeof(Vector3), typeof(Quaternion));

        PatchMethod(typeof(Transform), nameof(Transform.SetLocalPositionAndRotation),
            nameof(OnTransformSetLocalPositionAndRotation),
            typeof(Vector3), typeof(Quaternion));

        PatchMethod(typeof(Transform), nameof(Transform.Translate),
            nameof(OnTransformTranslateVector3Space),
            typeof(Vector3), typeof(Space));

        PatchMethod(typeof(Transform), nameof(Transform.Translate),
            nameof(OnTransformTranslateVector3),
            typeof(Vector3));

        PatchMethod(typeof(Transform), nameof(Transform.Translate),
            nameof(OnTransformTranslateVector3Transform),
            typeof(Vector3), typeof(Transform));

        PatchMethod(typeof(Transform), nameof(Transform.Rotate),
            nameof(OnTransformRotateVector3Space),
            typeof(Vector3), typeof(Space));

        PatchMethod(typeof(Transform), nameof(Transform.Rotate),
            nameof(OnTransformRotateVector3),
            typeof(Vector3));

        PatchMethod(typeof(Transform), nameof(Transform.Rotate),
            nameof(OnTransformRotateAxisAngleSpace),
            typeof(Vector3), typeof(float), typeof(Space));

        PatchMethod(typeof(Transform), nameof(Transform.RotateAround),
            nameof(OnTransformRotateAround),
            typeof(Vector3), typeof(Vector3), typeof(float));

        PatchMethod(typeof(Transform), nameof(Transform.LookAt),
            nameof(OnTransformLookAt),
            typeof(Vector3), typeof(Vector3));

        PatchSetterVector3(typeof(Rigidbody), nameof(Rigidbody.position));
        PatchSetterVector3(typeof(Rigidbody), nameof(Rigidbody.velocity));
        PatchSetterVector3(typeof(Rigidbody), nameof(Rigidbody.angularVelocity));
        PatchSetterVector3(typeof(Rigidbody), nameof(Rigidbody.centerOfMass));
        PatchSetterQuaternion(typeof(Rigidbody), nameof(Rigidbody.rotation));

        PatchMethod(typeof(Rigidbody), nameof(Rigidbody.MovePosition),
            nameof(OnRigidbodyMovePosition),
            typeof(Vector3));

        PatchMethod(typeof(Rigidbody), nameof(Rigidbody.MoveRotation),
            nameof(OnRigidbodyMoveRotation),
            typeof(Quaternion));

        PatchMethod(typeof(Rigidbody), nameof(Rigidbody.AddForce),
            nameof(OnRigidbodyAddForce),
            typeof(Vector3), typeof(ForceMode));

        PatchMethod(typeof(Rigidbody), nameof(Rigidbody.AddRelativeForce),
            nameof(OnRigidbodyAddRelativeForce),
            typeof(Vector3), typeof(ForceMode));

        PatchMethod(typeof(Rigidbody), nameof(Rigidbody.AddTorque),
            nameof(OnRigidbodyAddTorque),
            typeof(Vector3), typeof(ForceMode));

        PatchMethod(typeof(Rigidbody), nameof(Rigidbody.AddRelativeTorque),
            nameof(OnRigidbodyAddRelativeTorque),
            typeof(Vector3), typeof(ForceMode));

        PatchMethod(typeof(Rigidbody), nameof(Rigidbody.AddForceAtPosition),
            nameof(OnRigidbodyAddForceAtPosition),
            typeof(Vector3), typeof(Vector3), typeof(ForceMode));

        PatchMethod(typeof(Object), nameof(Object.Instantiate),
            nameof(OnInstantiatePositionRotation),
            typeof(Object), typeof(Vector3), typeof(Quaternion));

        PatchMethod(typeof(Object), nameof(Object.Instantiate),
            nameof(OnInstantiatePositionRotationParent),
            typeof(Object), typeof(Vector3), typeof(Quaternion), typeof(Transform));

        PatchSetterAvatarHeight(typeof(PlayerSetup), nameof(PlayerSetup.AvatarHeight));
    }

    private void PatchSetterVector3(Type type, string propertyName)
        => HarmonyInstance.Patch(
            AccessTools.Property(type, propertyName).SetMethod,
            new HarmonyMethod(GetPrefix(nameof(OnSetVector3))));

    private void PatchSetterQuaternion(Type type, string propertyName)
        => HarmonyInstance.Patch(
            AccessTools.Property(type, propertyName).SetMethod,
            new HarmonyMethod(GetPrefix(nameof(OnSetQuaternion))));

    private void PatchSetterAvatarHeight(Type type, string propertyName)
        => HarmonyInstance.Patch(
            AccessTools.Property(type, propertyName).SetMethod,
            new HarmonyMethod(GetPrefix(nameof(OnSetAvatarHeight))));

    private void PatchMethod(Type type, string methodName, string prefixName, params Type[] parameterTypes)
        => HarmonyInstance.Patch(
            AccessTools.Method(type, methodName, parameterTypes),
            new HarmonyMethod(GetPrefix(prefixName)));

    private static MethodInfo GetPrefix(string name)
        => typeof(BullshitWatcherMod).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe bool IsBullshit(float value)
        => (((*(int*)&value) & int.MaxValue) >= 0x7F800000)
        || value <= MaxAllowedValueBottom
        || value >= MaxAllowedValueTop;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsBullshit(Vector3 value)
        => IsBullshit(value.x) || IsBullshit(value.y) || IsBullshit(value.z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsBullshit(Quaternion value)
        => IsBullshit(value.x) || IsBullshit(value.y) || IsBullshit(value.z) || IsBullshit(value.w);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsBullshit(Vector3 a, Vector3 b)
        => IsBullshit(a.x) || IsBullshit(a.y) || IsBullshit(a.z)
        || IsBullshit(b.x) || IsBullshit(b.y) || IsBullshit(b.z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsBullshit(Vector3 vector, Quaternion quaternion)
        => IsBullshit(vector.x) || IsBullshit(vector.y) || IsBullshit(vector.z)
        || IsBullshit(quaternion.x) || IsBullshit(quaternion.y) || IsBullshit(quaternion.z) || IsBullshit(quaternion.w);

    // --- Sanitizers --------------------------------------------------------
    // NaN -> 0 (NaN can't be clamped meaningfully).
    // +/-Infinity and out-of-range finite values -> clamped to the allowed bound.
    // A bullshit Quaternion -> identity, because zeroing it produces a degenerate
    // (0,0,0,0) rotation that just turns back into NaN the moment Unity normalizes it.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Sanitize(float value)
    {
        if (float.IsNaN(value))
            return 0f;
        if (value >= MaxAllowedValueTop)    // also catches +Infinity
            return MaxAllowedValueTop;
        if (value <= MaxAllowedValueBottom) // also catches -Infinity
            return MaxAllowedValueBottom;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 Sanitize(Vector3 value)
        => new Vector3(Sanitize(value.x), Sanitize(value.y), Sanitize(value.z));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Quaternion Sanitize(Quaternion value)
        => IsBullshit(value) ? Quaternion.identity : value;
    // -----------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LogBullshitValue(Component instance, MethodBase method, object value)
    {
        string member = method.Name.Replace("set_", "");
        MelonLogger.Error($"Bullshit {instance.GetType().Name}.{member} = {value} on '{GetPath(instance.transform)}' (sanitized)");
        MelonLogger.Error(Environment.StackTrace);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LogBullshitArgs1(Component instance, MethodBase method, object arg0)
    {
        MelonLogger.Error($"Bullshit args in {instance.GetType().Name}.{method.Name}() on '{GetPath(instance.transform)}' (sanitized)");
        MelonLogger.Error($"  arg0: {arg0}");
        MelonLogger.Error(Environment.StackTrace);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LogBullshitArgs2(Component instance, MethodBase method, object arg0, object arg1)
    {
        MelonLogger.Error($"Bullshit args in {instance.GetType().Name}.{method.Name}() on '{GetPath(instance.transform)}' (sanitized)");
        MelonLogger.Error($"  arg0: {arg0}");
        MelonLogger.Error($"  arg1: {arg1}");
        MelonLogger.Error(Environment.StackTrace);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LogBullshitArgs3(Component instance, MethodBase method, object arg0, object arg1, object arg2)
    {
        MelonLogger.Error($"Bullshit args in {instance.GetType().Name}.{method.Name}() on '{GetPath(instance.transform)}' (sanitized)");
        MelonLogger.Error($"  arg0: {arg0}");
        MelonLogger.Error($"  arg1: {arg1}");
        MelonLogger.Error($"  arg2: {arg2}");
        MelonLogger.Error(Environment.StackTrace);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LogBullshitInstantiate(MethodBase method, Object original, Vector3 position, Quaternion rotation, Transform parent)
    {
        string originalName = original != null ? original.name : "null";
        string parentPath = parent != null ? GetPath(parent) : "<none>";

        MelonLogger.Error($"Bullshit args in Object.{method.Name}() original='{originalName}' parent='{parentPath}' (sanitized)");
        MelonLogger.Error($"  position: {position}");
        MelonLogger.Error($"  rotation: {rotation}");
        MelonLogger.Error(Environment.StackTrace);
    }

    private static void OnSetVector3(ref Vector3 value, Component __instance, MethodBase __originalMethod)
    {
        if (!IsBullshit(value))
            return;

        LogBullshitValue(__instance, __originalMethod, value);
        value = Sanitize(value);
    }

    private static void OnSetQuaternion(ref Quaternion value, Component __instance, MethodBase __originalMethod)
    {
        if (!IsBullshit(value))
            return;

        LogBullshitValue(__instance, __originalMethod, value);
        value = Sanitize(value);
    }

    private static void OnSetAvatarHeight(ref float value, Component __instance, MethodBase __originalMethod)
    {
        float clamped;
        if (float.IsNaN(value) || value < MinAvatarHeight) // also catches 0, negatives, -Infinity
            clamped = MinAvatarHeight;
        else if (value > MaxAllowedValueTop)               // also catches +Infinity
            clamped = MaxAllowedValueTop;
        else
            return; // height is fine, leave it alone

        LogBullshitValue(__instance, __originalMethod, value);
        value = clamped;
    }

    private static void OnTransformSetPositionAndRotation(ref Vector3 position, ref Quaternion rotation, Component __instance, MethodBase __originalMethod)
    {
        if (!IsBullshit(position, rotation))
            return;

        LogBullshitArgs2(__instance, __originalMethod, position, rotation);
        position = Sanitize(position);
        rotation = Sanitize(rotation);
    }

    private static void OnTransformSetLocalPositionAndRotation(ref Vector3 localPosition, ref Quaternion localRotation, Component __instance, MethodBase __originalMethod)
    {
        if (!IsBullshit(localPosition, localRotation))
            return;

        LogBullshitArgs2(__instance, __originalMethod, localPosition, localRotation);
        localPosition = Sanitize(localPosition);
        localRotation = Sanitize(localRotation);
    }

    private static void OnTransformTranslateVector3Space(ref Vector3 translation, Space relativeTo, Component __instance, MethodBase __originalMethod)
    {
        if (!IsBullshit(translation))
            return;

        LogBullshitArgs2(__instance, __originalMethod, translation, relativeTo);
        translation = Sanitize(translation);
    }

    private static void OnTransformTranslateVector3(ref Vector3 translation, Component __instance, MethodBase __originalMethod)
    {
        if (!IsBullshit(translation))
            return;

        LogBullshitArgs1(__instance, __originalMethod, translation);
        translation = Sanitize(translation);
    }

    private static void OnTransformTranslateVector3Transform(ref Vector3 translation, Transform relativeTo, Component __instance, MethodBase __originalMethod)
    {
        if (!IsBullshit(translation))
            return;

        LogBullshitArgs2(__instance, __originalMethod, translation, relativeTo);
        translation = Sanitize(translation);
    }

    private static void OnTransformRotateVector3Space(ref Vector3 eulers, Space relativeTo, Component __instance, MethodBase __originalMethod)
    {
        if (!IsBullshit(eulers))
            return;

        LogBullshitArgs2(__instance, __originalMethod, eulers, relativeTo);
        eulers = Sanitize(eulers);
    }

    private static void OnTransformRotateVector3(ref Vector3 eulers, Component __instance, MethodBase __originalMethod)
    {
        if (!IsBullshit(eulers))
            return;

        LogBullshitArgs1(__instance, __originalMethod, eulers);
        eulers = Sanitize(eulers);
    }

    private static void OnTransformRotateAxisAngleSpace(ref Vector3 axis, ref float angle, Space relativeTo, Component __instance, MethodBase __originalMethod)
    {
        if (!(IsBullshit(axis) || IsBullshit(angle)))
            return;

        LogBullshitArgs3(__instance, __originalMethod, axis, angle, relativeTo);
        axis = Sanitize(axis);
        angle = Sanitize(angle);
    }

    private static void OnTransformRotateAround(ref Vector3 point, ref Vector3 axis, ref float angle, Component __instance, MethodBase __originalMethod)
    {
        if (!(IsBullshit(point, axis) || IsBullshit(angle)))
            return;

        LogBullshitArgs3(__instance, __originalMethod, point, axis, angle);
        point = Sanitize(point);
        axis = Sanitize(axis);
        angle = Sanitize(angle);
    }

    private static void OnTransformLookAt(ref Vector3 worldPosition, ref Vector3 worldUp, Component __instance, MethodBase __originalMethod)
    {
        if (!IsBullshit(worldPosition, worldUp))
            return;

        LogBullshitArgs2(__instance, __originalMethod, worldPosition, worldUp);
        worldPosition = Sanitize(worldPosition);
        // worldUp must stay a usable direction; a zeroed up vector makes LookAt produce garbage.
        worldUp = Sanitize(worldUp);
        if (worldUp == Vector3.zero)
            worldUp = Vector3.up;
    }

    private static void OnRigidbodyMovePosition(ref Vector3 position, Component __instance, MethodBase __originalMethod)
    {
        if (!IsBullshit(position))
            return;

        LogBullshitArgs1(__instance, __originalMethod, position);
        position = Sanitize(position);
    }

    private static void OnRigidbodyMoveRotation(ref Quaternion rot, Component __instance, MethodBase __originalMethod)
    {
        if (!IsBullshit(rot))
            return;

        LogBullshitArgs1(__instance, __originalMethod, rot);
        rot = Sanitize(rot);
    }

    private static void OnRigidbodyAddForce(ref Vector3 force, ForceMode mode, Component __instance, MethodBase __originalMethod)
    {
        if (!IsBullshit(force))
            return;

        LogBullshitArgs2(__instance, __originalMethod, force, mode);
        force = Sanitize(force);
    }

    private static void OnRigidbodyAddRelativeForce(ref Vector3 force, ForceMode mode, Component __instance, MethodBase __originalMethod)
    {
        if (!IsBullshit(force))
            return;

        LogBullshitArgs2(__instance, __originalMethod, force, mode);
        force = Sanitize(force);
    }

    private static void OnRigidbodyAddTorque(ref Vector3 torque, ForceMode mode, Component __instance, MethodBase __originalMethod)
    {
        if (!IsBullshit(torque))
            return;

        LogBullshitArgs2(__instance, __originalMethod, torque, mode);
        torque = Sanitize(torque);
    }

    private static void OnRigidbodyAddRelativeTorque(ref Vector3 torque, ForceMode mode, Component __instance, MethodBase __originalMethod)
    {
        if (!IsBullshit(torque))
            return;

        LogBullshitArgs2(__instance, __originalMethod, torque, mode);
        torque = Sanitize(torque);
    }

    private static void OnRigidbodyAddForceAtPosition(ref Vector3 force, ref Vector3 position, ForceMode mode, Component __instance, MethodBase __originalMethod)
    {
        if (!IsBullshit(force, position))
            return;

        LogBullshitArgs3(__instance, __originalMethod, force, position, mode);
        force = Sanitize(force);
        position = Sanitize(position);
    }

    private static void OnInstantiatePositionRotation(Object original, ref Vector3 position, ref Quaternion rotation, MethodBase __originalMethod)
    {
        if (!IsBullshit(position, rotation))
            return;

        LogBullshitInstantiate(__originalMethod, original, position, rotation, null);
        position = Sanitize(position);
        rotation = Sanitize(rotation);
    }

    private static void OnInstantiatePositionRotationParent(Object original, ref Vector3 position, ref Quaternion rotation, Transform parent, MethodBase __originalMethod)
    {
        if (!IsBullshit(position, rotation))
            return;

        LogBullshitInstantiate(__originalMethod, original, position, rotation, parent);
        position = Sanitize(position);
        rotation = Sanitize(rotation);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GetPath(Transform current)
    {
        string path = current.name;
        for (Transform parent = current.parent; parent != null; parent = parent.parent)
            path = parent.name + "/" + path;
        return path;
    }
}