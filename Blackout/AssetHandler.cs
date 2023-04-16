using System.Reflection;
using UnityEngine;

namespace NAK.Melons.Blackout;

/*

    Kindly stolen from SDraw's leap motion mod. (MIT)
    https://github.com/SDraw/ml_mods_cvr/blob/master/ml_lme/AssetsHandler.cs

    *thank u sdraw, i wont be murderer now*

*/


static class AssetsHandler
{
    static readonly List<string> ms_assets = new List<string>()
    {
        "blackout_controller.asset"
    };

    static Dictionary<string, AssetBundle> ms_loadedAssets = new Dictionary<string, AssetBundle>();
    static Dictionary<string, GameObject> ms_loadedObjects = new Dictionary<string, GameObject>();

    public static void Load()
    {
        Assembly l_assembly = Assembly.GetExecutingAssembly();
        string l_assemblyName = l_assembly.GetName().Name;

        foreach (string l_assetName in ms_assets)
        {
            try
            {
                Stream l_assetStream = l_assembly.GetManifestResourceStream(l_assemblyName + ".resources." + l_assetName);
                if (l_assetStream != null)
                {
                    MemoryStream l_memorySteam = new MemoryStream((int)l_assetStream.Length);
                    l_assetStream.CopyTo(l_memorySteam);
                    AssetBundle l_assetBundle = AssetBundle.LoadFromMemory(l_memorySteam.ToArray(), 0);
                    if (l_assetBundle != null)
                    {
                        l_assetBundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                        ms_loadedAssets.Add(l_assetName, l_assetBundle);
                    }
                    else
                        MelonLoader.MelonLogger.Warning("Unable to load bundled '" + l_assetName + "' asset");
                }
                else
                    MelonLoader.MelonLogger.Warning("Unable to get bundled '" + l_assetName + "' asset stream");
            }
            catch (System.Exception e)
            {
                MelonLoader.MelonLogger.Warning("Unable to load bundled '" + l_assetName + "' asset, reason: " + e.Message);
            }
        }
    }

    public static GameObject GetAsset(string p_name)
    {
        GameObject l_result = null;
        if (ms_loadedObjects.ContainsKey(p_name))
        {
            l_result = UnityEngine.Object.Instantiate(ms_loadedObjects[p_name]);
            l_result.SetActive(true);
            l_result.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        }
        else
        {
            foreach (var l_pair in ms_loadedAssets)
            {
                if (l_pair.Value.Contains(p_name))
                {
                    GameObject l_bundledObject = (GameObject)l_pair.Value.LoadAsset(p_name, typeof(GameObject));
                    if (l_bundledObject != null)
                    {
                        ms_loadedObjects.Add(p_name, l_bundledObject);
                        l_result = UnityEngine.Object.Instantiate(l_bundledObject);
                        l_result.SetActive(true);
                        l_result.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                    }
                    break;
                }
            }
        }
        return l_result;
    }

    public static void Unload()
    {
        foreach (var l_pair in ms_loadedAssets)
            UnityEngine.Object.Destroy(l_pair.Value);
        ms_loadedAssets.Clear();
    }
}