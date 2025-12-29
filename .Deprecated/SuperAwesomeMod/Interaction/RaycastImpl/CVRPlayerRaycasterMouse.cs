using UnityEngine;

namespace ABI_RC.Core.Player.Interaction.RaycastImpl
{
    public class CVRPlayerRaycasterMouse : CVRPlayerRaycaster
    {
        #region Constructor
        
        public CVRPlayerRaycasterMouse(Transform rayOrigin, Camera camera) : base(rayOrigin) { _camera = camera; }
        
        private readonly Camera _camera;
        
        #endregion Constructor

        #region Overrides

        protected override Ray GetRayFromImpl() => Cursor.lockState == CursorLockMode.Locked 
            ? new Ray(_camera.transform.position, _camera.transform.forward) 
            : _camera.ScreenPointToRay(Input.mousePosition);
        
        protected override Ray GetProximityRayFromImpl() => Cursor.lockState == CursorLockMode.Locked 
            ? new Ray(_camera.transform.position, _camera.transform.forward) 
            : _camera.ScreenPointToRay(Input.mousePosition);

        #endregion Overrides
    }
}