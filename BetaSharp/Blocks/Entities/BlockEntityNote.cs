using BetaSharp.Blocks.Materials;
using BetaSharp.NBT;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks.Entities;

internal class BlockEntityNote : BlockEntity
{
    public override BlockEntityType Type => BlockEntity.Note;
    public sbyte note;
    public bool powered = false;

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
        nbt.SetByte("note", note);
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
        note = nbt.GetByte("note");
        if (note < 0)
        {
            note = 0;
        }

        if (note > 24)
        {
            note = 24;
        }
    }

    public void cycleNote()
    {
        note = (sbyte)((note + 1) % 25);
        markDirty();
    }

    public void playNote(IWorldContext level, int x, int y, int z)
    {
        if (level.Reader.GetMaterial(x, y + 1, z) == Material.Air)
        {
            Material material = level.Reader.GetMaterial(x, y - 1, z);
            byte instrument = 0;
            if (material == Material.Stone)
            {
                instrument = 1;
            }

            if (material == Material.Sand)
            {
                instrument = 2;
            }

            if (material == Material.Glass)
            {
                instrument = 3;
            }

            if (material == Material.Wood)
            {
                instrument = 4;
            }

            level.Broadcaster.PlayNote(x, y, z, instrument, note);
        }
    }
}
