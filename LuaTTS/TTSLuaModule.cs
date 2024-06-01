using ABI_RC.Systems.Communications.Audio.TTS;
using ABI.Scripting.CVRSTL.Common;
using JetBrains.Annotations;
using MoonSharp.Interpreter;

namespace NAK.LuaTTS.Modules;

[PublicAPI] // fak off its used
public class TTSLuaModule
{
    private CVRLuaContext context;
        
    internal TTSLuaModule(CVRLuaContext context)
    {
        this.context = context; // we don't really need the context for this shit module
    }

    internal static object RegisterUserData(Script script, CVRLuaContext context)
    {
        UserData.RegisterType<TTSLuaModule>(InteropAccessMode.Default, "TextToSpeech");
        return new TTSLuaModule(context);
    }

    // Check if TTS is playing
    public static bool IsPlaying()
        => Comms_TTSHandler.Instance.IsPlaying;
    
    // Check if TTS is processing a message
    public static bool IsProcessing()
        => Comms_TTSHandler.Instance.IsProcessing;
    
    // Check if TTS has no modules (only true for proton?)
    public static bool HasAnyModules()
        => Comms_TTSHandler._modules.Count > 0;
    
    // Get all available TTS modules
    public static string[] GetAvailableModules()
        => Comms_TTSHandler._modules.Keys.ToArray();
    
    // Get the current TTS module
    public static string GetCurrentModule()
        => Comms_TTSHandler.Instance.CurrentModuleId;

    // Set the current TTS module
    public static void SetCurrentModule(string moduleId)
        => Comms_TTSHandler.Instance.ChangeModule(moduleId);

    // Process a message for TTS playback
    public static void ProcessMessage(string message)
        => Comms_TTSHandler.Instance.ProcessMessage(message);

    // Cancel any currently playing TTS message
    public static void CancelMessage()
        => Comms_TTSHandler.Instance.ProcessMessage(string.Empty); // empty message cancels the current message

    // Get all available voices for the current module
    public static string[] GetAvailableVoices()
        => Comms_TTSHandler.Instance.CurrentModule.Voices.Keys.ToArray();

    // Get the current voice for the module
    public static string GetCurrentVoice()
        => Comms_TTSHandler.Instance.CurrentModule.CurrentVoice;

    // Set the current voice for the module
    public static void SetCurrentVoice(string voiceName)
        => Comms_TTSHandler.Instance.ChangeVoice(voiceName);
}