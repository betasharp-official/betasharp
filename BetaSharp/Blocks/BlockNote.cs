using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockNote : BlockWithEntity
{
    public BlockNote(int id) : base(id, 74, Material.Wood)
    {
    }

    public override int getTexture(int side)
    {
        return textureId;
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        if (id > 0 && Block.Blocks[id].canEmitRedstonePower())
        {
            bool isPowered = world.IsStrongPowered(x, y, z);
            BlockEntityNote blockEntity = (BlockEntityNote)world.GetBlockEntity(x, y, z);
            if (blockEntity.powered != isPowered)
            {
                if (isPowered)
                {
                    blockEntity.playNote(world, x, y, z);
                }

                blockEntity.powered = isPowered;
            }
        }

    }

    public override bool onUse(World world, int x, int y, int z, EntityPlayer player)
    {
        if (world.IsRemote)
        {
            return true;
        }
        else
        {
            BlockEntityNote blockEntity = (BlockEntityNote)world.GetBlockEntity(x, y, z);
            blockEntity.cycleNote();
            blockEntity.playNote(world, x, y, z);
            return true;
        }
    }

    public override void onBlockBreakStart(World world, int x, int y, int z, EntityPlayer player)
    {
        if (!world.IsRemote)
        {
            BlockEntityNote blockEntity = (BlockEntityNote)world.GetBlockEntity(x, y, z);
            blockEntity.playNote(world, x, y, z);
        }
    }

    protected override BlockEntity getBlockEntity()
    {
        return new BlockEntityNote();
    }

    public override void onBlockAction(World world, int x, int y, int z, int data1, int data2)
    {
        float pitch = (float)Math.Pow(2.0D, (double)(data2 - 12) / 12.0D);
        string instrumentName = "harp";
        if (data1 == 1)
        {
            instrumentName = "bd";
        }

        if (data1 == 2)
        {
            instrumentName = "snare";
        }

        if (data1 == 3)
        {
            instrumentName = "hat";
        }

        if (data1 == 4)
        {
            instrumentName = "bassattack";
        }

        world.PlaySound((double)x + 0.5D, (double)y + 0.5D, (double)z + 0.5D, "note." + instrumentName, 3.0F, pitch);
        world.AddParticle("note", (double)x + 0.5D, (double)y + 1.2D, (double)z + 0.5D, (double)data2 / 24.0D, 0.0D, 0.0D);
    }
}
