using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NAK.Melons.AASBufferFix;

public class Utils
{
    public static int GenerateAnimatorAASFootprint(Animator animator)
    {
        int avatarFloatCount = 0;
        int avatarIntCount = 0;
        int avatarBoolCount = 0;

        foreach (AnimatorControllerParameter animatorControllerParameter in animator.parameters)
        {
            if (animatorControllerParameter.name.Length > 0 && animatorControllerParameter.name[0] != '#' && !coreParameters.Contains(animatorControllerParameter.name))
            {
                AnimatorControllerParameterType type = animatorControllerParameter.type;
                switch (type)
                {
                    case AnimatorControllerParameterType.Float:
                        avatarFloatCount++;
                        break;
                    case (AnimatorControllerParameterType)2:
                        break;
                    case AnimatorControllerParameterType.Int:
                        avatarIntCount++;
                        break;
                    case AnimatorControllerParameterType.Bool:
                        avatarBoolCount++;
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

    private static HashSet<string> coreParameters = new HashSet<string>
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
