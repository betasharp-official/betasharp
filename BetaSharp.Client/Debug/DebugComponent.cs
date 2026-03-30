using System.ComponentModel;
using BetaSharp.NBT;

namespace BetaSharp.Client.Debug;

public abstract class DebugComponent
{
    [Description("Whether to show this component on the right side of the debug overlay instead of the left.")]
    public bool Right { get; set; }
    public abstract IEnumerable<DebugRowData> GetRows(DebugContext ctx);
    public abstract DebugComponent Duplicate();

    public void write(NBTTagCompound nbt)
    {
        nbt.SetBoolean("Right", Right);

        writeNBT(nbt);
    }

    public void read(NBTTagCompound nbt)
    {
        Right = nbt.GetBoolean("Right");

        readNBT(nbt);
    }

    public virtual void writeNBT(NBTTagCompound nbt) { }
    public virtual void readNBT(NBTTagCompound nbt) { }
}
