using System.Diagnostics;

namespace BetaSharp.Client;

public class Timer(float tps)
{
    public float DeltaTime { get; private set; }
    public float ticksPerSecond = tps;
    private double lastHRTime;
    public int elapsedTicks;
    public float renderPartialTicks;
    public float timerSpeed = 1.0F;
    public float elapsedPartialTicks;
    private long lastSyncSysClock = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private long lastSyncHRClock = Stopwatch.GetTimestamp();
    private long accumulatedSysTime;
    private double timeSyncAdjustment = 1.0D;

    public void UpdateTimer()
    {
        long currentSysTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long sysDelta = currentSysTime - lastSyncSysClock;
        long currentHighResTime = Stopwatch.GetTimestamp();
        double currentTimeSeconds = (double)currentHighResTime / Stopwatch.Frequency;
        if (sysDelta > 1000L)
        {
            lastHRTime = currentTimeSeconds;
        }
        else if (sysDelta < 0L)
        {
            lastHRTime = currentTimeSeconds;
        }
        else
        {
            accumulatedSysTime += sysDelta;
            if (accumulatedSysTime > 1000L)
            {
                long highResDeltaTicks = currentHighResTime - lastSyncHRClock;
                double highResDelta = highResDeltaTicks / Stopwatch.Frequency;
                double adjustmentRatio = accumulatedSysTime / (highResDelta * 1000.0);
                timeSyncAdjustment += (adjustmentRatio - timeSyncAdjustment) * (double)0.2F;
                lastSyncHRClock = currentHighResTime;
                accumulatedSysTime = 0L;
            }

            if (accumulatedSysTime < 0L)
            {
                lastSyncHRClock = currentHighResTime;
            }
        }

        lastSyncSysClock = currentSysTime;
        double frameDelta = (currentTimeSeconds - lastHRTime) * timeSyncAdjustment;
        DeltaTime = (float)Math.Clamp(frameDelta, 1.0f / 1000.0f, 1.0f);
        lastHRTime = currentTimeSeconds;

        if (frameDelta < 0.0D || frameDelta > 1.0D)
            frameDelta = 0.0D;

        elapsedPartialTicks = (float)((double)elapsedPartialTicks + frameDelta * (double)timerSpeed * (double)ticksPerSecond);
        elapsedTicks = (int)elapsedPartialTicks;
        elapsedPartialTicks -= (float)elapsedTicks;
        if (elapsedTicks > 10)
        {
            elapsedTicks = 10;
        }

        renderPartialTicks = elapsedPartialTicks;
    }
}
