using ABI_RC.Core.Savior;
using ABI_RC.Systems.ModNetwork;
using MoonSharp.Interpreter;
using Unity.Services.Authentication.Internal;

namespace NAK.LuaNetVars
{
    public partial class LuaNetVarController
    {
        private enum MessageType : byte
        {
            LuaVariable = 0,
            LuaEvent = 1,
            SyncVariables = 2,
            RequestSync = 3
        }

        private readonly LuaEventTracker _eventTracker = new();

        #region Mod Network Events

        private void OnMessageReceived(ModNetworkMessage msg)
        {
            msg.Read(out byte msgTypeRaw);
            if (!Enum.IsDefined(typeof(MessageType), msgTypeRaw)) return;

            MessageType msgType = (MessageType)msgTypeRaw;
            switch (msgType)
            {
                case MessageType.LuaVariable:
                    HandleLuaVariableUpdate(msg);
                    break;
                case MessageType.LuaEvent:
                    HandleLuaEvent(msg);
                    break;
                case MessageType.SyncVariables:
                    HandleSyncVariables(msg);
                    break;
                case MessageType.RequestSync:
                    HandleRequestSyncVariables(msg);
                    break;
            }
        }

        private void HandleLuaVariableUpdate(ModNetworkMessage msg)
        {
            msg.Read(out string varName);
            DynValue newValue = DeserializeDynValue(msg);

            LuaNetVarsMod.Logger.Msg($"Received LuaVariable update: {varName} = {newValue}");

            if (_registeredNetworkVars.TryGetValue(varName, out DynValue var))
            {
                UpdateNetworkVariable(varName, var, newValue);
            }
            else
            {
                LuaNetVarsMod.Logger.Warning($"Received update for unregistered variable {varName}");
            }
        }

        private void HandleLuaEvent(ModNetworkMessage msg)
        {
            string senderId = msg.Sender;
            msg.Read(out string eventName);
            msg.Read(out int argsCount);

            DateTime lastInvokeTime = _eventTracker.GetLastInvokeTimeForSender(eventName, senderId);
            LuaEventContext context = LuaEventContext.Create(senderId, lastInvokeTime);

            // Update tracking
            _eventTracker.UpdateInvokeTime(eventName, senderId);

            // Read event arguments
            var args = new DynValue[argsCount + 1]; // +1 for context
            args[0] = DynValue.FromObject(_luaClientBehaviour.script, context.ToLuaTable(_luaClientBehaviour.script));

            for (int i = 0; i < argsCount; i++)
            {
                args[i + 1] = DeserializeDynValue(msg);
            }

            LuaNetVarsMod.Logger.Msg($"Received LuaEvent: {eventName} from {context.SenderName} with {argsCount} args");

            InvokeLuaEvent(eventName, args);
        }

        private void HandleSyncVariables(ModNetworkMessage msg)
        {
            msg.Read(out int varCount);
            for (int i = 0; i < varCount; i++)
            {
                msg.Read(out string varName);
                DynValue newValue = DeserializeDynValue(msg);

                if (_registeredNetworkVars.TryGetValue(varName, out DynValue var))
                {
                    UpdateNetworkVariable(varName, var, newValue);
                }
                else
                {
                    LuaNetVarsMod.Logger.Warning($"Received sync for unregistered variable {varName}");
                }
            }
        }
        
        private void HandleRequestSyncVariables(ModNetworkMessage msg)
        {
            if (!IsSyncOwner()) return;
            SendVariableSyncToUser(msg.Sender);
        }

        #endregion

        #region Event Invocation

        private void InvokeLuaEvent(string eventName, DynValue[] args)
        {
            if (_registeredEventCallbacks.TryGetValue(eventName, out DynValue callback))
            {
                _luaClientBehaviour.script.Call(callback, args);
            }
            else
            {
                LuaNetVarsMod.Logger.Warning($"No registered callback for event {eventName}");
            }
        }

        #endregion

        #region Sending Methods

        private void SendVariableUpdates()
        {
            if (_dirtyVariables.Count == 0) return;

            using ModNetworkMessage modMsg = new(ModNetworkID); // can pass target userids as params if needed
            modMsg.Write((byte)MessageType.SyncVariables);
            modMsg.Write(_dirtyVariables.Count);
            modMsg.Send();

            foreach (var varName in _dirtyVariables)
            {
                modMsg.Write(varName);
                SerializeDynValue(modMsg, _registeredNetworkVars[varName]);
            }

            _dirtyVariables.Clear();
        }
        
        private void SendVariableSyncToUser(string userId)
        {
            using ModNetworkMessage modMsg = new(ModNetworkID, userId);
            modMsg.Write((byte)MessageType.SyncVariables);
            modMsg.Write(_registeredNetworkVars.Count);
            foreach (var kvp in _registeredNetworkVars)
            {
                modMsg.Write(kvp.Key);
                SerializeDynValue(modMsg, kvp.Value);
            }
            modMsg.Send();
            
            LuaNetVarsMod.Logger.Msg($"Sent variable sync to {userId}");
        }
        
        private void RequestVariableSync()
        {
            using ModNetworkMessage modMsg = new(ModNetworkID);
            modMsg.Write((byte)MessageType.RequestSync);
            modMsg.Send();
            LuaNetVarsMod.Logger.Msg("Requested variable sync");
        }

        // private DynValue SendLuaEventCallback(ScriptExecutionContext context, CallbackArguments args)
        // {
        //     if (args.Count < 1) return DynValue.Nil;
        //
        //     var eventName = args[0].CastToString();
        //     var eventArgs = args.GetArray().Skip(1).ToArray();
        //
        //     SendLuaEvent(eventName, eventArgs);
        //
        //     return DynValue.Nil;
        // }

        internal void SendLuaEvent(string eventName, DynValue[] args)
        {
            string senderId = MetaPort.Instance.ownerId;
            DateTime lastInvokeTime = _eventTracker.GetLastInvokeTimeForSender(eventName, senderId);
            LuaEventContext context = LuaEventContext.Create(senderId, lastInvokeTime);

            // Update tracking
            _eventTracker.UpdateInvokeTime(eventName, senderId);

            var argsWithContext = new DynValue[args.Length + 1];
            argsWithContext[0] = DynValue.FromObject(_luaClientBehaviour.script, context.ToLuaTable(_luaClientBehaviour.script));
            Array.Copy(args, 0, argsWithContext, 1, args.Length);

            InvokeLuaEvent(eventName, argsWithContext);

            using ModNetworkMessage modMsg = new(ModNetworkID);
            modMsg.Write((byte)MessageType.LuaEvent);
            modMsg.Write(eventName);
            modMsg.Write(args.Length);

            foreach (DynValue arg in args)
                SerializeDynValue(modMsg, arg);

            modMsg.Send();
        }

        #endregion
    }
}