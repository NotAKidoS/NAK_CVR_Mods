using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using MelonLoader;
using System.Text;
using UnityEngine;

namespace NAK.Melons.Blackout;

/*

    Functionality heavily inspired by VRSleeper on Booth: https://booth.pm/ja/items/2151940

    There are three states of "blackout":

    0 - Awake (no effect)
    1 - Drowsy (partial effect)
    2 - Sleep (full effect)

    After staying still for DrowsyModeTimer (minutes), you enter DrowsyMode.
    This mode dims the screen to your selected dimming strength.
    After continuing to stay still for SleepModeTimer (seconds), you enter SleepMode.
    This mode overrenders mostly everything with black.

    Slight movement while in SleepMode will place you in DrowsyMode until SleepModeTimer is reached again.
    Hard movement once entering DrowsyMode will fully wake you and return complete vision.

*/

public class BlackoutController : MonoBehaviour
{
    public static BlackoutController Instance;
    
    // The current state of the player's consciousness.
    public BlackoutState CurrentState = BlackoutState.Awake;

    // Should the states automatically change based on time?
    public bool AutomaticStateChange = true;
    // Should the sleep state be automatically transitioned to? Some may prefer drowsy state only due to dimming.
    public bool AutoSleepState = true;

    // The minimum amount of movement required to partially restore vision.
    public float drowsyThreshold = 2f;
    // The minimum amount of movement required to fully restore vision.
    public float wakeThreshold = 4f;

    // The amount of time the player must remain still to enter drowsy state (in minutes).
    public float DrowsyModeTimer = 3f;
    // The amount of time the player must remain in drowsy state before entering sleep state (in seconds).
    public float SleepModeTimer = 10f;

    // The amount by which DrowsyMode affects the screen.
    public float DrowsyDimStrength = 0.6f;
    // Should DrowsyDimStrength be affected by velocity?
    public bool DrowsyVelocityMultiplier = true;

    // Whether to display HUD messages.
    public bool HudMessages = true;

    // Whether to lower the frame rate while in sleep mode.
    public bool DropFPSOnSleep = false;

    // The available states of consciousness.
    public enum BlackoutState
    {
        Awake = 0,
        Drowsy,
        Sleeping,
    }

    private Camera activeModeCam;
    private Vector3 headVelocity = Vector3.zero;
    private Vector3 lastHeadPos = Vector3.zero;
    private float curTime = 0f;
    private float lastAwakeTime = 0f;
    private Animator blackoutAnimator;
    private int targetFPS;

    public void ChangeBlackoutStateFromInt(int state) => ChangeBlackoutState((BlackoutState)state);

    // Changes the player's state of consciousness.
    public void ChangeBlackoutState(BlackoutState newState)
    {
        if (!blackoutAnimator) return;
        if (newState == CurrentState) return;

        lastAwakeTime = curTime;

        // Update the blackout animator based on the new state.
        switch (newState)
        {
            case BlackoutState.Awake:
                blackoutAnimator.SetBool("BlackoutState.Drowsy", false);
                blackoutAnimator.SetBool("BlackoutState.Sleeping", false);
                drowsyMagnitude = 0f;
                break;
            case BlackoutState.Drowsy:
                blackoutAnimator.SetBool("BlackoutState.Drowsy", true);
                blackoutAnimator.SetBool("BlackoutState.Sleeping", false);
                drowsyMagnitude = 0f;
                break;
            case BlackoutState.Sleeping:
                blackoutAnimator.SetBool("BlackoutState.Drowsy", false);
                blackoutAnimator.SetBool("BlackoutState.Sleeping", true);
                drowsyMagnitude = 1f;
                break;
            default:
                break;
        }

        // Update the current state and send a HUD message if enabled.
        BlackoutState prevState = CurrentState;
        CurrentState = newState;
        SendHUDMessage($"Exiting {prevState} and entering {newState} state.");
        ChangeTargetFPS();
    }

    public void AdjustDrowsyDimStrength(float multiplier = 1f)
    {
        blackoutAnimator.SetFloat("BlackoutSetting.DrowsyStrength", DrowsyDimStrength * multiplier);
    }

    // Initialize the BlackoutInstance object.
    void Start()
    {
        Instance = this;

        // Get the blackout asset and instantiate it.
        GameObject blackoutAsset = AssetsHandler.GetAsset("Assets/BundledAssets/Blackout/Blackout.prefab");
        GameObject blackoutGO = Instantiate(blackoutAsset, new Vector3(0, 0, 0), Quaternion.identity);
        blackoutGO.name = "BlackoutInstance";

        // Get the blackout animator component.
        blackoutAnimator = blackoutGO.GetComponent<Animator>();
        if (!blackoutAnimator)
        {
            MelonLogger.Error("Blackout: Could not find blackout animator component!");
            return;
        }

        SetupBlackoutInstance();

        //we dont want this to ever disable
        Camera.onPreRender += OnPreRender;
        Camera.onPostRender += OnPostRender;
    }

