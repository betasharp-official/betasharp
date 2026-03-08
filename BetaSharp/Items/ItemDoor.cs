using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Items;

internal class ItemDoor : Item
{

    private Material doorMaterial;

    public ItemDoor(int id, Material material) : base(id)
    {
        doorMaterial = material;
        maxCount = 1;
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
            Block block;
            if (doorMaterial == Material.Wood)
            {
                block = Block.Door;
            }
            else
            {
                block = Block.IronDoor;
            }

            if (!block.canPlaceAt(world.BlocksView, x, y, z))
            {
                return false;
            }
            else
            {
                int direction = MathHelper.Floor((double)((entityPlayer.yaw + 180.0F) * 4.0F / 360.0F) - 0.5D) & 3;
                sbyte offsetX = 0;
                sbyte offsetZ = 0;
                if (direction == 0)
                {
                    offsetZ = 1;
                }

                if (direction == 1)
                {
                    offsetX = -1;
                }

                if (direction == 2)
                {
                    offsetZ = -1;
                }

                if (direction == 3)
                {
                    offsetX = 1;
                }

                int solidBlocksLeft = (world.shouldSuffocate(x - offsetX, y, z - offsetZ) ? 1 : 0) + (world.shouldSuffocate(x - offsetX, y + 1, z - offsetZ) ? 1 : 0);
                int solidBlocksRight = (world.shouldSuffocate(x + offsetX, y, z + offsetZ) ? 1 : 0) + (world.shouldSuffocate(x + offsetX, y + 1, z + offsetZ) ? 1 : 0);
                bool hasDoorOnLeft = world.getBlockId(x - offsetX, y, z - offsetZ) == block.id || world.getBlockId(x - offsetX, y + 1, z - offsetZ) == block.id;
                bool hasDoorOnRight = world.getBlockId(x + offsetX, y, z + offsetZ) == block.id || world.getBlockId(x + offsetX, y + 1, z + offsetZ) == block.id;
                bool shouldMirror = false;
                if (hasDoorOnLeft && !hasDoorOnRight)
                {
                    shouldMirror = true;
                }
                else if (solidBlocksRight > solidBlocksLeft)
                {
                    shouldMirror = true;
                }

                if (shouldMirror)
                {
                    direction = direction - 1 & 3;
                    direction += 4;
                }


                world.setBlockPaused(x, y, z, block.id, direction);
                world.setBlockPaused(x, y + 1, z, block.id, direction + 8);
                world.notifyNeighbors(x, y, z, block.id);
                world.notifyNeighbors(x, y + 1, z, block.id);
                --itemStack.count;
                return true;
            }
        }
    }
}
