using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.InteractionSystem.Base;
using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.ShareBubbles.UI;

// The script 'NAK.ShareBubbles.UI.BubbleInteract' could not be instantiated!
// Must be added manually by ShareBubble creation...
public class BubbleInteract : Interactable
{
    public override bool IsInteractableWithinRange(Vector3 sourcePos)
    {
        return Vector3.Distance(transform.position, sourcePos) < 1.5f;
    }

    public override void OnInteractDown(ControllerRay controllerRay)
    {
        // Not used
    }

    public override void OnInteractUp(ControllerRay controllerRay)
    {
        if (PlayerSetup.Instance.GetCurrentPropSelectionMode() 
            != PlayerSetup.PropSelectionMode.None)
            return;
        
        // Check if the player is holding a pickup on the same hand
        if (controllerRay.grabbedObject != null 
            && controllerRay.grabbedObject.transform == transform)
        {
            // Use the pickup
            GetComponentInParent<ShareBubble>().EquipContent();
            controllerRay.DropObject(); // Causes null ref in ControllerRay, but doesn't break anything
            return;
        }
        
        // Open the details page
        GetComponentInParent<ShareBubble>().ViewDetailsPage();
    }

    public override void OnHoverEnter()
    {
        // Not used
    }

    public override void OnHoverExit()
    {
        // Not used
    }
}