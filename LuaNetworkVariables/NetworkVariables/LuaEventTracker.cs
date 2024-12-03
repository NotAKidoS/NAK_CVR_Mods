namespace NAK.LuaNetVars;

internal class LuaEventTracker
{
    private class EventMetadata
    {
        public DateTime LastInvokeTime { get; set; }
        public Dictionary<string, DateTime> LastInvokeTimePerSender { get; } = new();
    }

    private readonly Dictionary<string, EventMetadata> _eventMetadata = new();

    public DateTime GetLastInvokeTime(string eventName)
    {
        if (!_eventMetadata.TryGetValue(eventName, out var metadata))
        {
            metadata = new EventMetadata();
            _eventMetadata[eventName] = metadata;
        }
        return metadata.LastInvokeTime;
    }

    public DateTime GetLastInvokeTimeForSender(string eventName, string senderId)
    {
        if (!_eventMetadata.TryGetValue(eventName, out var metadata))
        {
            metadata = new EventMetadata();
            _eventMetadata[eventName] = metadata;
        }

        if (!metadata.LastInvokeTimePerSender.TryGetValue(senderId, out DateTime time))
        {
            return DateTime.MinValue;
        }
        return time;
    }

    public void UpdateInvokeTime(string eventName, string senderId)
    {
        if (!_eventMetadata.TryGetValue(eventName, out EventMetadata metadata))
        {
            metadata = new EventMetadata();
            _eventMetadata[eventName] = metadata;
        }

        DateTime now = DateTime.Now;
        metadata.LastInvokeTime = now;
        metadata.LastInvokeTimePerSender[senderId] = now;
    }

    public void Clear()
    {
        _eventMetadata.Clear();
    }

    public void ClearEvent(string eventName)
    {
        _eventMetadata.Remove(eventName);
    }
}