using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.Melons.AASBufferFix
{
    public class AASBufferHelper : MonoBehaviour
    {
        //public stuff
        public bool GameHandlesAAS { get; private set; }

        //internal references
        private PuppetMaster _puppetMaster;

        //outside aas buffers
        private float[] _aasBufferFloat = new float[0];
        private int[] _aasBufferInt = new int[0];
        private byte[] _aasBufferByte = new byte[0];

        //calculated footprints
        private int _aasFootprint = -1;
        private int _avatarFootprint = 0;

        private void Start() => _puppetMaster = GetComponent<PuppetMaster>();

        public void OnAvatarInstantiated(Animator animator)
        {

            ///MelonLoader.MelonLogger.Msg("[OnInit] Remote avatar initialized. Generating avatar animator footprint.");

            _avatarFootprint = Utils.GenerateAnimatorAASFootprint(animator);

            // avatar does not contain proper AAS
            // this is likely because the avatar was made before AAS, or is blocked/hidden
            if (_avatarFootprint == 1)
            {
                ///MelonLoader.MelonLogger.Msg("[OnInit] Avatar does not contain valid AAS.");
                // we will let the game handle this by setting IsAcceptingAAS to true
                GameHandlesAAS = true;
                return;
            }

            ///MelonLoader.MelonLogger.Msg($"[OnInit] Avatar footprint is : {_avatarFootprint}");

            //check if we received expected AAS while we loaded the avatar, and if so, apply it now
            if (SyncDataMatchesExpected())
            {
                ///MelonLoader.MelonLogger.Msg("[OnInit] Valid buffered AAS found. Applying AAS immediatly.");
                ApplyExternalAASBuffer();
                return;
            }

            //we loaded avatar faster than wearer
            ///MelonLoader.MelonLogger.Msg("[OnInit] Remote avatar initialized faster than wearer. Waiting on valid AAS before applying.");
        }

        public void OnAvatarDestroyed()
        {
            GameHandlesAAS = false;
            _aasFootprint = -1;
            _avatarFootprint = 0;
        }

        public void OnApplyAAS(float[] settingsFloat, int[] settingsInt, byte[] settingsByte)
        {
            //avatar is still loading on our side, we must assume AAS data is correct and store it until we load
            //there is also a chance it errored
            if (_avatarFootprint == 0)
            {
                ///MelonLoader.MelonLogger.Msg("[OnSync] Avatar is still loading on our end.");
                // Calculate AAS footprint to compare against later.
                _aasFootprint = (settingsFloat.Length + 1) * (settingsInt.Length + 1) * (settingsByte.Length + 1);
                StoreExternalAASBuffer(settingsFloat, settingsInt, settingsByte);
                return;
            }

            //avatar is loaded on our end, and is not blocked by filter
            //this does run if it is manually hidden or distance hidden

            ///MelonLoader.MelonLogger.Msg("[OnSync] Avatar is loaded on our side and is not blocked. Comparing for expected values.");
            ///MelonLoader.MelonLogger.Msg($"[OnSync] Avatar Footprint is : {_avatarFootprint}");

            // Calculate AAS footprint to compare against.
            _aasFootprint = (settingsFloat.Length + 1) * (settingsInt.Length + 1) * (settingsByte.Length + 1);

            //if it matches, apply the settings and let game take over
            if (SyncDataMatchesExpected())
            {
                ///MelonLoader.MelonLogger.Msg("[OnSync] Avatar values matched and have been applied.");
                ApplyExternalAAS(settingsFloat, settingsInt, settingsByte);
                return;
            }

            //if it did not match, that means the avatar we see on our side is different than what the remote user is wearing and syncing
            ///MelonLoader.MelonLogger.Msg("[OnSync] Avatar loaded is different than wearer. The wearer is likely still loading the avatar!");
            StoreExternalAASBuffer(settingsFloat, settingsInt, settingsByte);
        }

        private void ApplyExternalAASBuffer()
        {
            GameHandlesAAS = true;
            _puppetMaster?.ApplyAdvancedAvatarSettings(_aasBufferFloat, _aasBufferInt, _aasBufferByte);
        }

        private void ApplyExternalAAS(float[] settingsFloat, int[] settingsInt, byte[] settingsByte)
        {
            GameHandlesAAS = true;
            _puppetMaster?.ApplyAdvancedAvatarSettings(settingsFloat, settingsInt, settingsByte);
        }

        private void StoreExternalAASBuffer(float[] settingsFloat, int[] settingsInt, byte[] settingsByte)
        {
            Array.Resize(ref _aasBufferFloat, settingsFloat.Length);
            Array.Resize(ref _aasBufferInt, settingsInt.Length);
            Array.Resize(ref _aasBufferByte, settingsByte.Length);
            Array.Copy(settingsFloat, _aasBufferFloat, settingsFloat.Length);
            Array.Copy(settingsInt, _aasBufferInt, settingsInt.Length);
            Array.Copy(settingsByte, _aasBufferByte, settingsByte.Length);
        }

        private bool SyncDataMatchesExpected() => _aasFootprint == _avatarFootprint;
    }
}