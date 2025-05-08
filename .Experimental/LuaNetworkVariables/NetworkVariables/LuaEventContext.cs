using ABI_RC.Core.Networking;
using MoonSharp.Interpreter;
using ABI_RC.Core.Player;

namespace NAK.LuaNetworkVariables;

public struct LuaEventContext
{
    private string SenderId { get; set; }
    public string SenderName { get; private set; }
    private DateTime LastInvokeTime { get; set; }
    private double TimeSinceLastInvoke { get; set; }
    private bool IsLocal { get; set; }

    public static LuaEventContext Create(bool isLocal, string senderId, DateTime lastInvokeTime)
    {
        var playerName = isLocal ? AuthManager.Username : CVRPlayerManager.Instance.TryGetPlayerName(senderId);
                            
        return new LuaEventContext
        {
            SenderId = senderId,
            SenderName = playerName ?? "Unknown",
            LastInvokeTime = lastInvokeTime,
            TimeSinceLastInvoke = (DateTime.Now - lastInvokeTime).TotalSeconds,
            IsLocal = isLocal
        };
    }

    public Table ToLuaTable(Script script)
    {
        Table table = new(script)
        {
            ["SenderId"] = SenderId,
            ["SenderName"] = SenderName,
            ["LastInvokeTime"] = LastInvokeTime.ToUniversalTime().ToString("O"),
            ["TimeSinceLastInvoke"] = TimeSinceLastInvoke,
            ["IsLocal"] = IsLocal
        };
        return table;
    }
}