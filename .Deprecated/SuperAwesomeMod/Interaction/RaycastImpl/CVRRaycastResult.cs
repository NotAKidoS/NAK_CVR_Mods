using ABI_RC.Core.InteractionSystem.Base;
using ABI_RC.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ABI_RC.Core.Player.Interaction.RaycastImpl
{
    public struct CVRRaycastResult
    {
        // Hit flags
        public bool hitWorld;           // Any non-specific collision
        public bool hitWater;           // Hit a fluid volume
        public bool hitCohtml;          // Specifically hit a COHTML view (Main/Quick Menu)
        public bool isProximityHit;     // Hit was from proximity sphere check
        public bool hitUnityUi;          // Hit a canvas
    
        // Main raycast hit info
        public RaycastHit hit;
        public RaycastHit? waterHit;        // Only valid if hitWater is true
        public Vector2 hitScreenPoint;      // Screen coordinates of the hit
    
        // Specific hit components
        public Pickupable hitPickupable;
        public Interactable hitInteractable;
        public Selectable hitSelectable;
        public ICanvasElement hitCanvasElement;
    
        // COHTML specific results
        public CohtmlControlledView hitCohtmlView;
        public Vector2 hitCohtmlCoords;
    
        // Telepathic pickup
        public bool hasTelepathicGrabCandidate;
        public Pickupable telepathicPickupable;
        public Vector3 telepathicGrabPoint;
    }
}