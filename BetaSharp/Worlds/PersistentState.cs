using BetaSharp.NBT;

namespace BetaSharp.Worlds;

public abstract class PersistentState(string id)
{
    public string Id { get; } = id;
    public bool Dirty { get; set; }

    public abstract void ReadNBT(NBTTagCompound from);

    public abstract void WriteNBT(NBTTagCompound to);
}
