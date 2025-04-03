using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.BullshitWatcher;

// Slapped together to log where bullshit values are coming from,
// instead of creating the same Unity Explorer patch for the 100th time

public class BullshitWatcherMod : MelonMod
{
    #region Initialize

    public override void OnInitializeMelon()
    {
        #region Transform Patches
        
        Type transformType = typeof(Transform);
        
        // Properties
        PatchProperty(transformType, nameof(Transform.position));
        PatchProperty(transformType, nameof(Transform.localPosition));
        PatchProperty(transformType, nameof(Transform.rotation));
        PatchProperty(transformType, nameof(Transform.localRotation));
        PatchProperty(transformType, nameof(Transform.localScale));
        PatchProperty(transformType, nameof(Transform.right));
        PatchProperty(transformType, nameof(Transform.up));
        PatchProperty(transformType, nameof(Transform.forward));
        
        // Methods
        PatchMethod(transformType, nameof(Transform.SetPositionAndRotation));
        PatchMethod(transformType, nameof(Transform.SetLocalPositionAndRotation));
        PatchMethod(transformType, nameof(Transform.Translate), new[] { typeof(Vector3), typeof(Space) });
        PatchMethod(transformType, nameof(Transform.Translate), new[] { typeof(Vector3) });
        PatchMethod(transformType, nameof(Transform.Translate), new[] { typeof(Vector3), typeof(Transform) });
        PatchMethod(transformType, nameof(Transform.Rotate), new[] { typeof(Vector3), typeof(Space) });
        PatchMethod(transformType, nameof(Transform.Rotate), new[] { typeof(Vector3) });
        PatchMethod(transformType, nameof(Transform.Rotate), new[] { typeof(Vector3), typeof(float), typeof(Space) });
        PatchMethod(transformType, nameof(Transform.RotateAround));
        PatchMethod(transformType, nameof(Transform.LookAt), new[] { typeof(Vector3), typeof(Vector3) });
        
        #endregion
        
        #region Rigidbody Patches
        
        Type rigidbodyType = typeof(Rigidbody);
        
        // Properties
        PatchProperty(rigidbodyType, nameof(Rigidbody.position));
        PatchProperty(rigidbodyType, nameof(Rigidbody.rotation));
        PatchProperty(rigidbodyType, nameof(Rigidbody.velocity));
        PatchProperty(rigidbodyType, nameof(Rigidbody.angularVelocity));
        PatchProperty(rigidbodyType, nameof(Rigidbody.centerOfMass));
        
        // Methods
        PatchMethod(rigidbodyType, nameof(Rigidbody.MovePosition));
        PatchMethod(rigidbodyType, nameof(Rigidbody.MoveRotation));
        PatchMethod(rigidbodyType, nameof(Rigidbody.AddForce), new[] { typeof(Vector3), typeof(ForceMode) });
        PatchMethod(rigidbodyType, nameof(Rigidbody.AddRelativeForce), new[] { typeof(Vector3), typeof(ForceMode) });
        PatchMethod(rigidbodyType, nameof(Rigidbody.AddTorque), new[] { typeof(Vector3), typeof(ForceMode) });
        PatchMethod(rigidbodyType, nameof(Rigidbody.AddRelativeTorque), new[] { typeof(Vector3), typeof(ForceMode) });
        PatchMethod(rigidbodyType, nameof(Rigidbody.AddForceAtPosition), new[] { typeof(Vector3), typeof(Vector3), typeof(ForceMode) });
        
        #endregion
    }
    
    private void PatchProperty(Type type, string propertyName)
    {
        PropertyInfo property = type.GetProperty(propertyName);
        if (property!.SetMethod != null)
        {
            HarmonyInstance.Patch(
                property.SetMethod,
                prefix: new HarmonyMethod(typeof(BullshitWatcherMod).GetMethod(nameof(OnSetValue),
                    BindingFlags.NonPublic | BindingFlags.Static))
            );
        }
    }

