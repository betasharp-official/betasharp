using BetaSharp.NBT;

namespace BetaSharp.Blocks.Entities;

internal class BlockEntityRecordPlayer : BlockEntity
{
    public override BlockEntityType Type => BlockEntity.RecordPlayer;
    public int recordId;

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
        recordId = nbt.GetInteger("Record");
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
        if (recordId > 0)
        {
            nbt.SetInteger("Record", recordId);
        }

    }
}
