using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockTNT : Block
{
    public BlockTNT(int id, int textureId) : base(id, textureId, Material.Tnt)
    {
    }

    public override int getTexture(int side)
    {
        return side == 0 ? textureId + 2 : (side == 1 ? textureId + 1 : textureId);
    }

    public override void onPlaced(World world, int x, int y, int z)
    {
        base.onPlaced(world, x, y, z);
        if (world.isPowered(x, y, z))
        {
            onMetadataChange(world, x, y, z, 1);
            world.setBlock(x, y, z, 0);
        }

    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        if (id > 0 && Block.Blocks[id].canEmitRedstonePower() && world.isPowered(x, y, z))
        {
            onMetadataChange(world, x, y, z, 1);
            world.setBlock(x, y, z, 0);
        }

    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return 0;
    }

    public override void onDestroyedByExplosion(World world, int x, int y, int z)
    {
        EntityTNTPrimed entityTNTPrimed = new EntityTNTPrimed(world, (double)((float)x + 0.5F), (double)((float)y + 0.5F), (double)((float)z + 0.5F));
        entityTNTPrimed.fuse = world.random.NextInt(entityTNTPrimed.fuse / 4) + entityTNTPrimed.fuse / 8;
        world.Entities.SpawnEntity(entityTNTPrimed);
    }

    public override void onMetadataChange(World world, int x, int y, int z, int meta)
    {
        if (!world.IsRemote)
        {
            if ((meta & 1) == 0)
            {
                dropStack(world, x, y, z, new ItemStack(Block.TNT.id, 1, 0));
            }
            else
            {
                EntityTNTPrimed entityTNTPrimed = new EntityTNTPrimed(world, (double)((float)x + 0.5F), (double)((float)y + 0.5F), (double)((float)z + 0.5F));
                world.Entities.SpawnEntity(entityTNTPrimed);
                world.playSound(entityTNTPrimed, "random.fuse", 1.0F, 1.0F);
            }

        }
    }

    public override void onBlockBreakStart(World world, int x, int y, int z, EntityPlayer player)
    {
        if (player.getHand() != null && player.getHand().itemId == Item.FlintAndSteel.id)
        {
            world.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, 1);
        }

        base.onBlockBreakStart(world, x, y, z, player);
    }

    public override bool onUse(World world, int x, int y, int z, EntityPlayer player)
    {
        return base.onUse(world, x, y, z, player);
    }
}