    private void PatchMethod(Type type, string methodName, Type[] parameters = null)
    {
        MethodInfo method;
        if (parameters != null)
        {
            method = type.GetMethod(methodName, 
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                null,
                parameters,
                null);
        }
        else
        {
            var methods = type.GetMethods()
                .Where(m => m.Name == methodName && !m.IsGenericMethod)
                .ToArray();
                
            // If there's only one method with this name, use it
            if (methods.Length == 1)
            {
                method = methods[0];
            }
            else
            {
                // This is fine :)
                LoggerInstance.Error($"Multiple methods found for {type.Name}.{methodName}, skipping ambiguous patch");
                return;
            }
        }
            
        if (method != null)
        {
            HarmonyInstance.Patch(
                method,
                prefix: new HarmonyMethod(typeof(BullshitWatcherMod).GetMethod(nameof(OnMethodCall),
                    BindingFlags.NonPublic | BindingFlags.Static))
            );
        }
        else
        {
            MelonLogger.Warning($"Could not find method {type.Name}.{methodName}");
        }
    }

    #endregion

    #region Validation Methods

    private static bool ContainsBullshitValue(Vector3 vector)
    {
        return float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z) ||
               float.IsInfinity(vector.x) || float.IsInfinity(vector.y) || float.IsInfinity(vector.z) ||
               float.IsNegativeInfinity(vector.x) || float.IsNegativeInfinity(vector.y) || float.IsNegativeInfinity(vector.z);
    }

    private static bool ContainsBullshitValue(Quaternion quaternion)
    {
        return float.IsNaN(quaternion.x) || float.IsNaN(quaternion.y) || float.IsNaN(quaternion.z) || float.IsNaN(quaternion.w) ||
               float.IsInfinity(quaternion.x) || float.IsInfinity(quaternion.y) || float.IsInfinity(quaternion.z) || float.IsInfinity(quaternion.w) ||
               float.IsNegativeInfinity(quaternion.x) || float.IsNegativeInfinity(quaternion.y) || 
               float.IsNegativeInfinity(quaternion.z) || float.IsNegativeInfinity(quaternion.w);
    }

    private static bool ContainsBullshitValue(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) || float.IsNegativeInfinity(value);
    }

    private static bool ContainsBullshitValues(object[] values)
    {
        foreach (var value in values)
        {
            if (value == null) continue;
            
            switch (value)
            {
                case Vector3 v3:
                    if (ContainsBullshitValue(v3)) return true;
                    break;
                case Quaternion q:
                    if (ContainsBullshitValue(q)) return true;
                    break;
                case float f:
                    if (ContainsBullshitValue(f)) return true;
                    break;
            }
        }
        return false;
    }

    #endregion

    #region Logging Methods

    private static void LogBullshitValue(string componentType, string propertyName, Component component, object value)
    {
        MelonLogger.Error($"Bullshit {componentType}.{propertyName} value detected on GameObject '{GetGameObjectPath(component.gameObject)}': {value}");
        MelonLogger.Error(Environment.StackTrace);
    }

    private static void LogBullshitMethod(string componentType, string methodName, Component component, object[] parameters)
    {
        MelonLogger.Error($"Bullshit parameters in {componentType}.{methodName} call on GameObject '{GetGameObjectPath(component.gameObject)}'");
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i] != null)
                MelonLogger.Error($"  Parameter {i}: {parameters[i]}");
        }
        MelonLogger.Error(Environment.StackTrace);
    }

    private static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = $"{parent.name}/{path}";
            parent = parent.parent;
        }
        
        return path;
    }

    #endregion

    #region Harmony Patches

    private static void OnSetValue(object value, Component __instance)
    {
        if (value == null) return;

        var componentType = __instance.GetType().Name;
        var propertyName = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name.Replace("set_", "");

        switch (value)
        {
            case Vector3 v3 when ContainsBullshitValue(v3):
                LogBullshitValue(componentType, propertyName, __instance, v3);
                break;
            case Quaternion q when ContainsBullshitValue(q):
                LogBullshitValue(componentType, propertyName, __instance, q);
                break;
            case float f when ContainsBullshitValue(f):
                LogBullshitValue(componentType, propertyName, __instance, f);
                break;
        }
    }

    private static void OnMethodCall(object[] __args, Component __instance)
    {
        if (__args == null || __args.Length == 0) return;

        if (ContainsBullshitValues(__args))
        {
            var componentType = __instance.GetType().Name;
            var methodName = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name;
            LogBullshitMethod(componentType, methodName, __instance, __args);
        }
    }

    #endregion
}