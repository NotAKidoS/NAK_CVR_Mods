using System.ComponentModel;
using ABI_RC.Core.Networking;
using ABI_RC.Systems.Communications;
using DarkRift.Client;
using FuckMLA;
using HarmonyLib;
using UnityEngine;
using Unity.Services.Vivox;
using ABI_RC.Core.Player;
using ABI_RC.Core;

namespace NAK.FuckVivox.HarmonyPatches;

internal class VivoxServiceInternalPatches
{
    // This is to catch some dumb issue where channel might not exist. There is no return even though the error is logged... -_-
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(VivoxServiceInternal), nameof(VivoxServiceInternal.Set3DPosition), 
        typeof(Vector3), typeof(Vector3), typeof(Vector3),
        typeof(Vector3), typeof(string), typeof(bool))]
    private static void Prefix_VivoxServiceInternal_Set3DPosition(
        Vector3 speakerPos, Vector3 listenerPos, Vector3 listenerAtOrient, Vector3 listenerUpOrient,
        string channelName, bool allowPanning,
        ref ILoginSession ___m_LoginSession,
        ref bool __runOriginal)
    {
        __runOriginal = true;

        try
        {
            IChannelSession channelSession = ___m_LoginSession.ChannelSessions.FirstOrDefault(channel =>
                channel.Channel.Type == ChannelType.Positional && channel.Channel.Name == channelName);

            if (channelSession != null) 
                return; // no~ fuck you
            
            __runOriginal = false; 
            FuckVivox.Logger.Msg("Caught an unhandled VivoxServiceInternal error.");
        }
        catch (Exception e)
        {
            FuckVivox.Logger.Error(e.ToString());
            __runOriginal = false;
        }
    }

    // This is to prevent a race condition between OnLoggedOut and OnConnectionFailedToRecover

    [HarmonyPrefix]
    [HarmonyPatch(typeof(VivoxServiceManager), nameof(VivoxServiceManager.OnConnectionFailedToRecover))]
    private static void Prefix_VivoxServiceInternal_OnConnectionFailedToRecover(ref bool __runOriginal)
    {
        __runOriginal = false;
        FuckVivox.Logger.Msg("(OnConnectionFailedToRecover) Possibly prevented a double re-login attempt!");
    }
    
    // This is to log us out until our connection stabilizes
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.ReconnectToGameServer))]
    private static void Prefix_NetworkManager_ReconnectToGameServer()
    {
        //FuckVivox.Logger.Msg("CONNECTION UNSTABLE, PANIC LOGOUT!!!");
        //VivoxHelpers.AttemptLogout();
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.OnGameNetworkConnected))]
    private static void Prefix_NetworkManager_OnGameNetworkConnected()
    {
        if (VivoxServiceManager.Instance.IsLoggedIn())
            return;
        
        //FuckVivox.Logger.Msg("(OnGameNetworkConnected) Not logged into Vivox. Connection is potentially stable now, so attempting to login.");
        //VivoxHelpers.AttemptLogin();
    }
    
    // This is to potentially fix an issue where on quick restart, we are in a channel before the bind attempts to add it???
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(VivoxServiceInternal), nameof(VivoxServiceInternal.OnChannelPropertyChanged))]
    private static void Prefix_VivoxServiceInternal_OnChannelPropertyChanged(
        object sender, PropertyChangedEventArgs args,
        ref VivoxServiceInternal __instance,
        ref bool __runOriginal)
    {
        __runOriginal = true;
        
        IChannelSession channelSession = (IChannelSession)sender;

        if (args.PropertyName != "ChannelState" || channelSession.ChannelState != ConnectionState.Connected) 
            return;

        if (!__instance.m_ActiveChannels.ContainsKey(channelSession.Channel.Name)) 
            return;
        
        FuckVivox.Logger.Warning($"Active Channel already contains key! :: + {channelSession.Channel.Name}");
        __runOriginal = false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.OnApplicationFocus))]
    private static void Prefix_InputManager_OnApplicationFocus(bool hasFocus)
    {
        FuckVivox.Logger.Msg("OnApplicationFocus: " + hasFocus); 
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(RootLogic), nameof(RootLogic.CursorLock))]
    private static void Prefix_RootLogic_CursorLock(bool value)
    {
        FuckVivox.Logger.Msg("CursorLock:" + value);
    }

    private static bool _isFocused = false;
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.LateUpdate))]
    private static void Prefix_InputManager_LateUpdate()
    {
        
        
        if (Application.isFocused == _isFocused)
            return;

        _isFocused = Application.isFocused;
        FuckVivox.Logger.Msg("Application.isFocused Updated!: " + _isFocused); 
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.Start))]
    private static void Prefix_InputManager_Start()
    {
        Application.focusChanged += Test;
    }

    private static void Test(bool value)
    {
        FuckVivox.Logger.Msg("Application.focusChanged! " + value);
    }
}