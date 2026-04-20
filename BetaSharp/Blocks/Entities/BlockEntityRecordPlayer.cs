using BetaSharp.NBT;

namespace BetaSharp.Blocks.Entities;

/// <summary>
///     Block entity for a record player, storing the record currently playing.
/// </summary>
internal class BlockEntityRecordPlayer : BlockEntity
{
    /// <summary>
    ///     Item ID of the record currently playing, or 0 if no record is playing.
    /// </summary>
    public int RecordID;

    public override BlockEntityType Type => RecordPlayer;

    public override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        RecordID = nbt.GetInteger("Record");
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        if (RecordID > 0)
        {
            nbt.SetInteger("Record", RecordID);
        }
    }
}
