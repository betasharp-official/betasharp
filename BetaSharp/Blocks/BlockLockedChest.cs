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
            int var6 = iBlockAccess.getBlockId(x, y, z - 1);
            int var7 = iBlockAccess.getBlockId(x, y, z + 1);
            int var8 = iBlockAccess.getBlockId(x - 1, y, z);
            int var9 = iBlockAccess.getBlockId(x + 1, y, z);
            sbyte var10 = 3;
            if (Block.BlocksOpaque[var6] && !Block.BlocksOpaque[var7])
            {
                var10 = 3;
            }

            if (Block.BlocksOpaque[var7] && !Block.BlocksOpaque[var6])
            {
                var10 = 2;
            }

            if (Block.BlocksOpaque[var8] && !Block.BlocksOpaque[var9])
            {
                var10 = 5;
            }

            if (Block.BlocksOpaque[var9] && !Block.BlocksOpaque[var8])
            {
                var10 = 4;
            }

            return side == var10 ? textureId + 1 : textureId;
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