    //Automatic State Change
    void Update()
    {
        //get the current position of the player's head
        Vector3 curHeadPos = activeModeCam.transform.position;
        //calculate the player's head velocity by taking the difference in position
        headVelocity = (curHeadPos - lastHeadPos) / Time.deltaTime;
        //store the current head position for use in the next frame
        lastHeadPos = curHeadPos;

        if (AutomaticStateChange)
        {
            curTime = Time.time;
            //handle current state
            switch (CurrentState)
            {
                case BlackoutState.Awake:
                    HandleAwakeState();
                    break;
                case BlackoutState.Drowsy:
                    HandleDrowsyState();
                    break;
                case BlackoutState.Sleeping:
                    HandleSleepingState();
                    break;
                default:
                    break;
            }
        }
        else
        {
            CalculateDimmingMultiplier();
        }
    }

    public void OnEnable()
    {
        curTime = Time.time;
        lastAwakeTime = curTime;
    }

    public void OnDisable()
    {
        ChangeBlackoutState(BlackoutState.Awake);
    }

    void OnPreRender(Camera cam)
    {
        if (cam == activeModeCam) return;
        blackoutAnimator.transform.localScale = Vector3.zero;
    }

    void OnPostRender(Camera cam)
    {
        blackoutAnimator.transform.localScale = Vector3.one;
    }

    public void SetupBlackoutInstance()
    {
        activeModeCam = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
        blackoutAnimator.transform.parent = activeModeCam.transform;
        blackoutAnimator.transform.localPosition = Vector3.zero;
        blackoutAnimator.transform.localRotation = Quaternion.identity;
        blackoutAnimator.transform.localScale = Vector3.one;
    }

    private float GetNextStateTimer()
    {
        switch (CurrentState)
        {
            case BlackoutState.Awake:
                return (lastAwakeTime + DrowsyModeTimer * 60 - curTime);
            case BlackoutState.Drowsy:
                return (lastAwakeTime + SleepModeTimer - curTime);
            case BlackoutState.Sleeping:
                return 0f;
            default:
                return 0f;
        }
    }

    //broken, needs to run next frame
    private void SendHUDMessage(string message)
    {
        MelonLogger.Msg(message);
        if (!CohtmlHud.Instance || !HudMessages) return;

        StringBuilder secondmessage = new StringBuilder();
        if (AutomaticStateChange)
        {
            if (CurrentState == BlackoutState.Drowsy && !AutoSleepState)
            {
                secondmessage = new StringBuilder("AutoSleepState is disabled. Staying in Drowsy State.");
            }
            else
            {
                secondmessage = new StringBuilder(GetNextStateTimer().ToString() + " seconds till next state change.");
            }
        }

        CohtmlHud.Instance.ViewDropTextImmediate("Blackout", message, secondmessage.ToString());
    }

    private void ChangeTargetFPS()
    {
        if (!DropFPSOnSleep) return;

        //store target FPS to restore, i check each time just in case it changed
        targetFPS = MetaPort.Instance.settings.GetSettingInt("GraphicsFramerateTarget", 0);

        Application.targetFrameRate = (CurrentState == BlackoutState.Sleeping) ? 5 : targetFPS;
    }

    private void HandleAwakeState()
    {
        //small movement should reset sleep timer
        if (headVelocity.magnitude > drowsyThreshold)
        {
            lastAwakeTime = curTime;
        }
        //enter drowsy mode after few minutes
        if (curTime > lastAwakeTime + DrowsyModeTimer * 60)
        {
            ChangeBlackoutState(BlackoutState.Drowsy);
        }
    }

    public float fadeSpeed = 0.8f; // The speed at which the value fades back to 0 or increases
    public float minimumThreshold = 0.5f; // The minimum value that the drowsy magnitude can have
    public float drowsyMagnitude = 0f;

    private void CalculateDimmingMultiplier()
    {
        if (!DrowsyVelocityMultiplier)
        {
            AdjustDrowsyDimStrength();
            return;
        }

        float normalizedMagnitude = headVelocity.magnitude / wakeThreshold;
        float targetMagnitude = 1f - normalizedMagnitude;
        targetMagnitude = Mathf.Max(targetMagnitude, minimumThreshold);
        drowsyMagnitude = Mathf.Lerp(drowsyMagnitude, targetMagnitude, fadeSpeed * Time.deltaTime);
        AdjustDrowsyDimStrength(drowsyMagnitude);
    }

    private void HandleDrowsyState()
    {
        //hard movement should exit drowsy state
        if (headVelocity.magnitude > wakeThreshold)
        {
            ChangeBlackoutState(BlackoutState.Awake);
            return;
        }
        //small movement should reset sleep timer
        if (headVelocity.magnitude > drowsyThreshold)
        {
            lastAwakeTime = curTime;
        }
        //enter full sleep mode
        if (AutoSleepState && curTime > lastAwakeTime + SleepModeTimer)
        {
            ChangeBlackoutState(BlackoutState.Sleeping);
        }
        CalculateDimmingMultiplier();
    }

    private void HandleSleepingState()
    {
        //small movement should enter drowsy state
        if (headVelocity.magnitude > drowsyThreshold)
        {
            ChangeBlackoutState(BlackoutState.Drowsy);
        }
    }
}