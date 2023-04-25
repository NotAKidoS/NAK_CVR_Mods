using MelonLoader;
using System;
using System.Reflection;
using UnityEngine;
using static ThirdPerson.CameraLogic;
using BuildInfo = ThirdPerson.BuildInfo;

[assembly: AssemblyCopyright("Created by " + BuildInfo.Author)]
[assembly: MelonInfo(typeof(ThirdPerson.ThirdPerson), BuildInfo.Name, BuildInfo.Version, BuildInfo.Author)]
[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonColor(ConsoleColor.DarkMagenta)]

namespace ThirdPerson;

public static class BuildInfo
{
    public const string Name = "ThirdPerson";
    public const string Author = "Davi & NotAKidoS";
    public const string Version = "1.0.1";
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
        if (State)
        {
            if (Input.GetAxis("Mouse ScrollWheel") > 0f) IncrementDist();
            else if (Input.GetAxis("Mouse ScrollWheel") < 0f) DecrementDist();
        }

        if (!Input.GetKey(KeyCode.LeftControl)) return;
        if (Input.GetKeyDown(KeyCode.T)) State = !State;
        if (!State || !Input.GetKeyDown(KeyCode.Y)) return;
        RelocateCam((CameraLocation)(((int)CurrentLocation + 1) % Enum.GetValues(typeof(CameraLocation)).Length), true);
    }
}