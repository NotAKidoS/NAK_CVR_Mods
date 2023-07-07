using ABI_RC.Core.AudioEffects;
using System.Reflection;
using UnityEngine;

namespace NAK.MuteSFX;

public static class AudioModuleManager
{
    public const string sfx_mute = "MuteSFX_sfx_mute";
    public const string sfx_unmute = "MuteSFX_sfx_unmute";

    public static void SetupDefaultAudioClips()
    {
        string path = Application.streamingAssetsPath + "/Cohtml/UIResources/GameUI/mods/MuteSFX/audio/";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            MuteSFX.Logger.Msg("Created MuteSFX/audio directory!");
        }

        string[] clipNames = { "sfx_mute.wav", "sfx_unmute.wav" };
        foreach (string clipName in clipNames)
        {
            string clipPath = Path.Combine(path, clipName);
            if (!File.Exists(clipPath))
            {
                byte[] clipData = null;
                string resourceName = "MuteSFX.SFX." + clipName;
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    clipData = new byte[stream.Length];
                    stream.Read(clipData, 0, clipData.Length);
                }

                using (FileStream fileStream = new FileStream(clipPath, FileMode.CreateNew))
                {
                    fileStream.Write(clipData, 0, clipData.Length);
                }

                MuteSFX.Logger.Msg("Placed missing sfx in audio folder: " + clipName);
            }
        }
    }

    public static void PlayAudioModule(string module)
    {
        InterfaceAudio.PlayModule(module);
    }
}