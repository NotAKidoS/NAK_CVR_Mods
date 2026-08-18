using System.Collections;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;

namespace NAK.CleanPlates.Helpers;

// Measures an avatar's highest point (in root space) via BakeMesh + bounds and
// stores the result on a NameplateAnchor at the root. Timesliced by vertex count.
public static class NameplateAnchorUtility
{
    private const int BakeMeshVertexBudget = 1 << 19;

    public static bool Profile;
    public static bool LogContributions;

    private static Mesh _scratchMesh;

    private class BakeResult
    {
        public float MaxY = float.NegativeInfinity;
    }

    public static IEnumerator BakeRoutine(GameObject root)
    {
        if (!root) yield break;

        var result = new BakeResult();
        Stopwatch stopwatch = Profile ? Stopwatch.StartNew() : null;

        IEnumerator measure = MeasureMaxY(root, result);
        while (measure.MoveNext())
            yield return measure.Current;

        if (stopwatch != null)
        {
            stopwatch.Stop();
            CleanPlatesMod.Logger.Msg(
                $"[Anchor] Measured {root.name} in {stopwatch.Elapsed.TotalMilliseconds:F2}ms wall, maxY={result.MaxY:F4}");
        }

        if (!root || float.IsNegativeInfinity(result.MaxY) || float.IsNaN(result.MaxY))
            yield break;

        Transform rootTransform = root.transform;

        bool hasHeadBone = false;
        Transform headBone = null;
        if (root.TryGetComponent(out Animator animator) && animator.isHuman)
            hasHeadBone = headBone = animator.GetBoneTransform(HumanBodyBones.Head);

        if (!root.TryGetComponent(out NameplateAnchor anchor))
            anchor = root.AddComponent<NameplateAnchor>();

        anchor.HeadBone = headBone;
        anchor.LocalTopFromRoot = result.MaxY;

        float headY = hasHeadBone
            ? math.transform(rootTransform.worldToLocalMatrix, headBone.position).y
            : 0f;

        float localTopAboveHead = hasHeadBone
            ? result.MaxY - headY
            : 0f;

        // Clamp extra height above head to 1/1.5 of head height from root.
        if (hasHeadBone) localTopAboveHead = Mathf.Min(localTopAboveHead, Mathf.Max(0f, headY / 1.5f));

        anchor.LocalTopAboveHead = localTopAboveHead;
    }
    
    private static IEnumerator MeasureMaxY(GameObject root, BakeResult result)
    {
        Transform rootTransform = root.transform;
        float4x4 worldToRoot = rootTransform.worldToLocalMatrix;

        List<Renderer> renderers = new(64);
        root.GetComponentsInChildren(true, renderers);
        List<(Renderer r, float y)> contributions = LogContributions ? new(64) : null;

        if (!_scratchMesh)
            _scratchMesh = new Mesh { name = "NameplateBakeScratch", hideFlags = HideFlags.HideAndDontSave };

        float maxY = float.NegativeInfinity;
        int budget = 0;

        for (int i = 0; i < renderers.Count; i++)
        {
            try
            {
                Measure(renderers[i], worldToRoot, ref maxY, ref budget, contributions);
            }
            catch (Exception ex)
            {
                CleanPlatesMod.Logger.Warning($"[Anchor] Skipped {renderers[i].name}: {ex.Message}");
            }

            if (budget < BakeMeshVertexBudget)
                continue;

            budget = 0;
            yield return null;

            // Avatar was destroyed mid-bake
            if (!root)
            {
                result.MaxY = maxY;
                yield break;
            }
            
            worldToRoot = rootTransform.worldToLocalMatrix;
        }

        result.MaxY = maxY;
        if (contributions != null) DumpContributions(contributions);
    }

    private static void Measure(Renderer r, float4x4 worldToRoot, ref float maxY, ref int budget,
        List<(Renderer r, float y)> contributions)
    {
        var smr = r as SkinnedMeshRenderer;
        Mesh mesh = smr ? smr.sharedMesh : r.TryGetComponent(out MeshFilter mf) ? mf.sharedMesh : null;

        if (!mesh || mesh.vertexCount == 0)
            return;

        Bounds bounds;
        float4x4 toRoot;

        if (smr)
        {
            smr.BakeMesh(_scratchMesh, true);
            _scratchMesh.RecalculateBounds();
            bounds = _scratchMesh.bounds;
            toRoot = math.mul(worldToRoot, smr.transform.localToWorldMatrix);
            budget += mesh.vertexCount;
        }
        else
        {
            bounds = mesh.bounds;
            toRoot = math.mul(worldToRoot, r.transform.localToWorldMatrix);
        }

        float3 c = bounds.center;
        float3 e = bounds.extents;

        float y = toRoot.c0.y * c.x + toRoot.c1.y * c.y + toRoot.c2.y * c.z + toRoot.c3.y
                  + math.abs(toRoot.c0.y) * e.x + math.abs(toRoot.c1.y) * e.y + math.abs(toRoot.c2.y) * e.z;

        if (y > maxY)
            maxY = y;

        if (contributions != null && !float.IsInfinity(y) && !float.IsNaN(y))
            contributions.Add((r, y));
    }

    // Who did it
    private static void DumpContributions(List<(Renderer r, float y)> contributions)
    {
        contributions.Sort(static (a, b) => b.y.CompareTo(a.y));

        for (int i = 0; i < contributions.Count && i < 8; i++)
        {
            (Renderer r, float y) = contributions[i];

            var sb = new System.Text.StringBuilder(r.name);
            for (Transform p = r.transform.parent; p; p = p.parent)
                sb.Insert(0, p.name + "/");

            CleanPlatesMod.Logger.Msg($"[Anchor] y={y:F4} scale={r.transform.lossyScale} {sb}");
        }
    }
}