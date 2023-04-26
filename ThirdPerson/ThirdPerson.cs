using MelonLoader;
using System.Reflection;
using UnityEngine;
using static NAK.ThirdPerson.CameraLogic;
using BuildInfo = NAK.ThirdPerson.BuildInfo;

[assembly: AssemblyCopyright("Created by " + BuildInfo.Author)]
[assembly: MelonInfo(typeof(NAK.ThirdPerson.ThirdPerson), BuildInfo.Name, BuildInfo.Version, BuildInfo.Author)]
[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonColor(ConsoleColor.DarkMagenta)]

namespace NAK.ThirdPerson;

public static class BuildInfo
{
    public const string Name = "ThirdPerson";
    public const string Author = "Davi & NotAKidoS";
    public const string Version = "1.0.2";
}

public class ThirdPerson : MelonMod
{
    internal static MelonLogger.Instance Logger;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        MelonCoroutines.Start(SetupCamera());

        Patches.Apply(HarmonyInstance);
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