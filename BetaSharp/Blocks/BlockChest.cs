using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

//NOTE: CHESTS DON'T ROTATE BASED ON PLAYER ORIENTATION, THIS IS VANILLA BEHAVIOR, NOT A BUG
internal class BlockChest : BlockWithEntity
{
    private JavaRandom random = new();

    public BlockChest(int id) : base(id, Material.Wood)
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
            int blockNorth = iBlockAccess.getBlockId(x, y, z - 1);
            int blockSouth = iBlockAccess.getBlockId(x, y, z + 1);
            int blockWest = iBlockAccess.getBlockId(x - 1, y, z);
            int blockEast = iBlockAccess.getBlockId(x + 1, y, z);
            int textureOffset;
            int cornerBlock1;
            int cornerBlock2;
            sbyte facingSide;
            if (blockNorth != id && blockSouth != id)
            {
                if (blockWest != id && blockEast != id)
                {
                    sbyte facing = 3;
                    if (Block.BlocksOpaque[blockNorth] && !Block.BlocksOpaque[blockSouth])
                    {
                        facing = 3;
                    }

                    if (Block.BlocksOpaque[blockSouth] && !Block.BlocksOpaque[blockNorth])
                    {
                        facing = 2;
                    }

                    if (Block.BlocksOpaque[blockWest] && !Block.BlocksOpaque[blockEast])
                    {
                        facing = 5;
                    }

                    if (Block.BlocksOpaque[blockEast] && !Block.BlocksOpaque[blockWest])
                    {
                        facing = 4;
                    }

                    return side == facing ? textureId + 1 : textureId;
                }
                else if (side != 4 && side != 5)
                {
                    textureOffset = 0;
                    if (blockWest == id)
                    {
                        textureOffset = -1;
                    }

                    cornerBlock1 = iBlockAccess.getBlockId(blockWest == id ? x - 1 : x + 1, y, z - 1);
                    cornerBlock2 = iBlockAccess.getBlockId(blockWest == id ? x - 1 : x + 1, y, z + 1);
                    if (side == 3)
                    {
                        textureOffset = -1 - textureOffset;
                    }

                    facingSide = 3;
                    if ((Block.BlocksOpaque[blockNorth] || Block.BlocksOpaque[cornerBlock1]) && !Block.BlocksOpaque[blockSouth] && !Block.BlocksOpaque[cornerBlock2])
                    {
                        facingSide = 3;
                    }

                    if ((Block.BlocksOpaque[blockSouth] || Block.BlocksOpaque[cornerBlock2]) && !Block.BlocksOpaque[blockNorth] && !Block.BlocksOpaque[cornerBlock1])
                    {
                        facingSide = 2;
                    }

                    return (side == facingSide ? textureId + 16 : textureId + 32) + textureOffset;
                }
                else
                {
                    return textureId;
                }
            }
            else if (side != 2 && side != 3)
            {
                textureOffset = 0;
                if (blockNorth == id)
                {
                    textureOffset = -1;
                }

                cornerBlock1 = iBlockAccess.getBlockId(x - 1, y, blockNorth == id ? z - 1 : z + 1);
                cornerBlock2 = iBlockAccess.getBlockId(x + 1, y, blockNorth == id ? z - 1 : z + 1);
                if (side == 4)
                {
                    textureOffset = -1 - textureOffset;
                }

                facingSide = 5;
                if ((Block.BlocksOpaque[blockWest] || Block.BlocksOpaque[cornerBlock1]) && !Block.BlocksOpaque[blockEast] && !Block.BlocksOpaque[cornerBlock2])
                {
                    facingSide = 5;
                }

                if ((Block.BlocksOpaque[blockEast] || Block.BlocksOpaque[cornerBlock2]) && !Block.BlocksOpaque[blockWest] && !Block.BlocksOpaque[cornerBlock1])
                {
                    facingSide = 4;
                }

                return (side == facingSide ? textureId + 16 : textureId + 32) + textureOffset;
            }
            else
            {
                return textureId;
            }
        }
    }

    public override int getTexture(int side)
    {
        return side == 1 ? textureId - 1 : (side == 0 ? textureId - 1 : (side == 3 ? textureId + 1 : textureId));
    }

    public override bool canPlaceAt(World world, int x, int y, int z)
    {
        int adjacentChestCount = 0;
        if (world.getBlockId(x - 1, y, z) == id)
        {
            ++adjacentChestCount;
        }

        if (world.getBlockId(x + 1, y, z) == id)
        {
            ++adjacentChestCount;
        }

        if (world.getBlockId(x, y, z - 1) == id)
        {
            ++adjacentChestCount;
        }

        if (world.getBlockId(x, y, z + 1) == id)
        {
            ++adjacentChestCount;
        }

        return adjacentChestCount > 1 ? false : (hasNeighbor(world, x - 1, y, z) ? false : (hasNeighbor(world, x + 1, y, z) ? false : (hasNeighbor(world, x, y, z - 1) ? false : !hasNeighbor(world, x, y, z + 1))));
    }

    private bool hasNeighbor(World world, int x, int y, int z)
    {
        return world.getBlockId(x, y, z) != id ? false : (world.getBlockId(x - 1, y, z) == id ? true : (world.getBlockId(x + 1, y, z) == id ? true : (world.getBlockId(x, y, z - 1) == id ? true : world.getBlockId(x, y, z + 1) == id)));
    }

    public override void onBreak(World world, int x, int y, int z)
    {
        BlockEntityChest chest = (BlockEntityChest)world.getBlockEntity(x, y, z);

        for (int slot = 0; slot < chest.size(); ++slot)
        {
            ItemStack stack = chest.getStack(slot);
            if (stack != null)
            {
                float offsetX = random.NextFloat() * 0.8F + 0.1F;
                float offsetY = random.NextFloat() * 0.8F + 0.1F;
                float offsetZ = random.NextFloat() * 0.8F + 0.1F;

                while (stack.count > 0)
                {
                    int amount = random.NextInt(21) + 10;
                    if (amount > stack.count)
                    {
                        amount = stack.count;
                    }

                    stack.count -= amount;
                    EntityItem entityItem = new EntityItem(world, (double)((float)x + offsetX), (double)((float)y + offsetY), (double)((float)z + offsetZ), new ItemStack(stack.itemId, amount, stack.getDamage()));
                    float var13 = 0.05F;
                    entityItem.velocityX = (double)((float)random.NextGaussian() * var13);
                    entityItem.velocityY = (double)((float)random.NextGaussian() * var13 + 0.2F);
                    entityItem.velocityZ = (double)((float)random.NextGaussian() * var13);
                    world.SpawnEntity(entityItem);
                }
            }
        }

        base.onBreak(world, x, y, z);
    }

    public override bool onUse(World world, int x, int y, int z, EntityPlayer player)
    {
        IInventory chestInventory = (BlockEntityChest)world.getBlockEntity(x, y, z);
        if (world.shouldSuffocate(x, y + 1, z))
        {
            return true;
        }
        else if (world.getBlockId(x - 1, y, z) == id && world.shouldSuffocate(x - 1, y + 1, z))
        {
            return true;
        }
        else if (world.getBlockId(x + 1, y, z) == id && world.shouldSuffocate(x + 1, y + 1, z))
        {
            return true;
        }
        else if (world.getBlockId(x, y, z - 1) == id && world.shouldSuffocate(x, y + 1, z - 1))
        {
            return true;
        }
        else if (world.getBlockId(x, y, z + 1) == id && world.shouldSuffocate(x, y + 1, z + 1))
        {
            return true;
        }
        else
        {
            if (world.getBlockId(x - 1, y, z) == id)
            {
                chestInventory = new InventoryLargeChest("Large chest", (BlockEntityChest)world.getBlockEntity(x - 1, y, z), (IInventory)chestInventory);
            }

            if (world.getBlockId(x + 1, y, z) == id)
            {
                chestInventory = new InventoryLargeChest("Large chest", (IInventory)chestInventory, (BlockEntityChest)world.getBlockEntity(x + 1, y, z));
            }

            if (world.getBlockId(x, y, z - 1) == id)
            {
                chestInventory = new InventoryLargeChest("Large chest", (BlockEntityChest)world.getBlockEntity(x, y, z - 1), (IInventory)chestInventory);
            }

            if (world.getBlockId(x, y, z + 1) == id)
            {
                chestInventory = new InventoryLargeChest("Large chest", (IInventory)chestInventory, (BlockEntityChest)world.getBlockEntity(x, y, z + 1));
            }

            if (world.isRemote)
            {
                return true;
            }
            else
            {
                player.openChestScreen((IInventory)chestInventory);
                return true;
            }
        }
    }

    protected override BlockEntity getBlockEntity()
    {
        return new BlockEntityChest();
    }
}
