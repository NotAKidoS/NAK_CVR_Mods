namespace NAK.VRCFTHeadTracking;

internal static class HeadTrackingData
{
    internal static readonly List<string> ParameterNames = new()
    {
        "v2/Head/Yaw", "v2/Head/Pitch", "v2/Head/Roll",
        "v2/Head/PosX", "v2/Head/PosY", "v2/Head/PosZ"
    };

    private static readonly float[] Values = new float[6];
    private static int _lastReceivedTick;

    internal static float Yaw => Volatile.Read(ref Values[0]);
    internal static float Pitch => Volatile.Read(ref Values[1]);
    internal static float Roll => Volatile.Read(ref Values[2]);
    internal static float PosX => Volatile.Read(ref Values[3]);
    internal static float PosY => Volatile.Read(ref Values[4]);
    internal static float PosZ => Volatile.Read(ref Values[5]);

    internal static bool IsFresh => Environment.TickCount - Volatile.Read(ref _lastReceivedTick) < 1000;

    internal static void TryIngest(string address, object[] arguments)
    {
        if (arguments.Length != 1 || !address.StartsWith("/avatar/parameters/", StringComparison.OrdinalIgnoreCase)) return;
        float value;
        switch (arguments[0])
        {
            case float f: value = f; break;
            case int i: value = i; break;
            default: return;
        }
        int parameterNamesCount = ParameterNames.Count;
        for (int i = 0; i < parameterNamesCount; i++)
        {
            if (!address.EndsWith(ParameterNames[i], StringComparison.OrdinalIgnoreCase)) continue;
            Volatile.Write(ref Values[i], value);
            Volatile.Write(ref _lastReceivedTick, Environment.TickCount);
            return;
        }
    }
}