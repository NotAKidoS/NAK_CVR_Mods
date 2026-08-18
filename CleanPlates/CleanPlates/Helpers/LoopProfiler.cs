using System.Diagnostics;

namespace NAK.CleanPlates.Helpers;

internal class LoopProfiler
{
    public bool Enabled;

    private readonly string label;
    private readonly bool trackDrawn;
    private readonly Stopwatch watch = new();
    private double totalMs;
    private double peakMs;
    private int frames;
    private int drawn;
    private float reportTime;

    public LoopProfiler(string label, bool trackDrawn)
    {
        this.label = label;
        this.trackDrawn = trackDrawn;
    }

    public void Begin()
    {
        if (Enabled) watch.Restart();
    }

    public void End(int drawnThisFrame, int total)
    {
        if (!Enabled) return;

        watch.Stop();
        double ms = watch.Elapsed.TotalMilliseconds;
        totalMs += ms;
        if (ms > peakMs) peakMs = ms;
        drawn += drawnThisFrame;
        frames++;

        float now = UnityEngine.Time.time;
        if (now < reportTime) return;
        reportTime = now + 5f;

        string tail = trackDrawn
            ? $"drawn {(float)drawn / frames:F1}/{total}"
            : $"count {total}";
        CleanPlatesMod.Logger.Msg(
            $"[{label}] avg {totalMs / frames:F3}ms peak {peakMs:F3}ms {tail}");

        totalMs = 0;
        peakMs = 0;
        drawn = 0;
        frames = 0;
    }
}