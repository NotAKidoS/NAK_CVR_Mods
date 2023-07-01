using MelonLoader;
using UnityEngine;

namespace NAK.AASBufferFix;

public class Utils
{
    public static int GenerateAnimatorAASFootprint(Animator animator)
    {
        int avatarFloatCount = 0;
        int avatarIntCount = 0;
        int avatarBoolCount = 0;
        int bitCount = 0;

        foreach (AnimatorControllerParameter animatorControllerParameter in animator.parameters)
        {
            if (animatorControllerParameter.name.Length > 0 && animatorControllerParameter.name[0] != '#' && !coreParameters.Contains(animatorControllerParameter.name) && bitCount <= 3200)
            {
                AnimatorControllerParameterType type = animatorControllerParameter.type;
                switch (type)
                {
                    case AnimatorControllerParameterType.Float:
                        avatarFloatCount++;
                        bitCount += 32;
                        break;
                    case AnimatorControllerParameterType.Int:
                        avatarIntCount++;
                        bitCount += 32;
                        break;
                    case AnimatorControllerParameterType.Bool:
                        avatarBoolCount++;
                        bitCount++;
                        break;
                    default:
                        //we dont count triggers
                        break;
                }
            }
        }

        //bool to byte
        avatarBoolCount = ((int)Math.Ceiling((double)avatarBoolCount / 8));

        //create the footprint

        return (avatarFloatCount + 1) * (avatarIntCount + 1) * (avatarBoolCount + 1);
    }

    private static readonly HashSet<string> coreParameters = new HashSet<string>
    {
        "MovementX",
        "MovementY",
        "Grounded",
        "Emote",
        "GestureLeft",
        "GestureRight",
        "Toggle",
        "Sitting",
        "Crouching",
        "CancelEmote",
        "Prone",
        "Flying"
    };
}
