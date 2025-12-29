using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.UI.UIRework.Managers;
using ABI_RC.Systems.ChatBox;
using ABI_RC.Systems.InputManagement;
using HarmonyLib;
using MelonLoader;

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace NAK.ChatBoxTweaks;

public class ChatBoxTweaksMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(KeyboardManager).GetMethod(nameof(KeyboardManager.OnKeyboardSubmit), 
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(ChatBoxTweaksMod).GetMethod(nameof(OnPreKeyboardManagerKeyboardSubmit),
                BindingFlags.NonPublic | BindingFlags.Static)),
            postfix: new HarmonyMethod(typeof(ChatBoxTweaksMod).GetMethod(nameof(OnPostKeyboardManagerKeyboardSubmit),
                    BindingFlags.NonPublic | BindingFlags.Static))
        );
    }

    private static void OnPreKeyboardManagerKeyboardSubmit(ref KeyboardManager __instance, ref KeyboardManager.OpenSource? __state)
    {
        __state = __instance.KeyboardOpenSource;
    }

    private static void OnPostKeyboardManagerKeyboardSubmit(ref KeyboardManager.OpenSource? __state)
    {
        if (__state == KeyboardManager.OpenSource.TextComms) ChatBoxAPI.OpenKeyboard();
    }
}


/*
public static class NetworkLoopInjector
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InjectNetworkFixedUpdate()
    {
        var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

        // Find the FixedUpdate phase
        int fixedUpdateIndex = Array.FindIndex(playerLoop.subSystemList, s => s.type == typeof(FixedUpdate));
        if (fixedUpdateIndex < 0)
        {
            Debug.LogError("FixedUpdate not found in player loop!");
            return;
        }

        var fixedUpdate = playerLoop.subSystemList[fixedUpdateIndex];

        // Create your custom PlayerLoopSystem
        var networkSystem = new PlayerLoopSystem
        {
            type = typeof(NetworkFixedUpdate),
            updateDelegate = NetworkFixedUpdate.Run
        };

        // Insert at the start so it runs before physics, animation, etc.
        var subs = fixedUpdate.subSystemList.ToList();
        subs.Insert(0, networkSystem);
        fixedUpdate.subSystemList = subs.ToArray();

        // Reassign and set back
        playerLoop.subSystemList[fixedUpdateIndex] = fixedUpdate;
        PlayerLoop.SetPlayerLoop(playerLoop);

        Debug.Log("[NetworkLoopInjector] Inserted NetworkFixedUpdate at start of FixedUpdate loop");
    }

    static class NetworkFixedUpdate
    {
        static int lastStateFrame = -1;
        
        public static void Run()
        {
            // Apply your networked object state syncs before physics simulation
            Debug.Log("Last State Frame: " + lastStateFrame + " Current Frame: " + Time.frameCount);
            lastStateFrame = Time.frameCount;
        }
    }
}
*/
