using ABI_RC.Systems.Communications.Audio.TTS;
using ABI.Scripting.CVRSTL.Common;
using JetBrains.Annotations;
using MoonSharp.Interpreter;

namespace NAK.LuaTTS.Modules;

[PublicAPI] // fak off its used
public class TTSLuaModule : BaseScriptedStaticWrapper
{
    public TTSLuaModule(CVRLuaContext context) : base(context)
    {
        // yes
    }

    internal static object RegisterUserData(Script script, CVRLuaContext context)
    {
        UserData.RegisterType<TTSLuaModule>(InteropAccessMode.Default, "TextToSpeech");
        return new TTSLuaModule(context);
    }

    // Check if TTS is playing
    public bool IsPlaying()
    {
        CheckIfCanAccessMethod(nameof(IsPlaying), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.LOCAL);
        
        return Comms_TTSHandler.Instance.IsPlaying;
    }
    
    // Check if TTS is processing a message
    public bool IsProcessing()
    {
        CheckIfCanAccessMethod(nameof(IsProcessing), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.LOCAL);
        
        return Comms_TTSHandler.Instance.IsProcessing;
    }
    
    // Check if TTS has no modules (only true for proton?)
    public bool HasAnyModules()
    {
        CheckIfCanAccessMethod(nameof(HasAnyModules), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.LOCAL);
        
        return Comms_TTSHandler._modules.Count > 0;
    }
    
    // Get all available TTS modules
    public string[] GetAvailableModules()
    {
        CheckIfCanAccessMethod(nameof(GetAvailableModules), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.LOCAL);
        
        return Comms_TTSHandler._modules.Keys.ToArray();
    }
    
    // Get the current TTS module
    public string GetCurrentModule()
    {
        CheckIfCanAccessMethod(nameof(GetCurrentModule), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.LOCAL);
        
        return Comms_TTSHandler.Instance.CurrentModuleId;
    }

    // Set the current TTS module
    public void SetCurrentModule(string moduleId)
    {
        CheckIfCanAccessMethod(nameof(SetCurrentModule), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.LOCAL);
        
         Comms_TTSHandler.Instance.ChangeModule(moduleId);
    }

    // Process a message for TTS playback
    public void ProcessMessage(string message)
    {
        CheckIfCanAccessMethod(nameof(ProcessMessage), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.LOCAL);
        
        Comms_TTSHandler.Instance.ProcessMessage(message);
    }

    // Cancel any currently playing TTS message
    public void CancelMessage()
    {
        CheckIfCanAccessMethod(nameof(CancelMessage), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.LOCAL);
        
        Comms_TTSHandler.Instance.ProcessMessage(string.Empty); // empty message cancels the current message
    }

    // Get all available voices for the current module
    public string[] GetAvailableVoices()
    {
        CheckIfCanAccessMethod(nameof(GetAvailableVoices), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.LOCAL);
        
        return Comms_TTSHandler.Instance.CurrentModule.Voices.Keys.ToArray();
    }

    // Get the current voice for the module
    public string GetCurrentVoice()
    {
        CheckIfCanAccessMethod(nameof(GetCurrentVoice), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.LOCAL);
        
        return Comms_TTSHandler.Instance.CurrentModule.CurrentVoice;
    }

    // Set the current voice for the module
    public void SetCurrentVoice(string voiceName)
    {
        CheckIfCanAccessMethod(nameof(SetCurrentVoice), false,
            CVRLuaEnvironmentContext.CLIENT, CVRLuaObjectContext.ALL_BUT_EVENTS, CVRLuaOwnerContext.LOCAL);
        
        Comms_TTSHandler.Instance.ChangeVoice(voiceName);
    }
}