using UnityEngine;

namespace ABI_RC.Core.Player.Interaction.RaycastImpl
{
    public class CVRPlayerRaycasterMouse : CVRPlayerRaycaster
    {
        private readonly Camera _camera;
        public CVRPlayerRaycasterMouse(Transform rayOrigin, Camera camera) : base(rayOrigin) { _camera = camera; }
        protected override Ray GetRayFromImpl() => Cursor.lockState == CursorLockMode.Locked 
            ? new Ray(_camera.transform.position, _camera.transform.forward) 
            : _camera.ScreenPointToRay(Input.mousePosition);
    }
}