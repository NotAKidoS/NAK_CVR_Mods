using MelonLoader;
using System.Reflection;
using UnityEngine;
using static NAK.ThirdPerson.CameraLogic;

namespace NAK.ThirdPerson;

public class ThirdPerson : MelonMod
{
    internal static MelonLogger.Instance Logger;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        Patches.Apply(HarmonyInstance);
        MelonCoroutines.Start(SetupCamera());
    }

    public override void OnUpdate()
    {
        // Prevents scrolling while in Menus or UnityExplorer
        if (State && Cursor.lockState == CursorLockMode.Locked)
        {
            if (Input.GetAxis("Mouse ScrollWheel") > 0f) IncrementDist();
            else if (Input.GetAxis("Mouse ScrollWheel") < 0f) DecrementDist();
        }

        if (!Input.GetKey(KeyCode.LeftControl)) return;
        if (Input.GetKeyDown(KeyCode.T)) State = !State;
        if (!State || !Input.GetKeyDown(KeyCode.Y)) return;
        RelocateCam((CameraLocation)(((int)CurrentLocation + (Input.GetKey(KeyCode.LeftShift) ? -1 : 1) + Enum.GetValues(typeof(CameraLocation)).Length) % Enum.GetValues(typeof(CameraLocation)).Length), true);
    }
}