using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using HarmonyLib;
using MagicaCloth2;
using MelonLoader;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace NAK.WhereAmIPointing;

public class WhereAmIPointingMod : MelonMod
{
    #region Melon Preferences
    
    // cannot disable because then id need extra logic to reset the alpha :)
    // private const string SettingsCategory = nameof(WhereAmIPointingMod);
    //
    // private static readonly MelonPreferences_Category Category =
    //     MelonPreferences.CreateCategory(SettingsCategory);
    //
    // private static readonly MelonPreferences_Entry<bool> Entry_Enabled =
    //     Category.CreateEntry("enabled", true, display_name: "Enabled",description: "Toggle WhereAmIPointingMod entirely.");
    
    #endregion Melon Preferences
    
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(TransformManager_Patches));
    }
    
    private void ApplyPatches(Type type)
    {
        try
        {
            HarmonyInstance.PatchAll(type);
        }
        catch (Exception e)
        {
            LoggerInstance.Msg($"Failed while patching {type.Name}!");
            LoggerInstance.Error(e);
        }
    }
    
    internal static class TransformManager_Patches
    {
        // Patch for EnableTransform(DataChunk, bool)
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TransformManager), nameof(TransformManager.EnableTransform), new[] { typeof(DataChunk), typeof(bool) })]
        private static bool OnEnableTransformChunk(TransformManager __instance, DataChunk c, bool sw, ref NativeArray<ExBitFlag8> ___flagArray)
        {
            try
            {
                // Enhanced validation
                if (!__instance.IsValid())
                    return false;
                
                if (___flagArray == null || !___flagArray.IsCreated)
                {
                    Debug.LogWarning("[MagicaCloth2] EnableTransform failed: Flag array is invalid or disposed");
                    return false;
                }

                if (!c.IsValid || c.startIndex < 0 || c.startIndex + c.dataLength > ___flagArray.Length)
                {
                    Debug.LogWarning($"[MagicaCloth2] EnableTransform failed: Invalid chunk parameters. Start: {c.startIndex}, Length: {c.dataLength}, Array Length: {___flagArray.Length}");
                    return false;
                }

                // Create and run the job with additional safety
                SafeEnableTransformJob job = new()
                {
                    chunk = c,
                    sw = sw,
                    flagList = ___flagArray,
                    maxLength = ___flagArray.Length
                };

                try
                {
                    job.Run();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MagicaCloth2] Error in EnableTransform job execution: {ex.Message}");
                    return false;
                }

                return false; // Prevent original method execution
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MagicaCloth2] Critical error in EnableTransform patch: {ex.Message}");
                return false;
            }
        }

        // Patch for EnableTransform(int, bool)
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TransformManager), nameof(TransformManager.EnableTransform), new[] { typeof(int), typeof(bool) })]
        private static bool OnEnableTransformIndex(TransformManager __instance, int index, bool sw, ref NativeArray<ExBitFlag8> ___flagArray)
        {
            try
            {
                // Enhanced validation
                if (!__instance.IsValid())
                    return false;

                if (___flagArray == null || !___flagArray.IsCreated)
                {
                    Debug.LogWarning("[MagicaCloth2] EnableTransform failed: Flag array is invalid or disposed");
                    return false;
                }

                if (index < 0 || index >= ___flagArray.Length)
                {
                    Debug.LogWarning($"[MagicaCloth2] EnableTransform failed: Index {index} out of range [0, {___flagArray.Length})");
                    return false;
                }

                // Safely modify the flag
                var flag = ___flagArray[index];
                if (flag.Value == 0)
                    return false;

                flag.SetFlag(TransformManager.Flag_Enable, sw);
                ___flagArray[index] = flag;

                return false; // Prevent original method execution
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MagicaCloth2] Critical error in EnableTransform patch: {ex.Message}");
                return false;
            }
        }

        [BurstCompile]
        private struct SafeEnableTransformJob : IJob
        {
            public DataChunk chunk;
            public bool sw;
            public NativeArray<ExBitFlag8> flagList;
            [ReadOnly] public int maxLength;

            public void Execute()
            {
                // Additional bounds checking
                if (chunk.startIndex < 0 || chunk.startIndex + chunk.dataLength > maxLength)
                    return;

                for (int i = 0; i < chunk.dataLength; i++)
                {
                    int index = chunk.startIndex + i;
                    if (index >= maxLength)
                        break;

                    ExBitFlag8 flag = flagList[index];
                    if (flag.Value == 0)
                        continue;

                    flag.SetFlag(TransformManager.Flag_Enable, sw);
                    flagList[index] = flag;
                }
            }
        }
    }
}