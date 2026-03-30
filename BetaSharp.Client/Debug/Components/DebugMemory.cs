using System.ComponentModel;
using BetaSharp.NBT;

namespace BetaSharp.Client.Debug.Components;

[DisplayName("Memory")]
[Description("Shows memory/GC info.")]
public class DebugMemory : DebugComponent
{
    public DebugMemory() { }

    [DisplayName("Show Memory")]
    [Description("Whether to show used memory and max memory.")]
    public bool ShowMem { get; set; } = true;

    [DisplayName("Show Allocated")]
    [Description("Whether to show allocated heap memory.")]
    public bool ShowAllocated { get; set; } = true;

    public override IEnumerable<DebugRowData> GetRows(DebugContext ctx)
    {
        long maxMem = ctx.GCMonitor.MaxMemoryBytes;
        long usedMem = ctx.GCMonitor.UsedMemoryBytes;
        long heapMem = ctx.GCMonitor.UsedHeapBytes;

        if (ShowMem) yield return new DebugRowData($"Mem: {FormatPercentage(usedMem, maxMem)} {FormatMegabytes(usedMem)}/{FormatMegabytes(maxMem)}MB");
        if (ShowAllocated) yield return new DebugRowData($"Allocated: {FormatPercentage(heapMem, maxMem)} {FormatMegabytes(heapMem)}MB");
    }

    public override DebugComponent Duplicate()
    {
        return new DebugMemory()
        {
            Right = Right,

            ShowMem = ShowMem,
            ShowAllocated = ShowAllocated
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

    public override void writeNBT(NBTTagCompound nbt)
    {
        nbt.SetBoolean("ShowMem", ShowMem);
        nbt.SetBoolean("ShowAllocated", ShowAllocated);
    }

    public override void readNBT(NBTTagCompound nbt)
    {
        ShowMem = nbt.GetBoolean("ShowMem");
        ShowAllocated = nbt.GetBoolean("ShowAllocated");
    }
}
