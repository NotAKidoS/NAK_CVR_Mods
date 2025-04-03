using MoonSharp.Interpreter;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;

namespace NAK.LuaNetVars;

public struct LuaEventContext
{
    private string SenderId { get; set; }
    public string SenderName { get; private set; }
    private DateTime LastInvokeTime { get; set; }
    private double TimeSinceLastInvoke { get; set; }
    private bool IsLocal { get; set; }

    public static LuaEventContext Create(string senderId, DateTime lastInvokeTime)
    {
        var playerName = CVRPlayerManager.Instance.TryGetPlayerName(senderId);
                            
        return new LuaEventContext
        {
            SenderId = senderId,
            SenderName = playerName ?? "Unknown",
            LastInvokeTime = lastInvokeTime,
            TimeSinceLastInvoke = (DateTime.Now - lastInvokeTime).TotalSeconds,
            IsLocal = senderId == MetaPort.Instance.ownerId
        };
    }

    public Table ToLuaTable(Script script)
    {
        Table table = new(script)
        {
            ["senderId"] = SenderId,
            ["senderName"] = SenderName,
            ["lastInvokeTime"] = LastInvokeTime.ToUniversalTime().ToString("O"),
            ["timeSinceLastInvoke"] = TimeSinceLastInvoke,
            ["isLocal"] = IsLocal
        };
        return table;
    }
}