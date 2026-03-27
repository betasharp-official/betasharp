using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemSign : Item
{

    public ItemSign(int id) : base(id)
    {
        maxCount = 1;
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        if (meta == 0)
        {
            return false;
        }
        else if (!world.Reader.GetMaterial(x, y, z).IsSolid)
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

            if (!Block.Sign.canPlaceAt(new CanPlaceAtContext(world, 0, x, y, z)))
            {
                return false;
            }
            else
            {
                if (meta == 1)
                {
                    world.Writer.SetBlock(x, y, z, Block.Sign.id, MathHelper.Floor((double)((entityPlayer.yaw + 180.0F) * 16.0F / 360.0F) + 0.5D) & 15);
                }
                else
                {
                    world.Writer.SetBlock(x, y, z, Block.WallSign.id, meta);
                }

                itemStack.ConsumeItem(entityPlayer);
                BlockEntitySign? blockEntitySign = world.Entities.GetBlockEntity<BlockEntitySign>(x, y, z);
                if (blockEntitySign != null)
                {
                    entityPlayer.openEditSignScreen(blockEntitySign);
                }

                return true;
            }
        }
    }
}
