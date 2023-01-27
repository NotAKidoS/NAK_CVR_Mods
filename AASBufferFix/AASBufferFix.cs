using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.Melons.AASBufferFix;

public class AASBufferFix : MonoBehaviour
{
    public PuppetMaster puppetMaster;

    public bool isAcceptingAAS = false;

    public float[] aasBufferFloat = new float[0];
    public int[] aasBufferInt = new int[0];
    public byte[] aasBufferByte = new byte[0];

    public int aasFootprint = -1;
    public int avatarFootprint = -1;

    public void Start()
    {
        puppetMaster = GetComponent<PuppetMaster>();
    }

    public void StoreExternalAASBuffer(float[] settingsFloat, int[] settingsInt, byte[] settingsByte)
    {
        //resize buffer if size changed, only should happen on first new avatar load
        if (aasBufferFloat.Length == settingsFloat.Length)
            aasBufferFloat = new float[settingsFloat.Length];

        if (aasBufferInt.Length == settingsInt.Length)
            aasBufferInt = new int[settingsInt.Length];

        if (aasBufferByte.Length == settingsByte.Length)
            aasBufferByte = new byte[settingsByte.Length];

        aasBufferFloat = settingsFloat;
        aasBufferInt = settingsInt;
        aasBufferByte = settingsByte;

        //haha shit lazy implementation
        aasFootprint = ((aasBufferFloat.Length * 32) + 1) * ((aasBufferInt.Length * 32) + 1) * ((aasBufferByte.Length * 8) + 1);

        CheckForFootprintMatch();
    }

    public void OnAvatarInstantiated(Animator animator)
    {
        avatarFootprint = GenerateAvatarFootprint(animator);
        CheckForFootprintMatch();
    }

    public void OnAvatarDestroyed()
    {
        isAcceptingAAS = false;
        //clear buffer
        aasBufferFloat = new float[0];
        aasBufferInt = new int[0];
        aasBufferByte = new byte[0];
        avatarFootprint = 0;
        aasFootprint = -1;
    }

    public void CheckForFootprintMatch()
    {
        //only apply if avatar footprints match
        if (aasFootprint == avatarFootprint)
        {
            isAcceptingAAS = true;
            puppetMaster?.ApplyAdvancedAvatarSettings(aasBufferFloat, aasBufferInt, aasBufferByte);
        }
    }

    public int GenerateAvatarFootprint(Animator animator)
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
                        avatarFloatCount += 32;
                        break;
                    case (AnimatorControllerParameterType)2:
                        break;
                    case AnimatorControllerParameterType.Int:
                        avatarIntCount += 32;
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
        avatarBoolCount = ((int)Math.Ceiling((double)avatarBoolCount / 8) * 8);

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