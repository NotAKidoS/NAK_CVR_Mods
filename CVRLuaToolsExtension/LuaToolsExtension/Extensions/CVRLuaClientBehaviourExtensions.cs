using ABI.CCK.Components;
using ABI.Scripting.CVRSTL.Client;
using System.Diagnostics;
using MTJobSystem;

namespace NAK.CVRLuaToolsExtension;

public static class CVRLuaClientBehaviourExtensions
{
    internal static readonly Dictionary<CVRLuaClientBehaviour, bool> _isRestarting = new();

    #region Public Methods
    
    public static void Restart(this CVRLuaClientBehaviour behaviour)
    {
        if (_isRestarting.TryGetValue(behaviour, out bool isRestarting) && isRestarting)
        {
            CVRLuaToolsExtensionMod.Logger.Warning($"Restart is already in progress for {behaviour.ScriptName}.");
            return;
        }

        _isRestarting[behaviour] = true;

        bool wasEnabled = behaviour.enabled;
        if (behaviour.Crashed)
        {
            CVRLuaToolsExtensionMod.Logger.Warning($"Restarting a script ({behaviour.ScriptName}) in a crashed state. Unable to determine original enabled state, defaulting to true.");
            wasEnabled = true;
        }
        
        behaviour.enabled = false;
        
        Task.Run(() =>
        {
            try
            {
                behaviour.ResetScriptCompletely();
                if (CVRLuaToolsExtensionMod.EntryAttemptInitOffMainThread.Value)
                {
                    CVRLuaToolsExtensionMod.Logger.Warning("Attempting to initialize the lua script off main thread. This may cause crashes or corruption if you are accessing UnityEngine Objects outside of Unity's callbacks!");
                    behaviour.DoInitialLoadOfScript();
                }
            }
            catch (Exception e)
            {
                CVRLuaToolsExtensionMod.Logger.Error(e); // don't wanna die in task
            }
            
            MTJobManager.RunOnMainThread("RestartScript", () =>
            {
                Stopwatch stopwatch = new();
                stopwatch.Start();

                try
                {
                    if (!CVRLuaToolsExtensionMod.EntryAttemptInitOffMainThread.Value)
                        behaviour.DoInitialLoadOfScript();

                    if (behaviour.InitialCodeRun) behaviour.IsScriptInitialized = true; // allow callbacks to run again
                    
                    // invoke custom event on reset
                    if (!behaviour.ShouldSkipEvent("CVRLuaTools_OnReset")) behaviour.ExecuteEvent(5000, "CVRLuaTools_OnReset");
                    
                    // re-enable the script & invoke the lifecycle events
                    if (!behaviour.ShouldSkipEvent("Awake")) behaviour.ExecuteEvent(1000, "Awake");
                    behaviour.enabled = wasEnabled;
                    if (wasEnabled)
                    {
                        if (!behaviour.ShouldSkipEvent("OnEnable")) behaviour.ExecuteEvent(1000, "OnEnable");
                        if (!behaviour.ShouldSkipEvent("Start")) behaviour.ExecuteEvent(1000, "Start");
                    }
                }
                catch (Exception e)
                {
                    CVRLuaToolsExtensionMod.Logger.Error(e); // don't wanna die prior to resetting restart flag
                }

                _isRestarting.Remove(behaviour);
                
                stopwatch.Stop();
                CVRLuaToolsExtensionMod.Logger.Msg($"Restarted {behaviour.ScriptName} in {stopwatch.ElapsedMilliseconds}ms.");
            });
        });
    }
    
    #endregion Public Methods

    #region Private Methods
    
    private static void ResetScriptCompletely(this CVRLuaClientBehaviour behaviour)
    {
        var boundObjectEntries = new List<LuaScriptFactory.BoundObjectEntry>();
        foreach (var kvp in behaviour.BoundObjects)
        {
            LuaScriptFactory.BoundObjectEntry entry = new(behaviour, behaviour.Context, kvp.Key, kvp.Value);
            boundObjectEntries.Add(entry);
        }

        behaviour.IsScriptInitialized = false; // prevent callbacks from causing null refs while restarting
        behaviour.InitialCodeRun = false; // needs to be set so LoadAndRunScriptIfNeeded will run the script again
        behaviour.Crashed = false; // reset the crash flag

        behaviour._scriptGlobalFunctions.Clear(); // will be repopulated
        behaviour._startupMessageQueue.Clear(); // will be repopulated
        behaviour.LogInfo("[CVRLuaToolsExtension] Resetting script...\n");

        behaviour.script = null;
        behaviour.script = LuaScriptFactory.ForLuaBehaviour(behaviour, boundObjectEntries, behaviour.gameObject, behaviour.transform);
                
        behaviour.InitTimerIfNeeded(); // only null if crashed prior
        behaviour.script.AttachDebugger(behaviour.timer); // reattach the debugger
    }
    
    private static void DoInitialLoadOfScript(this CVRLuaClientBehaviour behaviour)
    {
        behaviour.LogInfo("[CVRLuaToolsExtension] Interpreting " + behaviour.ScriptName + "...\n");
        behaviour.LoadAndRunScript(behaviour.ScriptText); // fucking slow
        behaviour.PopulateScriptGlobalFunctionsCache(behaviour.script.Globals); // tbh don't think we need to clear in first place
        behaviour.HandleMessageQueueEntries(); // shouldn't be any
        behaviour.InitialCodeRun = true;
    }
    
    #endregion Private Methods
}