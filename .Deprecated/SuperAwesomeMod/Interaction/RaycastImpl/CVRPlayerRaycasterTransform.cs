using UnityEngine;

namespace ABI_RC.Core.Player.Interaction.RaycastImpl
{
    public class CVRPlayerRaycasterTransform : CVRPlayerRaycaster
    {
        public CVRPlayerRaycasterTransform(Transform rayOrigin) : base(rayOrigin) { }
        protected override Ray GetRayFromImpl() => new(_rayOrigin.position, _rayOrigin.forward);
    }
}