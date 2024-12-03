using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.ShareBubbles.UI;

public class FacePlayerDirectionAxis : MonoBehaviour
{
    private const float rotationSpeed = 5.0f;

    private void Update()
    {
        Transform ourTransform = transform;
    
        Vector3 ourUpDirection = ourTransform.up;
        Vector3 playerDirection = PlayerSetup.Instance.GetPlayerPosition() - ourTransform.position;
    
        Vector3 projectedDirection = Vector3.ProjectOnPlane(playerDirection, ourUpDirection).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(-projectedDirection, ourUpDirection);
        ourTransform.rotation = Quaternion.Lerp(ourTransform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }
}