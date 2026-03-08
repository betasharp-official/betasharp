using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Items;

internal class ItemSign : Item
{

    public ItemSign(int id) : base(id)
    {
        maxCount = 1;
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, World world, int x, int y, int z, int meta)
    {
        if (meta == 0)
        {
            return false;
        }
        else if (!world.getMaterial(x, y, z).IsSolid)
        {
            return false;
        }
        else
        {
            if (meta == 1)
            {
                ++y;
            }

            if (meta == 2)
            {
                --z;
            }

            if (meta == 3)
            {
                ++z;
            }

            if (meta == 4)
            {
                --x;
            }

            if (meta == 5)
            {
                ++x;
            }

            if (!Block.Sign.canPlaceAt(world.BlocksView, x, y, z))
            {
                return false;
            }
            else
            {
                if (meta == 1)
                {
                    world.setBlock(x, y, z, Block.Sign.id, MathHelper.Floor((double)((entityPlayer.yaw + 180.0F) * 16.0F / 360.0F) + 0.5D) & 15);
                }
                else
                {
                    world.setBlock(x, y, z, Block.WallSign.id, meta);
                }

                --itemStack.count;
                BlockEntitySign blockEntitySign = (BlockEntitySign)world.getBlockEntity(x, y, z);
                if (blockEntitySign != null)
                {
                    entityPlayer.openEditSignScreen(blockEntitySign);
                }

                return true;
            }
        }
    }
}
