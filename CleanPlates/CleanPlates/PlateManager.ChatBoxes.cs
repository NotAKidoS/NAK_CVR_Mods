using ABI_RC.Core.AudioEffects;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core;
using ABI_RC.Systems.ChatBox;
using ABI_RC.Systems.Communications;
using MelonLoader;
using NAK.CleanPlates.Network;
using NAK.CleanPlates.UI;
using System.Collections;
using System.Text;
using UnityEngine;

namespace NAK.CleanPlates;

// Chatbox subsystem.
public static partial class PlateManager
{
    public static float LocalSoundVolume = 0.35f;
    public static float MentionVolume = 0.75f;
    public static string MentionOpenTag = "<color=#FFD166><b>";
    public static string MentionCloseTag = "</b></color>";
    public static bool LocalTypingSound = false;

    // Cap for TTS icon so it can't get stuck on forever.
    public static float MaxTtsSeconds = 60f;

    internal static string MentionTag = string.Empty;

    // Control characters break the bubble and HUD, so for the greater good, we are
    // intercepting all messages and sanitizing them (native bug).
    private static ChatBoxAPI.InterceptorResult MutateChatBoxMessageForTheGreaterGood(ChatBoxAPI.ChatBoxMessage message)
    {
        message.Message = SafeText.Clean(message.Message);
        return ChatBoxAPI.InterceptorResult.Ignore;
    }

    private static void OnMessageSent(ChatBoxAPI.ChatBoxMessage message)
        => OnMessageReceived(message);
    private static void OnIsTypingSent(ChatBoxAPI.ChatBoxTyping typing)
        => OnIsTypingReceived(typing);

    private static void OnTTSAudioStarted(AudioSource _, AudioClip clip)
    {
        float seconds = clip != null ? clip.length : 0f;
        seconds += 1f; // Buffer for processing delay & smoothed amplitude
        MelonCoroutines.Start(SendSignalTTSDelayed(seconds));
        if (!TryGetChat(PlayerSetup.Instance, out NameplateChat chat)) return;
        chat.SetPlayingTts(Mathf.Clamp(seconds, 0f, MaxTtsSeconds));
    }

    private static IEnumerator SendSignalTTSDelayed(float seconds)
    {
        // Tries to delay by comms delay. Probably iffy but gives an OK result...
        if (Comms_Manager.IsClientConnected)
        {
            const float bufferSeconds = 0.03f;
            const float maxDelaySeconds = 1f;
            float delaySeconds = Mathf.Clamp(
                (Comms_Manager.Instance.Client.Ping / 1000f) + bufferSeconds,
                0f,
                maxDelaySeconds
            );
            yield return new WaitForSecondsRealtime(delaySeconds);
        }
        CleanPlatesNetwork.SendSignalTTS(seconds);
    }

    private static void ApplyBubbleSettings(NameplateChat chat)
    {
        chat.SetBackgroundOpacity(ChatBoxBubbleBehavior.SettingBubbleOpacity);
        chat.SetBubbleScale(ChatBoxBubbleBehavior.SettingBubbleSize);
    }
    
    // The typing indicator will always show unless self moderation hides it.
    // This is so you can tell if someone is trying to communicate with you.
    private static bool ShouldShowTyping(string userGuid, ChatBoxAPI.MessageSource source, out bool muted)
    {
        muted = false;
        switch (MetaPort.Instance.SelfModerationManager.GetUserChatBoxVisibility(userGuid))
        {
            case ChatBoxManager.ChatBoxVisibility.UseGlobalSettings:
                muted = !ChatBoxManager.SettingEnabled
                        || (ChatBoxManager.SettingOnlyViewFriends && !Friends.FriendsWith(userGuid))
                        || (source == ChatBoxAPI.MessageSource.OSC && !ChatBoxManager.SettingOSCMessagesEnabled)
                        || (source == ChatBoxAPI.MessageSource.Mod && !ChatBoxManager.SettingModMessagesEnabled);
                break;
            case ChatBoxManager.ChatBoxVisibility.MsgsOsc:
                return source != ChatBoxAPI.MessageSource.Mod;
            case ChatBoxManager.ChatBoxVisibility.Msgs:
                return source is not (ChatBoxAPI.MessageSource.OSC or ChatBoxAPI.MessageSource.Mod);
            case ChatBoxManager.ChatBoxVisibility.None:
                return false;
        }
        return true;
    }

    // PlayNotificationSound gives us no volume control >:(
    private static void PlaySound(ChatBoxManager.NotificationType type, AudioClipField clip, Vector3 position, float volume)
    {
        if (!InterfaceAudio.Instance.TryGetClip(clip, out AudioClip audioClip)) return;

        ChatBoxManager manager = ChatBoxManager.Instance;
        AudioSource source = type == ChatBoxManager.NotificationType.Global
            ? manager.globalAudioSource
            : manager.localAudioSource;
        source.transform.position = position;
        source.PlayOneShot(audioClip, volume);
    }

    public static void SetPlayingTts(string senderGuid, float seconds)
    {
        if (!CVRPlayerManager.Instance.TryGetPlayerBase(senderGuid, out PlayerBase player)) return;
        if (!TryGetChat(player, out NameplateChat chat)) return;
        chat.SetPlayingTts(Mathf.Clamp(seconds, 0f, MaxTtsSeconds));
    }

