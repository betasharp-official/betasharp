using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockPumpkin : Block
{

    private bool lit;

    public BlockPumpkin(int id, int textureId, bool lit) : base(id, Material.Pumpkin)
    {
        this.textureId = textureId;
        setTickRandomly(true);
        this.lit = lit;
    }

    public override int getTexture(int side, int meta)
    {
        if (side == 1)
        {
            return textureId;
        }
        else if (side == 0)
        {
            return textureId;
        }
        else
        {
            int faceTexture = textureId + 1 + 16;
            if (lit)
            {
                ++faceTexture;
            }

            return meta == 2 && side == 2 ?
                faceTexture : (meta == 3 && side == 5 ? faceTexture : (meta == 0 && side == 3 ? faceTexture :
                    (meta == 1 && side == 4 ? faceTexture : textureId + 16)));
        }
    }

    public override int getTexture(int side)
    {
        return side == 1 ? textureId : (side == 0 ? textureId : (side == 3 ? textureId + 1 + 16 : textureId + 16));
    }

    public override void onPlaced(World world, int x, int y, int z)
    {
        base.onPlaced(world, x, y, z);
    }

    public override bool canPlaceAt(World world, int x, int y, int z)
    {
        int blockId = world.GetBlockId(x, y, z);
        return (blockId == 0 || Block.Blocks[blockId].material.IsReplaceable) && world.ShouldSuffocate(x, y - 1, z);
    }

    public override void onPlaced(World world, int x, int y, int z, EntityLiving placer)
    {
        int direction = MathHelper.Floor((double)(placer.yaw * 4.0F / 360.0F) + 2.5D) & 3;
        world.setBlockMeta(x, y, z, direction);
    }
}
