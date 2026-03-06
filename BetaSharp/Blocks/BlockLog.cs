using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockLog : Block
{
    public BlockLog(int id) : base(id, Material.Wood)
    {
        textureId = 20;
    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return 1;
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return Block.Log.id;
    }

    public override void afterBreak(World world, EntityPlayer player, int x, int y, int z, int meta)
    {
        base.afterBreak(world, player, x, y, z, meta);
    }

    public override void onBreak(World world, int x, int y, int z)
    {
        sbyte searchRadius = 4;
        int regionExtent = searchRadius + 1;
        if (world.IsRegionLoaded(x - regionExtent, y - regionExtent, z - regionExtent, x + regionExtent, y + regionExtent, z + regionExtent))
        {
            for (int offsetX = -searchRadius; offsetX <= searchRadius; ++offsetX)
            {
                for (int offsetY = -searchRadius; offsetY <= searchRadius; ++offsetY)
                {
                    for (int offsetZ = -searchRadius; offsetZ <= searchRadius; ++offsetZ)
                    {
                        int neighborBlockId = world.GetBlockId(x + offsetX, y + offsetY, z + offsetZ);
                        if (neighborBlockId == Block.Leaves.id)
                        {
                            int leavesMeta = world.GetBlockMeta(x + offsetX, y + offsetY, z + offsetZ);
                            if ((leavesMeta & 8) == 0)
                            {
                                world.SetBlockMetaWithoutNotifyingNeighbors(x + offsetX, y + offsetY, z + offsetZ, leavesMeta | 8);
                            }
                        }
                    }
                }
            }
        }

    }

    public override int getTexture(int side, int meta)
    {
        return side == 1 ? 21 : (side == 0 ? 21 : (meta == 1 ? 116 : (meta == 2 ? 117 : 20)));
    }

    protected override int getDroppedItemMeta(int blockMeta)
    {
        return blockMeta;
    }
}
