using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using HarmonyLib;
using MelonLoader;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Events;

namespace Blackout;

/*

    Functionality heavily inspired by VRSleeper on Booth: https://booth.pm/ja/items/2151940

    There are three states of "blackout":

    0 - Awake (no effect)
    1 - Drowsy (partial effect)
    2 - Sleep (full effect)

*/

public class BlackoutController : MonoBehaviour
{
    public int BlackoutState = 0;

    //degrees of movement to give partial vision
    public float drowsyThreshold = 1f;
    //degrees of movement to give complete vision
    public float wakeThreshold = 12f;

    //how long without movement until the screen dims
    public float enterSleepTime = 3f;   // MINUTES
    //how long should the wake state last before return
    public float returnSleepTime = 10f; // SECONDS

    //private BlackoutController instance;
    private Quaternion oldHeadRotation = Quaternion.identity;
    private float lastAwakeTime = 0f;
    private int nextUpdate = 1;

    void Update()
    {
        //only run once a second, angularMovement is "smoothed out" at high FPS otherwise
        float curTime = Time.time;
        if (!(curTime >= nextUpdate)) return;
        nextUpdate = Mathf.FloorToInt(curTime) + 1;

        //get difference between last frame rotation and current rotation
        Quaternion currentHeadRotation = PlayerSetup.Instance.GetActiveCamera().transform.rotation;
        float angularMovement = Quaternion.Angle(oldHeadRotation, currentHeadRotation);
        oldHeadRotation = currentHeadRotation;

        // These are SOFT movements
        if (angularMovement > drowsyThreshold)
        {
            lastAwakeTime = curTime;
            if (BlackoutState == 2)
            {
                BlackoutState = 1;
                MelonLogger.Msg("Exited Sleep state and entered Drowsy state.");
            }
        }

        // These are HARD movements
        if (angularMovement > wakeThreshold)
        {
            lastAwakeTime = curTime;

            if (BlackoutState == 0) return;
            BlackoutState = 0;
            MelonLogger.Msg("Exited anystate and entered Awake state.");
        }

        MelonLogger.Msg($"BlackoutState " + BlackoutState);
        MelonLogger.Msg($"curTime " + curTime);
        MelonLogger.Msg($"lastAwakeTime " + lastAwakeTime);

        if (BlackoutState == 2) return;

        //check if we should enter/return to sleep mode
        if (curTime > lastAwakeTime + (BlackoutState == 0 ? enterSleepTime * 60 : returnSleepTime))
        {
            BlackoutState = 2;
            MelonLogger.Msg("Entered sleep state.");
        }
    }

    void Start()
    {
        MelonLogger.Msg(Application.streamingAssetsPath);

        var myLoadedAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "blackoutfader"));
        if (myLoadedAssetBundle == null)
        {
            Debug.Log("Failed to load AssetBundle!");
            return;
        }

        var prefab = myLoadedAssetBundle.LoadAsset<GameObject>("MyObject");
        Instantiate(prefab);

        myLoadedAssetBundle.Unload(false);

        prefab.transform.parent = PlayerSetup.Instance.GetActiveCamera().transform;
        prefab.transform.localPosition = Vector3.zero;
        prefab.transform.localRotation = Quaternion.identity;
        prefab.transform.localScale = Vector3.zero;
    }
}