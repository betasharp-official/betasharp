using BetaSharp.Blocks.Materials;
using BetaSharp.NBT;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks.Entities;

/// <summary>
///     Block entity for the noteblock, playing notes.
/// </summary>
internal class BlockEntityNote : BlockEntity
{
    public override BlockEntityType Type => Music;

    public sbyte Note { get; set; }

    public bool Powered { get; set; } = false;

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        nbt.SetByte("note", Note);
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        Note = nbt.GetByte("note");
        if (Note < 0)
        {
            Note = 0;
        }

        if (Note > 24)
        {
            Note = 24;
        }
    }

    public void CycleNote()
    {
        Note = (sbyte)((Note + 1) % 25);
        MarkDirty();
    }

    public void PlayNote(IWorldContext level, int x, int y, int z)
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

            level.Broadcaster.PlayNote(x, y, z, instrument, Note);
        }
    }
}
