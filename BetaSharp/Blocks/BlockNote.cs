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

    public override int getTexture(int side) => textureId;

    public override void neighborUpdate(WorldBlockView world, int x, int y, int z, int id)
    {
        if (id > 0 && Blocks[id].canEmitRedstonePower())
        {
            bool isPowered = world.isStrongPowered(x, y, z);
            BlockEntityNote blockEntity = (BlockEntityNote)world.getBlockEntity(x, y, z);
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
        if (world.isRemote)
        {
            return true;
        }

        BlockEntityNote blockEntity = (BlockEntityNote)world.getBlockEntity(x, y, z);
        blockEntity.cycleNote();
        blockEntity.playNote(world, x, y, z);
        return true;
    }

    public override void onBlockBreakStart(World world, int x, int y, int z, EntityPlayer player)
    {
        if (!world.isRemote)
        {
            BlockEntityNote blockEntity = (BlockEntityNote)world.getBlockEntity(x, y, z);
            blockEntity.playNote(world, x, y, z);
        }
    }

    protected override BlockEntity getBlockEntity() => new BlockEntityNote();

    public override void onBlockAction(World world, int x, int y, int z, int data1, int data2)
    {
        float pitch = (float)Math.Pow(2.0D, (data2 - 12) / 12.0D);
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

        world.playSound(x + 0.5D, y + 0.5D, z + 0.5D, "note." + instrumentName, 3.0F, pitch);
        world.addParticle("note", x + 0.5D, y + 1.2D, z + 0.5D, data2 / 24.0D, 0.0D, 0.0D);
    }
}
