using ABI_RC.Core;
using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using ABI_RC.Systems.Movement;
using UnityEngine;

namespace NAK.ChatBoxExtensions.Integrations;

internal class ChilloutVRBaseCommands : CommandBase
{
    public static void RegisterCommands()
    {
        Commands.RegisterCommand("respawn",
        onCommandSent: (message, sound, displayMsg) =>
        {
            LocalCommandIgnoreOthers(message, args =>
            {
                RootLogic.Instance.Respawn();
            });
        },
        onCommandReceived: (sender, message, sound, displayMsg) =>
        {
            RemoteCommandListenForAll(message, (args) =>
            {
                RootLogic.Instance.Respawn();
            });
        });

        Commands.RegisterCommand("mute",
        onCommandSent: (message, sound, displayMsg) =>
        {
            LocalCommandIgnoreOthers(message, args =>
            {
                AudioManagement.SetMicrophoneActive(false);
            });
        },
        onCommandReceived: (sender, message, sound, displayMsg) =>
        {
            RemoteCommandListenForAll(message, args =>
            {
                AudioManagement.SetMicrophoneActive(false);
            });
        });
        
        // teleport [x] [y] [z]
        Commands.RegisterCommand("teleport",
        onCommandSent: (message, sound, displayMsg) =>
        {
            LocalCommandIgnoreOthers(message, args =>
            {
                if (args.Length > 2 && float.TryParse(args[0], out float x) && float.TryParse(args[1], out float y) && float.TryParse(args[2], out float z))
                {
                    BetterBetterCharacterController.Instance.TeleportPlayerTo(new Vector3(x, y, z), false, false);
                }
            });
        },
        onCommandReceived: (sender, message, sound, displayMsg) =>
        {
            RemoteCommandListenForAll(message, args =>
            {
                if (args.Length > 2 && float.TryParse(args[0], out float x) && float.TryParse(args[1], out float y) && float.TryParse(args[2], out float z))
                {
                    BetterBetterCharacterController.Instance.TeleportPlayerTo(new Vector3(x, y, z), false, false);
                }
            });
        });
        
        // tp [x] [y] [z]
        Commands.RegisterCommand("tp",
        onCommandSent: (message, sound, displayMsg) =>
        {
            LocalCommandIgnoreOthers(message, args =>
            {
                if (args.Length > 2 && float.TryParse(args[0], out float x) && float.TryParse(args[1], out float y) && float.TryParse(args[2], out float z))
                {
                    BetterBetterCharacterController.Instance.TeleportPlayerTo(new Vector3(x, y, z), false, false);
                }
            });
        },
        onCommandReceived: (sender, message, sound, displayMsg) =>
        {
            RemoteCommandListenForAll(message, args =>
            {
                if (args.Length > 2 && float.TryParse(args[0], out float x) && float.TryParse(args[1], out float y) && float.TryParse(args[2], out float z))
                {
                    BetterBetterCharacterController.Instance.TeleportPlayerTo(new Vector3(x, y, z), false, false);
                }
            });
        });
        
        // teleport [player]
        Commands.RegisterCommand("teleport",
        onCommandSent: (message, sound, displayMsg) =>
        {
            LocalCommandIgnoreOthers(message, args =>
            {
                if (args.Length > 0)
                {
                    string player = args[0];
                    CVRPlayerEntity playerEnt = CVRPlayerManager.Instance.NetworkPlayers.FirstOrDefault(x => x.Username == player);
                    if (playerEnt != null) BetterBetterCharacterController.Instance.TeleportPlayerTo(playerEnt.PuppetMaster.transform.position, false, false);
                }
            });
        },
        onCommandReceived: (sender, message, sound, displayMsg) =>
        {
            RemoteCommandListenForAll(message, args =>
            {
                if (args.Length > 0)
                {
                    string player = args[0];
                    CVRPlayerEntity playerEnt = CVRPlayerManager.Instance.NetworkPlayers.FirstOrDefault(x => x.Username == player);
                    if (playerEnt != null) BetterBetterCharacterController.Instance.TeleportPlayerTo(playerEnt.PuppetMaster.transform.position, false, false);
                }
            });
        });
    }
}
