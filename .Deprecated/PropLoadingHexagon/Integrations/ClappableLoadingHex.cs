// using NAK.PropLoadingHexagon.Components;
// using UnityEngine;
//
// namespace NAK.PropLoadingHexagon.Integrations;
//
// // ReSharper disable once ClassNeverInstantiated.Global
// public class TheClapperIntegration
// {
//     public static void Init()
//     {
//         PropLoadingHexagonMod.OnPropPlaceholderCreated += (placeholder) =>
//         {
//             if (placeholder.TryGetComponent(out LoadingHexagonController loadingHexagon))
//                 ClappableLoadingHex.Create(loadingHexagon);
//         };
//     }
// }
//
// public class ClappableLoadingHex : Kafe.TheClapper.Clappable
// {
//     private LoadingHexagonController _loadingHexagon;
//     
//     public override void OnClapped(Vector3 clappablePosition) 
//     {
//         if (_loadingHexagon == null) return;
//         _loadingHexagon.IsLoadingCanceled = true;
//         Kafe.TheClapper.TheClapper.EmitParticles(clappablePosition, new Color(1f, 1f, 0f), 2f); // why this internal
//     }
//     
//     public static void Create(LoadingHexagonController loadingHexagon)
//     {
//         GameObject target = loadingHexagon.transform.GetChild(0).gameObject;
//         if (!target.gameObject.TryGetComponent(out ClappableLoadingHex clappableHexagon))
//             clappableHexagon = target.AddComponent<ClappableLoadingHex>();
//         clappableHexagon._loadingHexagon = loadingHexagon;
//     }
// }