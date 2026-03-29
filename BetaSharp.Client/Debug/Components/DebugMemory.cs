using System.ComponentModel;

namespace BetaSharp.Client.Debug.Components;

[DisplayName("Memory")]
[Description("Shows memory/GC info.")]
public class DebugMemory : DebugComponent
{
    public DebugMemory() { }

    public override IEnumerable<DebugRowData> GetRows(DebugContext ctx)
    {
        long maxMem = ctx.GCMonitor.MaxMemoryBytes;
        long usedMem = ctx.GCMonitor.UsedMemoryBytes;
        long heapMem = ctx.GCMonitor.UsedHeapBytes;

        yield return new DebugRowData($"Mem: {FormatPercentage(usedMem, maxMem)} {FormatMegabytes(usedMem)}/{FormatMegabytes(maxMem)}MB");
        yield return new DebugRowData($"Allocated: {FormatPercentage(heapMem, maxMem)} {FormatMegabytes(heapMem)}MB");
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
