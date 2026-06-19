using ABI_RC.Core.Player;
using ABI_RC.Systems.ChatBox;
using ABI_RC.Systems.UI.UILib;
using ABI_RC.Systems.UI.UILib.UIObjects;
using ABI_RC.Systems.UI.UILib.UIObjects.Components;
using MelonLoader;

namespace NAK.FeedbackDotChilloutVRDotNet;

public class FeedbackDotChilloutVRDotNetMod : MelonMod
{
    private static MelonPreferences_Category _prefs;
    private static MelonPreferences_Entry<bool> _autoReact;

    public override void OnInitializeMelon()
    {
        // Emails:
        // Moderation Reports moderation@chilloutvr.net
        // Legal legal@chilloutvr.net
        // Team (general feedback, bug reports) team@chilloutvr.net

        // Websites:
        // Feedback (Issue Tracker) feedback.chilloutvr.net
        // Docs docs.chilloutvr.net
        // Hub hub.chilloutvr.net
        // Discord discord.gg/chilloutvr

        // Quick Messages:
        // I am not moderation. You will need to reach out to moderation@chilloutvr.net.

        // Auto React (goofy, behind toggle):
        // issue, bug -> feedback site
        // nak fix, @NotAKid fix, @NotAKidoS fix, pls fix, go fix -> no

        _prefs = MelonPreferences.CreateCategory(nameof(FeedbackDotChilloutVRDotNetMod));
        _autoReact = _prefs.CreateEntry("AutoReact", false, "Auto React",
            "Automatically reply to messages containing certain keywords (goofy).");

        SetupMenu();
        SetupAutoReact();
    }

    private static void SetupMenu()
    {
        Category category = QuickMenuAPI.MiscTabPage.AddCategory("Quick ChatBox Messages", true, true);
        category.ChildIndex = 0;

        // Websites
        AddMessageButton(category, "Feedback", "feedback.chilloutvr.net");
        AddMessageButton(category, "Docs", "docs.chilloutvr.net");
        AddMessageButton(category, "Hub", "hub.chilloutvr.net");
        AddMessageButton(category, "Discord", "discord.gg/chilloutvr");

        // Emails
        AddMessageButton(category, "Moderation", "moderation@chilloutvr.net");
        AddMessageButton(category, "Legal", "legal@chilloutvr.net");
        AddMessageButton(category, "Team", "team@chilloutvr.net");

        // Canned responses
        AddMessageButton(category, "Not Moderation",
            "I am not moderation. You will need to reach out to moderation@chilloutvr.net.");

        // Toggle for the goofy auto-react interceptor
        ToggleButton autoReactToggle = category.AddToggle("Auto React",
            "Automatically reply to messages with certain keywords (goofy).",
            _autoReact.Value);
        autoReactToggle.OnValueUpdated += b => _autoReact.Value = b;
    }

    private static Button AddMessageButton(Category category, string name, string message)
    {
        Button button = category.AddButton(name, string.Empty, message, ButtonStyle.TextOnly);
        button.OnPress += () => ChatBoxAPI.SendMessage(message, true, true, false);
        return button;
    }

    private static readonly ChatBoxAPI.InterceptorResult _emptyCauseCantReturnNull = new(false, false);
    
    private static void SetupAutoReact()
    {
        ChatBoxAPI.AddReceivingInterceptor(message =>
        {
            if (_autoReact.Value)
            {
                string text = message.Message;
                string username = CVRPlayerManager.Instance.TryGetPlayerName(message.SenderGuid);

                // Order matters: catch the "fix" demands before the generic feedback keywords.
                if (ContainsAny(text, "nak fix", "@NotAKid fix", "@NotAKidoS fix", "pls fix", "go fix"))
                    ChatBoxAPI.SendMessage($"@{username} no", true, true, false);
                else if (ContainsAny(text, "issue", "bug", "feedback"))
                    ChatBoxAPI.SendMessage($"@{username} feedback.chilloutvr.net", true, true, false);
                
                // game broke -> remove mods, find player.log
                
            }
            return _emptyCauseCantReturnNull;
        });
    }

    private static bool ContainsAny(string source, params string[] values)
    {
        foreach (string value in values)
        {
            if (source.Contains(value, StringComparison.InvariantCultureIgnoreCase))
                return true;
        }
        return false;
    }
}