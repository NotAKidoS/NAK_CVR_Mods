using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Util;
using ABI_RC.Systems.Communications;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.RuntimeDebug;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.YouAreMyPropNowWeAreHavingSoftTacosLater;

public class ChatBubblesMod : MelonMod
{
    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(YouAreMyPropNowWeAreHavingSoftTacosLater));

    private static readonly MelonPreferences_Entry<KeyCode> EntryChatBubblesKey =
        Category.CreateEntry("keyboard_bind", KeyCode.Y, display_name: "Chat Bubbles Key", 
            description: "Key to open chat bubble input.");
    
    private static readonly MelonPreferences_Entry<float> EntryBubbleDuration =
        Category.CreateEntry("bubble_duration", 5.0f, display_name: "Bubble Duration", 
            description: "Base duration in seconds for chat bubbles to stay visible.");
    
    private static readonly MelonPreferences_Entry<float> EntryDurationPerChar =
        Category.CreateEntry("duration_per_char", 0.05f, display_name: "Duration Per Character", 
            description: "Additional duration per character in the message.");
    
    private static readonly MelonPreferences_Entry<int> EntryPoolSize =
        Category.CreateEntry("pool_size", 20, display_name: "Pool Size", 
            description: "Maximum number of chat bubbles that can be displayed at once.");
    
    private static readonly MelonPreferences_Entry<Color> EntryBubbleColor =
        Category.CreateEntry("bubble_color", new Color(1.0f, 1.0f, 1.0f, 1.0f), display_name: "Bubble Color", 
            description: "Color of chat bubble text.");
    
    #endregion Melon Preferences
    
    #region Object Pool
    
    private class ChatBubble
    {
        public CVRPlayerEntity Player { get; set; }
        public string Message { get; set; }
        public float Duration { get; set; }
        public float Timer { get; set; }
        public bool IsActive { get; set; }
        
        public void Reset()
        {
            Player = null;
            Message = string.Empty;
            Duration = 0f;
            Timer = 0f;
            IsActive = false;
        }
    }
    
    private readonly List<ChatBubble> _chatBubblePool = [];
    private readonly List<ChatBubble> _activeBubbles = [];
    
    private void InitializePool()
    {
        MelonLogger.Msg("Initializing chat bubble pool with size: " + EntryPoolSize.Value);
        _chatBubblePool.Clear();
        
        for (int i = 0; i < EntryPoolSize.Value; i++)
        {
            _chatBubblePool.Add(new ChatBubble());
        }
    }
    
    private ChatBubble GetBubbleFromPool()
    {
        // First try to find an inactive bubble
        foreach (var bubble in _chatBubblePool)
        {
            if (!bubble.IsActive)
            {
                bubble.Reset();
                bubble.IsActive = true;
                _activeBubbles.Add(bubble);
                return bubble;
            }
        }
        
        // If all bubbles are active, reuse the oldest one
        if (_activeBubbles.Count > 0)
        {
            var oldestBubble = _activeBubbles[0];
            _activeBubbles.RemoveAt(0);
            oldestBubble.Reset();
            oldestBubble.IsActive = true;
            _activeBubbles.Add(oldestBubble);
            return oldestBubble;
        }
        
        // This should never happen if the pool is initialized properly
        MelonLogger.Warning("No chat bubbles available in pool!");
        return null;
    }
    
    private void ReturnBubbleToPool(ChatBubble bubble)
    {
        bubble.IsActive = false;
        _activeBubbles.Remove(bubble);
    }
    
    #endregion Object Pool
    
    #region Melon Events
    
    public override void OnInitializeMelon()
    {
        MelonLogger.Msg("Initializing Chat Bubbles Mod");
        InitializePool();
        
        CVRGameEventSystem.Communications.TextChat.Local.OnMessageReceived.AddListener(OnLocalMessageReceived);
        CVRGameEventSystem.Communications.TextChat.Direct.OnMessageReceived.AddListener(OnGlobalMessageReceived);
        CVRGameEventSystem.Communications.TextChat.Global.OnMessageReceived.AddListener(OnDirectMessageReceived);
        
        HarmonyInstance.Patch(
            typeof(ViewManager).GetMethod(nameof(ViewManager.SendToWorldUi),
                BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(ChatBubblesMod).GetMethod(nameof(OnViewManagerSendToWorldUi),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        MelonLogger.Msg("Chat Bubbles Mod initialized successfully");
    }

    public override void OnUpdate()
    {
        // Process key input to open chat
        if (Input.GetKeyDown(EntryChatBubblesKey.Value))
        {
            if (!CVRSyncHelper.IsConnectedToGameNetwork())
                return;

            IsInKeyboardToWriteANiceMessage = true;
            ViewManager.Instance.openMenuKeyboard(string.Empty);
        }
        
        // Update and render active chat bubbles
        UpdateChatBubbles();
    }
    
    private void UpdateChatBubbles()
    {
        // Make a copy of the list to avoid issues when modifying during iteration
        var bubblesToUpdate = new List<ChatBubble>(_activeBubbles);
        
        foreach (var bubble in bubblesToUpdate)
        {
            if (bubble.Player == null || bubble.Player.PuppetMaster == null)
            {
                ReturnBubbleToPool(bubble);
                continue;
            }
            
            bubble.Timer += Time.deltaTime;
            
            if (bubble.Timer >= bubble.Duration)
            {
                ReturnBubbleToPool(bubble);
                continue;
            }
            
            // Draw the bubble text at the player's nameplate position
            var nameplatePosition = bubble.Player.PuppetMaster.GetNamePlateWorldPosition();
            // Offset slightly above the nameplate
            nameplatePosition.y += 0.2f;
            
            RuntimeGizmos.DrawText(nameplatePosition, bubble.Message, 5, EntryBubbleColor.Value);
        }
    }
    
    #endregion Melon Events
    
    #region Game Events
    
    public static bool IsInKeyboardToWriteANiceMessage { get; set; }

    private static void OnViewManagerSendToWorldUi(string value)
    {
        if (!IsInKeyboardToWriteANiceMessage) return;
        IsInKeyboardToWriteANiceMessage = false;
        Comms_Manager.SendLocalTextMessage(value);
        Comms_Manager.SendDirectTextMessage(CVRPlayerManager.Instance.NetworkPlayers[0].Uuid, value);
        Comms_Manager.SendGlobalTextMessage(value);
        MelonLogger.Msg("Sending message: " + value);
    }
    
    private void OnLocalMessageReceived(CVRPlayerEntity player, string message)
    {
        CreateChatBubble(player, message);
        MelonLogger.Msg("OnLocalMessageReceived - Player: " + player?.Username + ", Message: " + message);
    }
    
    private void OnDirectMessageReceived(CVRPlayerEntity player, string message)
    {
        CreateChatBubble(player, message);
        MelonLogger.Msg("OnDirectMessageReceived - Player: " + player?.Username + ", Message: " + message);
    }
    
    private void OnGlobalMessageReceived(CVRPlayerEntity player, string message)
    {
        CreateChatBubble(player, message);
        MelonLogger.Msg("OnGlobalMessageReceived - Player: " + player?.Username + ", Message: " + message);
    }

    private void CreateChatBubble(CVRPlayerEntity player, string message)
    {
        if (player == null || player.PuppetMaster == null)
            return;
        
        // Calculate duration based on message length
        float baseDuration = EntryBubbleDuration.Value;
        float charDuration = EntryDurationPerChar.Value * message.Length;
        float totalDuration = Mathf.Clamp(baseDuration + charDuration, baseDuration, 15f);
        
        ChatBubble bubble = GetBubbleFromPool();
        if (bubble == null) return;
        
        bubble.Player = player;
        bubble.Message = message;
        bubble.Duration = totalDuration;
        bubble.Timer = 0f;
    }
    
    #endregion Game Events
}