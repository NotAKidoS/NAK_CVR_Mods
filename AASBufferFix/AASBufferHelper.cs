using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.Melons.AASBufferFix;

public class AASBufferHelper : MonoBehaviour
{
    public bool isAcceptingAAS = false;

    internal PuppetMaster puppetMaster;

    //outside buffers that dont get nuked on avatar load
    private float[] aasBufferFloat = new float[0];
    private int[] aasBufferInt = new int[0];
    private byte[] aasBufferByte = new byte[0];

    //footprint is each parameter bit type count multiplied together
    private int aasFootprint = -1;
    private int avatarFootprint = 0;

    public void Start()
    {
        puppetMaster = GetComponent<PuppetMaster>();
    }

    public void OnAvatarInstantiated(Animator animator)
    {
        //create the loaded avatar footprint
        avatarFootprint = Utils.GenerateAnimatorAASFootprint(animator);

        //previous "bad data" now matches, apply buffered data
        if (SyncDataMatchesExpected())
        {
            ApplyExternalAASBuffer();
        }
    }

    public void OnAvatarDestroyed()
    {
        aasFootprint = -1;
        avatarFootprint = 0;
        isAcceptingAAS = false;
    }
    
    public void OnApplyAAS(float[] settingsFloat, int[] settingsInt, byte[] settingsByte)
    {
        //create the synced data footprint
        aasFootprint = (settingsFloat.Length + 1) * (settingsInt.Length + 1) * (settingsByte.Length + 1);

        if (!SyncDataMatchesExpected())
        {
            if (avatarFootprint == 0)
            {
                //we are receiving synced data, but the avatar has not loaded on our end
                //we can only assume the data is correct, and store it for later
                StoreExternalAASBuffer(settingsFloat, settingsInt, settingsByte);
                return;
            }

            //avatar is loaded on our screen, but wearer is syncing bad data
            //we will need to wait until it has loaded on their end

            //there is also a chance the avatar is hidden, so the avatar footprint returned 1 on initialization
            //(this was only one encounter during testing, someone being hidden by safety on world load) (x, 1)
            //these avatars do attempt to sync AAS, but the avatar footprint will never match

            //there is also a chance the avatar is an old avatar before AAS, so they do not sync any data
            //and have an avatar footprint of 1 (-1, 1)
            //these avatars do not seem to attempt AAS syncing, so it isnt much of a problem
        }
        else
        {
            //synced data matches what we expect
            ApplyExternalAAS(settingsFloat, settingsInt, settingsByte);
        }
    }

    public void ApplyExternalAASBuffer()
    {
        isAcceptingAAS = true;
        puppetMaster?.ApplyAdvancedAvatarSettings(aasBufferFloat, aasBufferInt, aasBufferByte);
    }

    public void ApplyExternalAAS(float[] settingsFloat, int[] settingsInt, byte[] settingsByte)
    {
        isAcceptingAAS = true;
        puppetMaster?.ApplyAdvancedAvatarSettings(settingsFloat, settingsInt, settingsByte);
    }

    public void StoreExternalAASBuffer(float[] settingsFloat, int[] settingsInt, byte[] settingsByte)
    {
        Array.Resize(ref aasBufferFloat, settingsFloat.Length);
        Array.Resize(ref aasBufferInt, settingsInt.Length);
        Array.Resize(ref aasBufferByte, settingsByte.Length);
        Array.Copy(settingsFloat, aasBufferFloat, settingsFloat.Length);
        Array.Copy(settingsInt, aasBufferInt, settingsInt.Length);
        Array.Copy(settingsByte, aasBufferByte, settingsByte.Length);
    }

    public bool SyncDataMatchesExpected()
    {
        return aasFootprint == avatarFootprint;
    }
}