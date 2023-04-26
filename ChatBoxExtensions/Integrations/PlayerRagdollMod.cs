using Kafe.ChatBox;
using ml_prm;

namespace NAK.Melons.ChatBoxExtensions.Integrations;

internal class PlayerRagdollModCommands : CommandBase
{
    public static void RegisterCommands()
    {
        Commands.RegisterCommand("unragdoll",
        onCommandSent: (message, sound) =>
        {
            LocalCommandIgnoreOthers(message, (args) =>
            {
                if (RagdollController.Instance.IsRagdolled())
                {
                    RagdollController.Instance.SwitchRagdoll();
                }
            });
        },
        onCommandReceived: (sender, message, sound) =>
        {
            RemoteCommandListenForAll(message, (args) =>
            {
                if (RagdollController.Instance.IsRagdolled())
                {
                    RagdollController.Instance.SwitchRagdoll();
                }
            });
        });

        Commands.RegisterCommand("ragdoll",
        onCommandSent: (message, sound) =>
        {
            LocalCommandIgnoreOthers(message, (args) =>
            {
                bool switchRagdoll = true;

                if (args.Length > 0 && bool.TryParse(args[0], out bool state))
                {
                    switchRagdoll = state != RagdollController.Instance.IsRagdolled();
                }

                if (switchRagdoll)
                {
                    RagdollController.Instance.SwitchRagdoll();
                }
            });
        },
        onCommandReceived: (sender, message, sound) =>
        {
            RemoteCommandListenForAll(message, (args) =>
            {
                bool switchRagdoll = true;

                if (args.Length > 1 && bool.TryParse(args[1], out bool state))
                {
                    switchRagdoll = state != RagdollController.Instance.IsRagdolled();
                }

                if (switchRagdoll)
                {
                    RagdollController.Instance.SwitchRagdoll();
                }
            });
        });

        Commands.RegisterCommand("kill",
        onCommandSent: (message, sound) =>
        {
            LocalCommandIgnoreOthers(message, (args) =>
            {
                if (!RagdollController.Instance.IsRagdolled())
                {
                    RagdollController.Instance.SwitchRagdoll();
                }
            });
        },
        onCommandReceived: (sender, message, sound) =>
        {
            RemoteCommandListenForAll(message, (args) =>
            {
                if (!RagdollController.Instance.IsRagdolled())
                {
                    RagdollController.Instance.SwitchRagdoll();
                }
            });
        });
    }
}
