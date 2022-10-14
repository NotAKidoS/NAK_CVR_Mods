using ABI_RC.Core.Player;
using ABI_RC.Core.UI;
using ABI_RC.Core.Savior;
using MelonLoader;
using UnityEngine;

namespace Blackout;

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

    public BlackoutState CurrentState = BlackoutState.Awake;

    //degrees of movement to give partial vision
    public float drowsyThreshold = 1f;
    //degrees of movement to give complete vision
    public float wakeThreshold = 12f;

    //how long without movement until the screen dims
    public float DrowsyModeTimer = 3f;   // MINUTES
    //how long should the wake state last before return
    public float SleepModeTimer = 10f; // SECONDS

    //how much does DrowsyMode affect the screen
    public float DrowsyDimStrength = 0.5f;

    //this is uh, not work well- might rewrite now that i know how this should work
    public bool HudMessages = false;

    //lower FPS while in sleep mode
    public bool DropFPSOnSleep = false;

    public enum BlackoutState
    {
        Awake = 0,
        Drowsy,
        Sleeping,
    }

    private Quaternion oldHeadRotation = Quaternion.identity;
    private float angularMovement = 0f;
    private float curTime = 0f;
    private float lastAwakeTime = 0f;
    private int nextUpdate = 1;
    private Animator blackoutAnimator;
    private int targetFPS;

    public void ChangeBlackoutState(BlackoutState newState)
    {
        if (!blackoutAnimator) return;
        if (newState == CurrentState) return;

        lastAwakeTime = curTime;

        switch (newState)
        {
            case BlackoutState.Awake:
                blackoutAnimator.SetBool("BlackoutState.Drowsy", false);
                blackoutAnimator.SetBool("BlackoutState.Sleeping", false);
                blackoutAnimator.SetFloat("BlackoutSetting.DrowsyPartial", DrowsyDimStrength);
                break;
            case BlackoutState.Drowsy:
                blackoutAnimator.SetBool("BlackoutState.Drowsy", true);
                blackoutAnimator.SetBool("BlackoutState.Sleeping", false);
                blackoutAnimator.SetFloat("BlackoutSetting.DrowsyPartial", DrowsyDimStrength);
                break;
            case BlackoutState.Sleeping:
                blackoutAnimator.SetBool("BlackoutState.Drowsy", false);
                blackoutAnimator.SetBool("BlackoutState.Sleeping", true);
                blackoutAnimator.SetFloat("BlackoutSetting.DrowsyPartial", DrowsyDimStrength);
                break;
            default:
                break;
        }
        BlackoutState prevState = CurrentState;
        CurrentState = newState;
        SendHUDMessage($"Exiting {prevState} and entering {newState} state.");
        ChangeTargetFPS();
    }

    void Update()
    {
        //only run once a second, angularMovement is "smoothed out" at high FPS otherwise
        //for the sake of responsivness while user is in a sleepy state, this might be removed to prevent confusion...
        curTime = Time.time;
        if (!(curTime >= nextUpdate)) return;
        nextUpdate = Mathf.FloorToInt(curTime) + 1;

        //get difference between last frame rotation and current rotation
        Quaternion currentHeadRotation = PlayerSetup.Instance.GetActiveCamera().transform.rotation;
        angularMovement = Quaternion.Angle(oldHeadRotation, currentHeadRotation);
        oldHeadRotation = currentHeadRotation;

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

        //debug
        //MelonLogger.Msg("curTime " + curTime);
        //MelonLogger.Msg("lastAwakeTime " + lastAwakeTime);
        //MelonLogger.Msg("timeleft " + GetNextStateTimer());
        //MelonLogger.Msg("current state " + CurrentState);
    }

    //initialize BlackoutInstance object
    void Start()
    {
        Instance = this;

        GameObject blackoutAsset = AssetsHandler.GetAsset("Assets/BundledAssets/Blackout/Blackout.prefab");
        GameObject blackoutGO = Instantiate(blackoutAsset, new Vector3(0, 0, 0), Quaternion.identity);

        if (blackoutGO != null)
        {
            blackoutGO.name = "BlackoutInstance";
            blackoutAnimator = blackoutGO.GetComponent<Animator>();
            SetupBlackoutInstance();
        }
    }
        
    void OnDisable()
    {
        ChangeBlackoutState(BlackoutState.Awake);
    }

    public void SetupBlackoutInstance()
    {
        blackoutAnimator.transform.parent = PlayerSetup.Instance.GetActiveCamera().transform;
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
        CohtmlHud.Instance.ViewDropTextImmediate("Blackout", message, GetNextStateTimer().ToString() + " seconds till next state change.");
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
        if (angularMovement > drowsyThreshold)
        {
            lastAwakeTime = curTime;
        }
        //enter drowsy mode after few minutes
        if (curTime > lastAwakeTime + DrowsyModeTimer * 60)
        {
            ChangeBlackoutState(BlackoutState.Drowsy);
        }
    }
    private void HandleDrowsyState()
    {
        //hard movement should exit drowsy state
        if (angularMovement > wakeThreshold)
        {
            ChangeBlackoutState(BlackoutState.Awake);
        }
        //enter full sleep mode
        if (curTime > lastAwakeTime + SleepModeTimer)
        {
            ChangeBlackoutState(BlackoutState.Sleeping);
        }
    }
    private void HandleSleepingState()
    {
        //small movement should enter drowsy state
        if (angularMovement > drowsyThreshold)
        {
            ChangeBlackoutState(BlackoutState.Drowsy);
        }
    }
}