using BetaSharp.NBT;

namespace BetaSharp.Worlds.Storage;

public abstract class PersistentState
{
    public readonly string Id;
    private bool _dirty;

    public PersistentState(string id) => Id = id;

    public abstract void ReadNBT(NBTTagCompound nbt);

    public abstract void WriteNBT(NBTTagCompound nbt);

    public void MarkDirty() => SetDirty(true);

    public void SetDirty(bool dirty) => _dirty = dirty;

    public bool IsDirty() => _dirty;
}
