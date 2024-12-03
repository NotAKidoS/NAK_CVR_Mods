using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.ShareBubbles.UI;

public class FacePlayerCamDirection : MonoBehaviour
{
    private const float lerpSpeed = 5.0f;

    private void LateUpdate()
    {
        Transform ourTransform = transform;
        Vector3 playerCamPos = PlayerSetup.Instance.activeCam.transform.position;
        Vector3 playerUp = PlayerSetup.Instance.transform.up;
        
        Vector3 direction = (playerCamPos - ourTransform.position).normalized;
        
        Quaternion targetRotation = Quaternion.LookRotation(direction, playerUp);
        ourTransform.rotation = Quaternion.Slerp(ourTransform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
    }
}