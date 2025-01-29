using UnityEngine;

namespace NAK.AvatarCloneTest;

public partial class AvatarClone
{
    #region Public API Methods
    
    /// <summary>
    /// Sets whether the specific renderer requires additional runtime checks when copying to the clone.
    /// For example, Magica Cloth modifies the sharedMesh & bones of the renderer at runtime. This is not needed
    /// for most renderers, so copying for all renderers would be inefficient.
    /// </summary>
    public void SetRendererNeedsAdditionalChecks(Renderer rend, bool needsChecks)
    {
        switch (rend)
        {
            case MeshRenderer meshRenderer:
            {
                int index = _standardRenderers.IndexOf(meshRenderer);
                if (index == -1) return;

                if (needsChecks && !_standardRenderersNeedingChecks.Contains(index))
                {
                    int insertIndex = _standardRenderersNeedingChecks.Count;
                    _standardRenderersNeedingChecks.Add(index);
                    _cachedSharedMeshes.Insert(insertIndex, null);
                }
                else if (!needsChecks)
                {
                    int removeIndex = _standardRenderersNeedingChecks.IndexOf(index);
                    if (removeIndex != -1)
                    {
                        _standardRenderersNeedingChecks.RemoveAt(removeIndex);
                        _cachedSharedMeshes.RemoveAt(removeIndex);
                    }
                }
                return;
            }
            case SkinnedMeshRenderer skinnedRenderer:
            {
                int index = _skinnedRenderers.IndexOf(skinnedRenderer);
                if (index == -1) return;

                if (needsChecks && !_skinnedRenderersNeedingChecks.Contains(index))
                {
                    int insertIndex = _skinnedRenderersNeedingChecks.Count;
                    _skinnedRenderersNeedingChecks.Add(index);
                    _cachedSharedMeshes.Insert(_standardRenderersNeedingChecks.Count + insertIndex, null);
                    _cachedSkinnedBoneCounts.Add(0);
                }
                else if (!needsChecks)
                {
                    int removeIndex = _skinnedRenderersNeedingChecks.IndexOf(index);
                    if (removeIndex != -1)
                    {
                        _skinnedRenderersNeedingChecks.RemoveAt(removeIndex);
                        _cachedSharedMeshes.RemoveAt(_standardRenderersNeedingChecks.Count + removeIndex);
                        _cachedSkinnedBoneCounts.RemoveAt(removeIndex);
                    }
                }
                break;
            }
        }
    }
    
    #endregion Public API Methods
}