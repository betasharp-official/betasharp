using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockLockedChest : Block
{

    public BlockLockedChest(int id) : base(id, Material.Wood)
    {
        textureId = 26;
    }

    public override int getTextureId(IBlockAccess iBlockAccess, int x, int y, int z, int side)
    {
        if (side == 1)
        {
            return textureId - 1;
        }
        else if (side == 0)
        {
            return textureId - 1;
        }
        else
        {
            int blockIdNorth = iBlockAccess.getBlockId(x, y, z - 1);
            int blockIdSouth = iBlockAccess.getBlockId(x, y, z + 1);
            int blockIdWest = iBlockAccess.getBlockId(x - 1, y, z);
            int blockIdEast = iBlockAccess.getBlockId(x + 1, y, z);
            sbyte orientationMetadata = 3;
            if (Block.BlocksOpaque[blockIdNorth] && !Block.BlocksOpaque[blockIdSouth])
            {
                orientationMetadata = 3;
            }

            if (Block.BlocksOpaque[blockIdSouth] && !Block.BlocksOpaque[blockIdNorth])
            {
                orientationMetadata = 2;
            }

            if (Block.BlocksOpaque[blockIdWest] && !Block.BlocksOpaque[blockIdEast])
            {
                orientationMetadata = 5;
            }

            if (Block.BlocksOpaque[blockIdEast] && !Block.BlocksOpaque[blockIdWest])
            {
                orientationMetadata = 4;
            }

            return side == orientationMetadata ? textureId + 1 : textureId;
        }
    }

    public override int getTexture(int side)
    {
        return side == 1 ? textureId - 1 : (side == 0 ? textureId - 1 : (side == 3 ? textureId + 1 : textureId));
    }

    public override bool canPlaceAt(World world, int x, int y, int z)
    {
        return true;
    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        world.setBlock(x, y, z, 0);
    }
}
