using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.InteractionSystem.Base;
using UnityEngine;

namespace ABI_RC.Core.Player.Interaction
{
    public class CVRPlayerHand : MonoBehaviour
    {
        #region Fields

        [SerializeField] 
        private CVRHand _hand;
        
        // Pickup rig
        [SerializeField] private Transform rayDirection;
        [SerializeField] private Transform _attachmentPoint;
        [SerializeField] private Transform _pivotPoint;
        [SerializeField] private VelocityTracker _velocityTracker;
        
        // Pickup state
        private bool _isHoldingObject;
        private Pickupable _heldPickupable;
        private Pickupable _proximityPickupable;
        
        #endregion Fields

        #region Unity Events
        

        #endregion Unity Events

        #region Private Methods

        #endregion Private Methods
        
        #region Public Methods
        
        #endregion Public Methods
    }
}