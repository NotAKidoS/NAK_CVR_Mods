using ABI_RC.Core.Savior;
using UnityEngine;

namespace NAK.ChatBoxExtensions.InputModules;

internal class InputModuleChatBoxExtensions : CVRInputModule
{
    public bool jump = false;
    public float emote = -1;
    public Vector2 lookVector = Vector2.zero;
    public Vector3 movementVector = Vector3.zero;

    public override void UpdateInput()
    {
        CVRInputManager.Instance.jump |= jump;
        CVRInputManager.Instance.movementVector += movementVector;
        CVRInputManager.Instance.lookVector += lookVector;
        if (emote > 0) CVRInputManager.Instance.emote = emote;

        jump = false;
        emote = -1;
        lookVector = Vector3.zero;
        movementVector = Vector3.zero;
    }
}
