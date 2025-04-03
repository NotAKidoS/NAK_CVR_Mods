using ABI_RC.Core.Savior;
using UnityEngine;

namespace NAK.LuaNetVars;

public partial class LuaNetVarController
{
    private bool TryGetUniqueNetworkID(out int hash)
    {
        string path = GetGameObjectPath(transform);
        hash = path.GetHashCode();

        // Check if it already exists (this **should** only matter in worlds)
        if (_hashes.Contains(hash))
        {
            LuaNetVarsMod.Logger.Warning($"Duplicate RelativeSyncMarker found at path {path}");
            if (!FindAvailableHash(ref hash)) // Super lazy fix idfc
            {
                LuaNetVarsMod.Logger.Error($"Failed to find available hash for RelativeSyncMarker after 16 tries! {path}");
                return false;
            }
        }

        _hashes.Add(hash);
        
        return true;
        
        static bool FindAvailableHash(ref int hash)
        {
            for (int i = 0; i < 16; i++)
            {
                hash += 1;
                if (!_hashes.Contains(hash)) return true;
            }
            return false; // Failed to find a hash in 16 tries
        }
    }

    private static string GetGameObjectPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;

            // Only true at root of local player object
            if (transform.CompareTag("Player"))
            {
                path = MetaPort.Instance.ownerId + "/" + path;
                break;
            }

            path = transform.name + "/" + path;
        }
        return path;
    }
}