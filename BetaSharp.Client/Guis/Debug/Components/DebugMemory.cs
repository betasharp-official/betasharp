using System.ComponentModel;

namespace BetaSharp.Client.Guis.Debug.Components;


[DisplayName("Memory")]
[Description("Shows memory/GC info.")]
public class DebugMemory : DebugComponent
{

    public DebugMemory() { }

    public override void Draw(DebugContext ctx)
    {
        long maxMem = ctx.GCMonitor.MaxMemoryBytes;
        long usedMem = ctx.GCMonitor.UsedMemoryBytes;
        long heapMem = ctx.GCMonitor.UsedHeapBytes;

        ctx.String($"Mem: {FormatPercentage(usedMem, maxMem)} {FormatMegabytes(usedMem)}/{FormatMegabytes(maxMem)}MB");
        ctx.String($"Allocated: {FormatPercentage(heapMem, maxMem)} {FormatMegabytes(heapMem)}MB");
    }

    public override DebugComponent Duplicate()
    {
        return new DebugMemory()
        {
            Right = Right
        };
    }
    private static string FormatMegabytes(long bytes)
    {
        return bytes <= 0L ? "N/A" : $"{bytes / 1024L / 1024L}";
    }

    private static string FormatPercentage(long value, long total)
    {
        return total > 0L ? $"{value * 100L / total}%" : "N/A";
    }

}
