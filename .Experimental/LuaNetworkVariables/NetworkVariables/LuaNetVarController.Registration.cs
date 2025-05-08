using MoonSharp.Interpreter;

namespace NAK.LuaNetworkVariables;

public partial class LuaNetVarController
{
    internal void RegisterNetworkVar(string varName)
    {
        if (_registeredNetworkVars.ContainsKey(varName))
        {
            LuaNetworkVariablesMod.Logger.Warning($"Network variable {varName} already registered!");
            return;
        }

        _registeredNetworkVars[varName] = DynValue.Nil;
        _luaClientBehaviour.script.Globals[varName] = DynValue.Nil;

        RegisterGetterFunction(varName);
        RegisterSetterFunction(varName);

        LuaNetworkVariablesMod.Logger.Msg($"Registered network variable {varName}");
    }

    private void RegisterGetterFunction(string varName)
    {
        _luaClientBehaviour.script.Globals["Get" + varName] = DynValue.NewCallback((context, args) =>
        {
            return _registeredNetworkVars.TryGetValue(varName, out var value) ? value : DynValue.Nil;
        });
    }

    private void RegisterSetterFunction(string varName)
    {
        _luaClientBehaviour.script.Globals["Set" + varName] = DynValue.NewCallback((context, args) =>
        {
            if (args.Count < 1) return DynValue.Nil;

            var newValue = args[0];
            if (!IsSupportedDynValue(newValue))
            {
                LuaNetworkVariablesMod.Logger.Error($"Unsupported DynValue type: {newValue.Type} for variable {varName}");
                return DynValue.Nil;
            }

            if (_registeredNetworkVars.TryGetValue(varName, out var oldValue))
            {
                UpdateNetworkVariable(varName, oldValue, newValue);
            }

            return DynValue.Nil;
        });
    }

    private void UpdateNetworkVariable(string varName, DynValue oldValue, DynValue newValue)
    {
        _registeredNetworkVars[varName] = newValue;
        _luaClientBehaviour.script.Globals[varName] = newValue;
        _dirtyVariables.Add(varName);

        if (_registeredNotifyCallbacks.TryGetValue(varName, out var callback))
        {
            _luaClientBehaviour.script.Call(callback, DynValue.NewString(varName), oldValue, newValue);
        }
    }

    internal void RegisterNotifyCallback(string varName, DynValue callback)
    {
        if (!ValidateCallback(callback) || !ValidateNetworkVar(varName)) return;

        if (_registeredNotifyCallbacks.ContainsKey(varName))
            LuaNetworkVariablesMod.Logger.Warning($"Overwriting notify callback for {varName}");

        _registeredNotifyCallbacks[varName] = callback;
        LuaNetworkVariablesMod.Logger.Msg($"Registered notify callback for {varName}");
    }

    internal void RegisterEventCallback(string eventName, DynValue callback)
    {
        if (!ValidateCallback(callback)) return;

        if (_registeredEventCallbacks.ContainsKey(eventName))
            LuaNetworkVariablesMod.Logger.Warning($"Overwriting event callback for {eventName}");

        _registeredEventCallbacks[eventName] = callback;
        LuaNetworkVariablesMod.Logger.Msg($"Registered event callback for {eventName}");
    }

    private bool ValidateCallback(DynValue callback)
    {
        if (callback?.Function != null) return true;
        LuaNetworkVariablesMod.Logger.Error("Passed DynValue must be a function");
        return false;
    }

    private bool ValidateNetworkVar(string varName)
    {
        if (_registeredNetworkVars.ContainsKey(varName)) return true;
        LuaNetworkVariablesMod.Logger.Error($"Attempted to register notify callback for non-registered variable {varName}.");
        return false;
    }
}