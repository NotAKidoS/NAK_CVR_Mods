﻿using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI.CCK.Components;
using UnityEngine;

namespace NAK.RelativeSync.Components;

public class RelativeSyncMarker : MonoBehaviour
{
    public int pathHash { get; private set; }

    public bool IsComponentActive 
        => _component.isActiveAndEnabled;

    public bool ApplyRelativePosition = true;
    public bool ApplyRelativeRotation = true;
    public bool OnlyApplyRelativeHeading;

    private MonoBehaviour _component;

    private void Start()
    {
        RegisterWithManager();
        ConfigureForPotentialMovementParent();
    }

    private void OnDestroy()
    {
        RelativeSyncManager.RelativeSyncTransforms.Remove(pathHash);
    }

    public void OnHavingSoftTacosNow()
        => RegisterWithManager();

    private void RegisterWithManager()
    {
        // Remove old hash in case this is a re-registration
        RelativeSyncManager.RelativeSyncTransforms.Remove(pathHash);

        string path = GetGameObjectPath(transform);
        int hash = path.GetHashCode();

        // check if it already exists (this **should** only matter in worlds)
        if (RelativeSyncManager.RelativeSyncTransforms.ContainsKey(hash))
        {
            RelativeSyncMod.Logger.Warning($"Duplicate RelativeSyncMarker found at path {path}");
            if (!FindAvailableHash(ref hash)) // super lazy fix idfc
            {
                RelativeSyncMod.Logger.Error($"Failed to find available hash for RelativeSyncMarker after 16 tries! {path}");
                return;
            }
        }

        pathHash = hash;
        RelativeSyncManager.RelativeSyncTransforms.Add(hash, this);
    }

    private void ConfigureForPotentialMovementParent()
    {
        if (!gameObject.TryGetComponent(out CVRMovementParent movementParent))
        {
            _component = GetComponent<CVRSeat>(); // users cant animate enabled state so i dont think matters
            return;
        }
        _component = movementParent;

        // TODO: a refactor may be needed to handle the orientation mode being animated

        // respect orientation mode & gravity zone
        ApplyRelativeRotation = movementParent.orientationMode == CVRMovementParent.OrientationMode.RotateWithParent;
        OnlyApplyRelativeHeading = movementParent.GetComponent<GravityZone>() == null;
    }

    private static string GetGameObjectPath(Transform transform)
    {
        // props already have a unique instance identifier at root
        // worlds uhhhh, dont duplicate the same thing over and over thx
        // avatars on remote/local client have diff path, we need to account for it -_-

        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;

            // only true at root of local player object
            if (transform.CompareTag("Player"))
            {
                path = MetaPort.Instance.ownerId + "/" + path;
                break;
            } // remote player object root is already player guid

            path = transform.name + "/" + path;
        }

        return path;
    }

    private static bool FindAvailableHash(ref int hash)
    {
        for (int i = 0; i < 16; i++)
        {
            hash += 1;
            if (!RelativeSyncManager.RelativeSyncTransforms.ContainsKey(hash)) return true;
        }

        // failed to find a hash in 16 tries, dont care
        return false;
    }
}
