using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockJukeBox : BlockWithEntity
{

    public BlockJukeBox(int id, int textureId) : base(id, textureId, Material.Wood)
    {
    }

    public override int getTexture(int side)
    {
        return textureId + (side == 1 ? 1 : 0);
    }

    public override bool onUse(World world, int x, int y, int z, EntityPlayer player)
    {
        if (world.GetBlockMeta(x, y, z) == 0)
        {
            return false;
        }
        else
        {
            tryEjectRecord(world, x, y, z);
            return true;
        }
    }

    public void insertRecord(World world, int x, int y, int z, int id)
    {
        if (!world.IsRemote)
        {
            BlockEntityRecordPlayer jukebox = (BlockEntityRecordPlayer)world.GetBlockEntity(x, y, z);
            jukebox.recordId = id;
            jukebox.markDirty();
            world.setBlockMeta(x, y, z, 1);
        }
    }

    public void tryEjectRecord(World world, int x, int y, int z)
    {
        if (!world.IsRemote)
        {
            BlockEntityRecordPlayer jukebox = (BlockEntityRecordPlayer)world.GetBlockEntity(x, y, z);
            int recordId = jukebox.recordId;
            if (recordId != 0)
            {
                world.WorldEvent(1005, x, y, z, 0);
                world.PlayStreaming((String)null, x, y, z);
                jukebox.recordId = 0;
                jukebox.markDirty();
                world.setBlockMeta(x, y, z, 0);
                float spreadFactor = 0.7F;
                double offsetX = (world.random.NextFloat() * spreadFactor) + (1.0F - spreadFactor) * 0.5D;
                double offsetY = (world.random.NextFloat() * spreadFactor) + (1.0F - spreadFactor) * 0.2D + 0.6D;
                double offsetZ = (world.random.NextFloat() * spreadFactor) + (1.0F - spreadFactor) * 0.5D;
                EntityItem entityItem = new (world, x + offsetX, y + offsetY, z + offsetZ, new ItemStack(recordId, 1, 0));
                entityItem.delayBeforeCanPickup = 10;
                world.Entities.SpawnEntity(entityItem);
            }
        }
    }

    public override void onBreak(World world, int x, int y, int z)
    {
        tryEjectRecord(world, x, y, z);
        base.onBreak(world, x, y, z);
    }

    public override void dropStacks(World world, int x, int y, int z, int meta, float luck)
    {
        if (!world.IsRemote)
        {
            base.dropStacks(world, x, y, z, meta, luck);
        }
    }

    protected override BlockEntity getBlockEntity()
    {
        return new BlockEntityRecordPlayer();
    }
}
