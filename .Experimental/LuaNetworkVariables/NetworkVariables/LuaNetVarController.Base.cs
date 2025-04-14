using ABI_RC.Core.Savior;
using ABI_RC.Core.Util;
using ABI_RC.Systems.ModNetwork;
using ABI.CCK.Components;
using ABI.Scripting.CVRSTL.Common;
using MoonSharp.Interpreter;
using UnityEngine;
using Coroutine = UnityEngine.Coroutine;

namespace NAK.LuaNetVars;

public partial class LuaNetVarController : MonoBehaviour
{
    private static readonly HashSet<int> _hashes = new();
        
    private const string MODULE_ID = "NAK.LNV:";

    private int _uniquePathHash;
    private string ModNetworkID { get; set; }
    private CVRLuaClientBehaviour _luaClientBehaviour;

    private readonly Dictionary<string, DynValue> _registeredNetworkVars = new();
    private readonly Dictionary<string, DynValue> _registeredNotifyCallbacks = new();
    private readonly Dictionary<string, DynValue> _registeredEventCallbacks = new();
    private readonly HashSet<string> _dirtyVariables = new();
    
    private bool _requestInitialSync;
    private CVRSpawnable _spawnable;
    private CVRObjectSync _objectSync;
    
    private bool _isInitialized;
    private Coroutine _syncCoroutine;
    
    #region Unity Events
    
    private void Awake()
        => _isInitialized = Initialize();
    
    private void OnDestroy()
        => Cleanup();

    private void OnEnable()
        => StartStopVariableUpdatesCoroutine(true);
    
    private void OnDisable()
        => StartStopVariableUpdatesCoroutine(false);
    
    #endregion Unity Events

    #region Private Methods
    
    private bool Initialize()
    {
        if (!TryGetComponent(out _luaClientBehaviour)) return false;
        if (!TryGetUniqueNetworkID(out _uniquePathHash)) return false;
            
        ModNetworkID = MODULE_ID + _uniquePathHash.ToString("X8");

        if (ModNetworkID.Length > ModNetworkManager.MaxMessageIdLength)
        {
            LuaNetVarsMod.Logger.Error($"ModNetworkID ({ModNetworkID}) exceeds max length of {ModNetworkManager.MaxMessageIdLength} characters!");
            return false;
        }
        
        _hashes.Add(_uniquePathHash);
        ModNetworkManager.Subscribe(ModNetworkID, OnMessageReceived);
        LuaNetVarsMod.Logger.Msg($"Registered LuaNetVarController with ModNetworkID: {ModNetworkID}");
        
        switch (_luaClientBehaviour.Context.objContext)
        {
            case CVRLuaObjectContext.AVATAR:
                _requestInitialSync = !_luaClientBehaviour.Context.IsWornByMe;
                break;
            case CVRLuaObjectContext.PROP:
                _spawnable = _luaClientBehaviour.Context.RootComponent as CVRSpawnable;
                _requestInitialSync = !_luaClientBehaviour.Context.IsSpawnedByMe;
                break;
            case CVRLuaObjectContext.WORLD:
                _objectSync = GetComponentInParent<CVRObjectSync>();
                _requestInitialSync = true; // idk probably works
                break;
            default:
                _requestInitialSync = true;
                break;
        }
        
        return true;
    }

    // TODO: evaluate if having dedicated globals is better behaviour (i think so)
    // private void ConfigureLuaEnvironment()
    // {
    //     _luaClientBehaviour.script.Globals["SendLuaEvent"] = DynValue.NewCallback(SendLuaEventCallback);
    // }

    private void Cleanup()
    {
        _eventTracker.Clear();
        
        if (_uniquePathHash == 0 || string.IsNullOrEmpty(ModNetworkID)) 
            return;
        
        ModNetworkManager.Unsubscribe(ModNetworkID);
        LuaNetVarsMod.Logger.Msg($"Unsubscribed LuaNetVarController with ModNetworkID: {ModNetworkID}");
        _hashes.Remove(_uniquePathHash);
    }

    private void StartStopVariableUpdatesCoroutine(bool start)
    {
        if (_syncCoroutine != null) StopCoroutine(_syncCoroutine);
        _syncCoroutine = null;
        if (start) _syncCoroutine = StartCoroutine(SendVariableUpdatesCoroutine());
    }
    
    private System.Collections.IEnumerator SendVariableUpdatesCoroutine()
    {
        while (isActiveAndEnabled)
        {
            yield return new WaitForSeconds(0.1f);
            if (IsSyncOwner()) SendVariableUpdates();
            if (!_requestInitialSync) continue;
            _requestInitialSync = false;
            RequestVariableSync();
        }
    }
    
    #endregion Private Methods

    #region Ownership Methods
    
    public bool IsSyncOwner()
    {
        if (_objectSync) return _objectSync.SyncedByMe; // idk
        if (_spawnable)
        {
            if (_spawnable.IsSyncedByMe()) return true; // is held / attached locally
            CVRSyncHelper.PropData propData = CVRSyncHelper.Props.Find(x => x.InstanceId == _spawnable.instanceId);
            if (propData != null) return propData.syncedBy == MetaPort.Instance.ownerId; // last updated by me
            return false; // not held / attached locally and not last updated by me
        }
        return false;
    }
    
    public string GetSyncOwner()
    {
        if (_objectSync) return _objectSync.syncedBy;
        if (_spawnable)
        {
            CVRSyncHelper.PropData propData = CVRSyncHelper.Props.Find(x => x.InstanceId == _spawnable.instanceId);
            return propData?.syncedBy ?? string.Empty;
        }
        return string.Empty;
    }
    
    #endregion Ownership Methods
}