    private static void OnIsTypingReceived(ChatBoxAPI.ChatBoxTyping typing)
    {
        if (!ShouldShowTyping(typing.SenderGuid, typing.Source, out bool muted)) return;
        bool isLocal = typing.SenderGuid == MetaPort.Instance.ownerId;
        if (!CVRPlayerManager.Instance.TryGetPlayerBase(typing.SenderGuid, out PlayerBase player)) return;
        if (!TryGetChat(player, out NameplateChat chat)) return;

        ApplyBubbleSettings(chat);
        bool wasTyping = chat.IsTyping;
        chat.SetTypingMuted(muted);
        chat.SetTyping(typing.IsTyping);
        if (isLocal) SetLocalTypingForCVRParameterStream(typing.IsTyping);

        if (!typing.IsTyping || wasTyping || !typing.TriggerNotification) return;
        if (muted || !ChatBoxManager.SettingEnableIsTypingSounds) return;
        if (isLocal && !LocalTypingSound) return;
        if (!chat.IsVisible) return;

        PlaySound(ChatBoxManager.NotificationType.Local, AudioClipField.ChatBoxTyping,
            chat.transform.position, isLocal ? LocalSoundVolume : 1f);
    }

    private static void OnMessageReceived(ChatBoxAPI.ChatBoxMessage message)
    {
        if (!message.DisplayOnChatBox) return;
        if (SafeText.IsBlank(message.Message)) return;
        if (!ChatBoxManager.ShouldShowMessage(message.SenderGuid, message.Source)) return;
        bool isLocal = message.SenderGuid == MetaPort.Instance.ownerId;
        if (!CVRPlayerManager.Instance.TryGetPlayerBase(message.SenderGuid, out PlayerBase player)) return;
        if (!TryGetChat(player, out NameplateChat chat)) return;

        ApplyBubbleSettings(chat);
        chat.SetTyping(false);
        if (isLocal) SetLocalTypingForCVRParameterStream(false);

        // Displayed user messages don't get shoved out by osc/mod spam
        if (chat.HasMessages && chat.NewestKind == ChatMessageKind.Message
            && message.Source != ChatBoxAPI.MessageSource.Internal)
            return;

        string body = MarkMentions(message.Message, out bool mentioned);

        // Mod/OSC messages are always pushed as bubbles, but for Internal messages
        // the user can configure if they route to HUD or bubble.
        if (message.Source != ChatBoxAPI.MessageSource.Internal
            || ChatBoxManager.ShouldReceiverShowMessage(ChatBoxManager.MessageReceiver.Bubble))
            chat.PushMessage(KindFor(message.Source), body, !message.TriggerNotification,
                SourceLabelFor(message));

        if (!message.TriggerNotification || !ChatBoxManager.SettingEnableOnMessageSounds) return;

        // No sound for a bubble that is collapsed out of view, the message still
        // queues up for whenever they wander back into range.
        if (chat.IsVisible)
            PlaySound(ChatBoxManager.NotificationType.Local, AudioClipField.ChatBoxMessage,
                chat.transform.position, isLocal ? LocalSoundVolume : 1f);

        // Pings still fire at any distance.
        if (!isLocal && ChatBoxManager.SettingEnableOnMessageMentionSounds && mentioned)
            RootLogic.Instance.StartExternCoroutine(PlayDelayedMention(chat.transform.position));
    }

    // Neutralizes rich text before we add our own tag, and only counts
    // a mention when the name actually ends there so NotAKid does not
    // light up for NotAKidoS.
    private static string MarkMentions(string message, out bool mentioned)
    {
        mentioned = false;
        message = message.Replace("<", "<\u200B");

        if (string.IsNullOrEmpty(MentionTag))
            return message;

        StringBuilder builder = null;
        int cursor = 0;
        while (true)
        {
            int index = message.IndexOf(MentionTag, cursor, StringComparison.OrdinalIgnoreCase);
            if (index < 0) break;

            int mentionTagLength = MentionTag.Length;
            int end = index + mentionTagLength;
            if (end < message.Length && (char.IsLetterOrDigit(message[end]) || message[end] == '_'))
            {
                cursor = end;
                continue;
            }

            mentioned = true;
            builder ??= new StringBuilder(message.Length + 32);
            builder.Append(message, cursor, index - cursor)
                .Append(MentionOpenTag)
                .Append(message, index, mentionTagLength)
                .Append(MentionCloseTag);
            cursor = end;
        }

        if (builder == null) return message;
        builder.Append(message, cursor, message.Length - cursor);
        return builder.ToString();
    }

    // The OSC ChatBoxIsTyping param reads IsTypingIndicatorActive off the gutted
    // native bubble, so need to write to it to keep that working.
    private static void SetLocalTypingForCVRParameterStream(bool typing)
        => ChatBoxManager.Instance.LocalPlayerBubble.IsTypingIndicatorActive = typing;

    private static IEnumerator PlayDelayedMention(Vector3 position)
    {
        yield return new WaitForSeconds(0.5f);
        PlaySound(ChatBoxManager.NotificationType.Global, AudioClipField.ChatBoxPing, position, MentionVolume);
    }

    private static string SourceLabelFor(ChatBoxAPI.ChatBoxMessage message) => message.Source switch
    {
        ChatBoxAPI.MessageSource.OSC => "OSC",
        ChatBoxAPI.MessageSource.Mod => "Mod",
        _ => null
    };

    private static ChatMessageKind KindFor(ChatBoxAPI.MessageSource source) => source switch
    {
        ChatBoxAPI.MessageSource.OSC => ChatMessageKind.OSC,
        ChatBoxAPI.MessageSource.Mod => ChatMessageKind.Mod,
        _ => ChatMessageKind.Message,
    };
}