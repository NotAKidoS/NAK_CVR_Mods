using ABI_RC.Core.Base;
using ABI.Scripting.CVRSTL.Common;
using JetBrains.Annotations;
using MoonSharp.Interpreter;

namespace NAK.LuaNetworkVariables.Modules;

[PublicAPI]
public class LuaNetModule : BaseScriptedStaticWrapper
{
    public const string MODULE_ID = "NetworkModule";

    private LuaNetVarController _controller;

    public LuaNetModule(CVRLuaContext context) : base(context)
    {
        _controller = context.behaviour.AddComponentIfMissing<LuaNetVarController>();
    }

    internal static object RegisterUserData(Script script, CVRLuaContext context)
    {
        // Register the LuaNetModule type with MoonSharp
        UserData.RegisterType<LuaNetModule>(InteropAccessMode.Default, MODULE_ID);
        return new LuaNetModule(context);
    }

    #region Module Instance Methods

    /// <summary>
    /// Registers a network variable that can be synchronized over the network.
    /// </summary>
    /// <param name="varName">The name of the variable to register.</param>
    public void RegisterNetworkVar(string varName)
    {
        CheckIfCanAccessMethod(nameof(RegisterNetworkVar), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.ANY);

        if (_controller == null)
        {
            LuaNetworkVariablesMod.Logger.Error("LuaNetVarController is null.");
            return;
        }

        _controller.RegisterNetworkVar(varName);
    }

    /// <summary>
    /// Registers a callback function to be called when the specified network variable changes.
    /// </summary>
    /// <param name="varName">The name of the variable to watch.</param>
    /// <param name="callback">The Lua function to call when the variable changes.</param>
    public void RegisterNotifyCallback(string varName, DynValue callback)
    {
        CheckIfCanAccessMethod(nameof(RegisterNotifyCallback), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.ANY);

        if (_controller == null)
        {
            LuaNetworkVariablesMod.Logger.Error("LuaNetVarController is null.");
            return;
        }

        _controller.RegisterNotifyCallback(varName, callback);
    }

    /// <summary>
    /// Registers a callback function to be called when the specified event is received.
    /// </summary>
    /// <param name="eventName">The name of the event to watch.</param>
    /// <param name="callback">The Lua function to call when the event occurs.</param>
    public void RegisterEventCallback(string eventName, DynValue callback)
    {
        CheckIfCanAccessMethod(nameof(RegisterEventCallback), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.ANY);

        if (_controller == null)
        {
            LuaNetworkVariablesMod.Logger.Error("LuaNetVarController is null.");
            return;
        }

        _controller.RegisterEventCallback(eventName, callback);
    }

    /// <summary>
    /// Sends a Lua event to other clients.
    /// </summary>
    /// <param name="eventName">The name of the event to send.</param>
    /// <param name="args">Optional arguments to send with the event.</param>
    public void SendLuaEvent(string eventName, params DynValue[] args)
    {
        CheckIfCanAccessMethod(nameof(SendLuaEvent), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.ANY);

        if (_controller == null)
        {
            LuaNetworkVariablesMod.Logger.Error("LuaNetVarController is null.");
            return;
        }

        _controller.SendLuaEvent(eventName, args);
    }
    
    /// <summary>
    /// Sends a Lua event to other clients.
    /// </summary>
    /// <param name="eventName">The name of the event to send.</param>
    /// <param name="args">Optional arguments to send with the event.</param>
    public void SendLuaEventToUser(string eventName, string userId, params DynValue[] args)
    {
        CheckIfCanAccessMethod(nameof(SendLuaEventToUser), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.ANY);

        if (_controller == null)
        {
            LuaNetworkVariablesMod.Logger.Error("LuaNetVarController is null.");
            return;
        }

        _controller.SendLuaEventToUser(eventName, userId, args);
    }

    /// <summary>
    /// Checks if the current client is the owner of the synchronized object.
    /// </summary>
    public bool IsSyncOwner()
    {
        CheckIfCanAccessMethod(nameof(IsSyncOwner), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.ANY);

        if (_controller == null)
        {
            LuaNetworkVariablesMod.Logger.Error("LuaNetVarController is null.");
            return false;
        }

        return _controller.IsSyncOwner();
    }

    public string GetSyncOwner()
    {
        CheckIfCanAccessMethod(nameof(GetSyncOwner), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.ANY);

        if (_controller == null)
        {
            LuaNetworkVariablesMod.Logger.Error("LuaNetVarController is null.");
            return string.Empty;
        }

        return _controller.GetSyncOwner();
    }

    #endregion
}