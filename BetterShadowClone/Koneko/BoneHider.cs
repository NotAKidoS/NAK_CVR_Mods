// using System.Collections.Generic;
// using System.Linq;
// using NAK.BetterShadowClone;
// using UnityEngine;
// using UnityEngine.Serialization;
//
// namespace Koneko.BetterHeadHider;
//
// public class BoneHider : MonoBehaviour {
//     public static ComputeShader shader;
//     
//     public SkinnedMeshRenderer[] targets;
//     public Transform shrinkBone;
//     private ComputeBuffer[] weightedBuffers;
//     private int[] weightedCounts;
//     private int[] bufferLayouts;
//     private int[] threadGroups;
//     private bool Initialized;
//
//
// #region Public Methods
//     public static void SetupAvatar(GameObject avatar, Transform shrinkBone, SkinnedMeshRenderer[] targets) {
//         BoneHider shrink = avatar.AddComponent<BoneHider>();
//         shrink.shrinkBone = shrinkBone;
//         shrink.targets = targets;
//     }
// #endregion
//
// #region Private Methods
//     private void Initialize() {
//         Dispose();
//
//         weightedBuffers = new ComputeBuffer[targets.Length];
//         weightedCounts = new int[targets.Length];
//         bufferLayouts = new int[targets.Length];
//         threadGroups = new int[targets.Length];
//
//         for (int i = 0; i < targets.Length; i++) {
//             List<int> weighted = FindHeadVertices(targets[i]);
//             if (weighted.Count == 0) continue;
//
//             targets[i].sharedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
//             weightedBuffers[i] = new(weighted.Count, sizeof(int));
//             weightedBuffers[i].SetData(weighted.ToArray());
//             weightedCounts[i] = weighted.Count;
//
//             int bufferLayout = 0;
//             if (targets[i].sharedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Position)) bufferLayout += 3;
//             if (targets[i].sharedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal)) bufferLayout += 3;
//             if (targets[i].sharedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent)) bufferLayout += 4;
//             bufferLayouts[i] = bufferLayout;
//
//             threadGroups[i] = Mathf.CeilToInt(weighted.Count / 64.0f);
//             Debug.Log(threadGroups[i]);
//         }
//
//         Initialized = true;
//     }
//
//     private List<int> FindHeadVertices(SkinnedMeshRenderer target) {
//         List<int> headVertices = new();
//         BoneWeight[] boneWeights = target.sharedMesh.boneWeights;
//         HashSet<Transform> bones = new();
//
//         bones = shrinkBone.GetComponentsInChildren<Transform>(true).ToHashSet();
//
//         //get indexs of child bones
//         HashSet<int> weights = new();
//         for (int i = 0; i < target.bones.Length; i++) {
//             if (bones.Contains(target.bones[i])) weights.Add(i);
//         }
//
//         for (int i = 0; i < boneWeights.Length; i++) {
//             BoneWeight weight = boneWeights[i];
//             if (weights.Contains(weight.boneIndex0) || weights.Contains(weight.boneIndex1) ||
//                 weights.Contains(weight.boneIndex2) || weights.Contains(weight.boneIndex3)) {
//                 headVertices.Add(i);
//             }
//         }
//         return headVertices;
//     }
//
//     private void MyOnPreRender(Camera cam) {
//         if (!Initialized) 
//             return;
//         
//         // NOTE: We only hide head once, so any camera rendered after wont see the head!
//
//         if (cam != ShadowCloneMod.PlayerCamera // only hide in player cam, or in portable cam if debug is on
//               && (!ModSettings.EntryHideInPortableCamera.Value || cam != ShadowCloneMod.HandCamera))
//             return;
//
//         if (!ShadowCloneMod.CheckWantsToHideHead(cam))
//             return; // listener said no (Third Person, etc)
//
//         for (int i = 0; i < targets.Length; i++) {
//             SkinnedMeshRenderer target = targets[i];
//             
//             if (target == null 
//                 || !target.gameObject.activeInHierarchy 
//                 || weightedBuffers[i] == null) continue;
//             
//             GraphicsBuffer vertexBuffer = targets[i].GetVertexBuffer();
//             if(vertexBuffer == null) continue;
//             
//             shader.SetVector(s_Pos, Vector3.positiveInfinity); // todo: fix
//             shader.SetInt(s_WeightedCount, weightedCounts[i]);
//             shader.SetInt(s_BufferLayout, bufferLayouts[i]);
//             shader.SetBuffer(0, s_WeightedVertices, weightedBuffers[i]);
//             shader.SetBuffer(0, s_VertexBuffer, vertexBuffer);
//             shader.Dispatch(0, threadGroups[i], 1, 1);
//             vertexBuffer.Dispose();
//         }
//     }
//
//     private void Dispose() {
//         Initialized = false;
//         if (weightedBuffers == null) return;
//         foreach (ComputeBuffer c in weightedBuffers)
//             c?.Dispose();
//     }
// #endregion
//
// #region Unity Events
//     private void OnEnable() => Camera.onPreRender += MyOnPreRender;
//     private void OnDisable() => Camera.onPreRender -= MyOnPreRender;
//     private void OnDestroy() => Dispose();
//     private void Start() => Initialize();
// #endregion
// }