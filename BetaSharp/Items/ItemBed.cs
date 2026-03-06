using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Items;

internal class ItemBed : Item
{

    public ItemBed(int id) : base(id)
    {
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, World world, int x, int y, int z, int meta)
    {
        if (meta != 1)
        {
            return false;
        }
        else
        {
            ++y;
            BlockBed blockBed = (BlockBed)Block.Bed;
            int direction = MathHelper.Floor((double)(entityPlayer.yaw * 4.0F / 360.0F) + 0.5D) & 3;
            sbyte headOffsetX = 0;
            sbyte headOffsetZ = 0;
            if (direction == 0)
            {
                headOffsetZ = 1;
            }

            if (direction == 1)
            {
                headOffsetX = -1;
            }

            if (direction == 2)
            {
                headOffsetZ = -1;
            }

            if (direction == 3)
            {
                headOffsetX = 1;
            }

            if (world.IsAir(x, y, z) && world.IsAir(x + headOffsetX, y, z + headOffsetZ) && world.ShouldSuffocate(x, y - 1, z) && world.ShouldSuffocate(x + headOffsetX, y - 1, z + headOffsetZ))
            {
                world.SetBlock(x, y, z, blockBed.id, direction);
                world.SetBlock(x + headOffsetX, y, z + headOffsetZ, blockBed.id, direction + 8);
                --itemStack.count;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
