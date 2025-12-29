// using ABI_RC.Core.Base;
// using ABI_RC.Core.Player;
// using UnityEngine;
// using UnityEngine.EventSystems;
// using UnityEngine.UI;
//
// namespace NAK.SuperAwesomeMod.Components
// {
//     public class CVRCanvasWrapper : MonoBehaviour
//     {
//         public bool IsInteractable = true;
//         public float MaxInteractDistance = 10f;
//         
//         private Canvas _canvas;
//         private GraphicRaycaster _graphicsRaycaster;
//         private static readonly List<RaycastResult> _raycastResults = new();
//         private static readonly PointerEventData _pointerEventData = new(EventSystem.current);
//         
//         private static Selectable _workingSelectable;
//         private Camera _camera;
//         private RectTransform _rectTransform;
//         
//         #region Unity Events
//
//         private void Awake()
//         {
//             if (!TryGetComponent(out _canvas)
//                 || _canvas.renderMode != RenderMode.WorldSpace)
//             {
//                 IsInteractable = false;
//                 return;
//             }
//
//             _rectTransform = _canvas.GetComponent<RectTransform>();
//         }
//         
//         private void Start()
//         {
//             _graphicsRaycaster = _canvas.gameObject.AddComponent<GraphicRaycaster>();
//             _camera = PlayerSetup.Instance.activeCam;
//             _canvas.worldCamera = _camera;
//         }
//
//         #endregion Unity Events
//         
//         #region Public Methods
//         
//         public bool GetGraphicsHit(Ray worldRay, out RaycastResult result)
//         {
//             result = default;
//             
//             if (!IsInteractable || _camera == null) return false;
//
//             // Get the plane of the canvas
//             Plane canvasPlane = new(transform.forward, transform.position);
//
//             // Find where the ray intersects the canvas plane
//             if (!canvasPlane.Raycast(worldRay, out float distance))
//                 return false;
//
//             // Get the world point of intersection
//             Vector3 worldHitPoint = worldRay.origin + worldRay.direction * distance;
//
//             // Check if hit point is within max interaction distance
//             if (Vector3.Distance(worldRay.origin, worldHitPoint) > MaxInteractDistance)
//                 return false;
//
//             // Check if hit point is within canvas bounds
//             Vector3 localHitPoint = transform.InverseTransformPoint(worldHitPoint);
//             Rect canvasRect = _rectTransform.rect;
//             if (!canvasRect.Contains(new Vector2(localHitPoint.x, localHitPoint.y)))
//                 return false;
//
//             // Convert world hit point to screen space
//             Vector2 screenPoint = _camera.WorldToScreenPoint(worldHitPoint);
//             
//             // Update pointer event data
//             _pointerEventData.position = screenPoint;
//             _pointerEventData.delta = Vector2.zero;
//             
//             // Clear previous results and perform raycast
//             _raycastResults.Clear();
//             _graphicsRaycaster.Raycast(_pointerEventData, _raycastResults);
//             
//             // Early out if no hits
//             if (_raycastResults.Count == 0)
//             {
//                 //Debug.Log($"No hits on canvas {_canvas.name}");
//                 return false;
//             }
//                 
//             // Find first valid interactive UI element
//             foreach (RaycastResult hit in _raycastResults)
//             {
//                 if (!hit.isValid)
//                 {
//                     //Debug.Log($"Invalid hit on canvas {_canvas.name}");
//                     continue;
//                 }
//                     
//                 // Check if the hit object has a Selectable component and is interactable
//                 GameObject hitObject = hit.gameObject;
//                 if (!hitObject.TryGetComponent(out _workingSelectable)
//                     || !_workingSelectable.interactable)
//                 {
//                     //Debug.Log($"Non-interactable hit on canvas {_canvas.name} - {hitObject.name}");
//                     continue;
//                 }
//                 
//                 //Debug.Log($"Hit on canvas {_canvas.name} with {hitObject.name}");
//                     
//                 result = hit;
//                 return true;
//             }
//             
//             return false;
//         }
//         
//         #endregion Public Methods
//     }
// }