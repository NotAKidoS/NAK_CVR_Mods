using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using MelonLoader;
using NAK.PickupPushPull.InputModules;

namespace NAK.PickupPushPull;

public class PickupPushPull : MelonMod
{
    public static readonly MelonPreferences_Category Category = 
        MelonPreferences.CreateCategory(nameof(PickupPushPull));
    
    //Global settings
    public static readonly MelonPreferences_Entry<float> EntryPushPullSpeed = 
        Category.CreateEntry<float>("Push Pull Speed", 2f, description: "Up/down on right joystick for VR. Left button + Up/down on right joystick for Gamepad.");

    public static readonly MelonPreferences_Entry<float> EntryRotateSpeed = 
        Category.CreateEntry<float>("Rotate Speed", 6f);

    public static readonly MelonPreferences_Entry<bool> EntryEnableRotation = 
        Category.CreateEntry<bool>("Enable Rotation", false, description: "Hold left trigger in VR or right button on Gamepad.");

    //Desktop settings
    public static readonly MelonPreferences_Entry<bool> EntryDesktopUseZoomForRotate = 
        Category.CreateEntry<bool>("Desktop Use Zoom For Rotate", true, description: "Use zoom bind for rotation while a prop is held.");

    //VR settings
    public static readonly MelonPreferences_Entry<BindingOptionsVR.BindHand> EntryVRRotateHand = 
        Category.CreateEntry<BindingOptionsVR.BindHand>("VR Hand", BindingOptionsVR.BindHand.LeftHand);

    public static readonly MelonPreferences_Entry<BindingOptionsVR.BindingOptions> EntryVRRotateBind = 
        Category.CreateEntry<BindingOptionsVR.BindingOptions>("VR Binding", BindingOptionsVR.BindingOptions.ButtonATouch);

    public override void OnInitializeMelon()
    {
        foreach (var entry in Category.Entries)
        {
            entry.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);
        }

        EntryVRRotateBind.OnEntryValueChangedUntyped.Subscribe(OnUpdateVRBinding);

        MelonLoader.MelonCoroutines.Start(WaitForLocalPlayer());
    }


    System.Collections.IEnumerator WaitForLocalPlayer()
    {
        while (PlayerSetup.Instance == null)
            yield return null;

        CVRInputManager.Instance.gameObject.AddComponent<PickupPushPull_Module>();

        //update BlackoutController settings after it initializes
        while (PickupPushPull_Module.Instance == null)
            yield return null;

        UpdateVRBinding();
        UpdateAllSettings();
    }

    private void OnUpdateSettings(object arg1, object arg2) => UpdateAllSettings();
    private void OnUpdateVRBinding(object arg1, object arg2) => UpdateVRBinding();

    private void UpdateAllSettings()
    {
        if (!PickupPushPull_Module.Instance) return;

        //Global settings
        PickupPushPull_Module.Instance.EntryPushPullSpeed = EntryPushPullSpeed.Value * 50;
        PickupPushPull_Module.Instance.EntryRotationSpeed = EntryRotateSpeed.Value * 50;
        PickupPushPull_Module.Instance.EntryEnableRotation = EntryEnableRotation.Value;
        //Desktop settings
        PickupPushPull_Module.Instance.Desktop_UseZoomForRotate = EntryDesktopUseZoomForRotate.Value;
        //VR settings
        PickupPushPull_Module.Instance.VR_RotateHand = EntryVRRotateHand.Value;
    }

    private void UpdateVRBinding()
    {
        //VR special settings
        PickupPushPull_Module.Instance.VR_RotateBind = EntryVRRotateBind.Value;
        PickupPushPull_Module.Instance.UpdateVRBinding();
    }
}

public class BindingOptionsVR
{
    public enum BindHand
    {
        Any,
        LeftHand,
        RightHand
    }
    public enum BindingOptions
    {
        //Only oculus bindings have by default
        ButtonATouch,
        ButtonBTouch,
        TriggerTouch,
        //doesnt work?
        StickTouch,
        //Index only
        GripTouch
    }
}