using System.Collections.Concurrent;
using System.Diagnostics;

namespace NAK.CleanPlates;

internal static class VoiceActivity
{
    private const double WindowSeconds = 0.3;

    private class State
    {
        public long Voice;
        public long Tts;
    }

    private static readonly ConcurrentDictionary<string, State> States = new();

    internal static void OnPacket(string userId, bool isTts)
    {
        State state = States.GetOrAdd(userId, static _ => new State());
        long now = Stopwatch.GetTimestamp();
        state.Voice = now;
        if (isTts) state.Tts = now;
    }

    internal static bool IsSpeaking(string userId)
        => States.TryGetValue(userId, out State state) && IsRecent(state.Voice);

    internal static bool IsPlayingTts(string userId)
        => States.TryGetValue(userId, out State state) && IsRecent(state.Tts);

    private static bool IsRecent(long timestamp)
        => timestamp != 0 && (Stopwatch.GetTimestamp() - timestamp) / (double)Stopwatch.Frequency <= WindowSeconds;
}
