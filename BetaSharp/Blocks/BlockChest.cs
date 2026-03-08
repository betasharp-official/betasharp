using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

//NOTE: CHESTS DON'T ROTATE BASED ON PLAYER ORIENTATION, THIS IS VANILLA BEHAVIOR, NOT A BUG
internal class BlockChest : BlockWithEntity
{
    private readonly JavaRandom random = new();

    public BlockChest(int id) : base(id, Material.Wood) => textureId = 26;

    public override int getTextureId(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        if (side == 1)
        {
            return textureId - 1;
        }

        if (side == 0)
        {
            return textureId - 1;
        }

        int blockNorth = iBlockReader.GetBlockId(x, y, z - 1);
        int blockSouth = iBlockReader.GetBlockId(x, y, z + 1);
        int blockWest = iBlockReader.GetBlockId(x - 1, y, z);
        int blockEast = iBlockReader.GetBlockId(x + 1, y, z);
        int textureOffset;
        int cornerBlock1;
        int cornerBlock2;
        sbyte facingSide;
        if (blockNorth != id && blockSouth != id)
        {
            if (blockWest != id && blockEast != id)
            {
                sbyte facing = 3;
                if (BlocksOpaque[blockNorth] && !BlocksOpaque[blockSouth])
                {
                    facing = 3;
                }

                if (BlocksOpaque[blockSouth] && !BlocksOpaque[blockNorth])
                {
                    facing = 2;
                }

                if (BlocksOpaque[blockWest] && !BlocksOpaque[blockEast])
                {
                    facing = 5;
                }

                if (BlocksOpaque[blockEast] && !BlocksOpaque[blockWest])
                {
                    facing = 4;
                }

                return side == facing ? textureId + 1 : textureId;
            }

            if (side != 4 && side != 5)
            {
                textureOffset = 0;
                if (blockWest == id)
                {
                    textureOffset = -1;
                }

                cornerBlock1 = iBlockReader.GetBlockId(blockWest == id ? x - 1 : x + 1, y, z - 1);
                cornerBlock2 = iBlockReader.GetBlockId(blockWest == id ? x - 1 : x + 1, y, z + 1);
                if (side == 3)
                {
                    textureOffset = -1 - textureOffset;
                }

                facingSide = 3;
                if ((BlocksOpaque[blockNorth] || BlocksOpaque[cornerBlock1]) && !BlocksOpaque[blockSouth] && !BlocksOpaque[cornerBlock2])
                {
                    facingSide = 3;
                }

                if ((BlocksOpaque[blockSouth] || BlocksOpaque[cornerBlock2]) && !BlocksOpaque[blockNorth] && !BlocksOpaque[cornerBlock1])
                {
                    facingSide = 2;
                }

                return (side == facingSide ? textureId + 16 : textureId + 32) + textureOffset;
            }

            return textureId;
        }

        if (side != 2 && side != 3)
        {
            textureOffset = 0;
            if (blockNorth == id)
            {
                textureOffset = -1;
            }

            cornerBlock1 = iBlockReader.GetBlockId(x - 1, y, blockNorth == id ? z - 1 : z + 1);
            cornerBlock2 = iBlockReader.GetBlockId(x + 1, y, blockNorth == id ? z - 1 : z + 1);
            if (side == 4)
            {
                textureOffset = -1 - textureOffset;
            }

            facingSide = 5;
            if ((BlocksOpaque[blockWest] || BlocksOpaque[cornerBlock1]) && !BlocksOpaque[blockEast] && !BlocksOpaque[cornerBlock2])
            {
                facingSide = 5;
            }

            if ((BlocksOpaque[blockEast] || BlocksOpaque[cornerBlock2]) && !BlocksOpaque[blockWest] && !BlocksOpaque[cornerBlock1])
            {
                facingSide = 4;
            }

            return (side == facingSide ? textureId + 16 : textureId + 32) + textureOffset;
        }

        return textureId;
    }

    public override int getTexture(int side) => side == 1 ? textureId - 1 : side == 0 ? textureId - 1 : side == 3 ? textureId + 1 : textureId;

    public override bool canPlaceAt(OnPlacedEvt ctx)
    {
        int adjacentChestCount = 0;
        if (ctx.WorldRead.GetBlockId(ctx.X - 1, ctx.Y, ctx.Z) == id)
        {
            ++adjacentChestCount;
        }

        if (ctx.WorldRead.GetBlockId(ctx.X + 1, ctx.Y, ctx.Z) == id)
        {
            ++adjacentChestCount;
        }

        if (ctx.WorldRead.GetBlockId(ctx.X, ctx.Y, ctx.Z - 1) == id)
        {
            ++adjacentChestCount;
        }

        if (ctx.WorldRead.GetBlockId(ctx.X, ctx.Y, ctx.Z + 1) == id)
        {
            ++adjacentChestCount;
        }

        return adjacentChestCount > 1 ? false : hasNeighbor(ctx) ? false : hasNeighbor(ctx) ? false : hasNeighbor(ctx) ? false : !hasNeighbor(ctx);
    }

    private bool hasNeighbor(OnPlacedEvt ctx) => ctx.WorldRead.GetBlockId(ctx.X, ctx.Y, ctx.Z) != id ? false :
        ctx.WorldRead.GetBlockId(ctx.X - 1, ctx.Y, ctx.Z) == id ? true :
        ctx.WorldRead.GetBlockId(ctx.X + 1, ctx.Y, ctx.Z) == id ? true :
        ctx.WorldRead.GetBlockId(ctx.X, ctx.Y, ctx.Z - 1) == id ? true : ctx.WorldRead.GetBlockId(ctx.X, ctx.Y, ctx.Z + 1) == id;

    public override void onBreak(OnBreakEvt ctx)
    {
        BlockEntityChest? chest = (BlockEntityChest?)ctx.WorldRead.GetBlockEntity(ctx.X, ctx.Y, ctx.Z);

        for (int slot = 0; slot < chest!.size(); ++slot)
        {
            ItemStack stack = chest!.getStack(slot);
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
                    EntityItem entityItem = new(ctx.World, ctx.X + offsetX, ctx.Y + offsetY, ctx.Z + offsetZ, new ItemStack(stack.itemId, amount, stack.getDamage()));
                    float var13 = 0.05F;
                    entityItem.velocityX = random.NextGaussian() * var13;
                    entityItem.velocityY = random.NextGaussian() * var13 + 0.2F;
                    entityItem.velocityZ = random.NextGaussian() * var13;
                    ctx.Entities.SpawnEntity(entityItem);
                }
            }
        }

        base.onBreak(ctx);
    }

    public override bool onUse(OnUseEvt ctx)
    {
        IInventory chestInventory = (BlockEntityChest)ctx.WorldRead.GetBlockEntity(ctx.X, ctx.Y, ctx.Z);
        if (ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y + 1, ctx.Z))
        {
            return true;
        }

        if (ctx.WorldRead.GetBlockId(ctx.X - 1, ctx.Y, ctx.Z) == id && ctx.WorldRead.ShouldSuffocate(ctx.X - 1, ctx.Y + 1, ctx.Z))
        {
            return true;
        }

        if (ctx.WorldRead.GetBlockId(ctx.X + 1, ctx.Y, ctx.Z) == id && ctx.WorldRead.ShouldSuffocate(ctx.X + 1, ctx.Y + 1, ctx.Z))
        {
            return true;
        }

        if (ctx.WorldRead.GetBlockId(ctx.X, ctx.Y, ctx.Z - 1) == id && ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y + 1, ctx.Z - 1))
        {
            return true;
        }

        if (ctx.WorldRead.GetBlockId(ctx.X, ctx.Y, ctx.Z + 1) == id && ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y + 1, ctx.Z + 1))
        {
            return true;
        }

        if (ctx.WorldRead.GetBlockId(ctx.X - 1, ctx.Y, ctx.Z) == id)
        {
            chestInventory = new InventoryLargeChest("Large chest", (BlockEntityChest)ctx.WorldRead.GetBlockEntity(ctx.X - 1, ctx.Y, ctx.Z), chestInventory);
        }

        if (ctx.WorldRead.GetBlockId(ctx.X + 1, ctx.Y, ctx.Z) == id)
        {
            chestInventory = new InventoryLargeChest("Large chest", chestInventory, (BlockEntityChest)ctx.WorldRead.GetBlockEntity(ctx.X + 1, ctx.Y, ctx.Z));
        }

        if (ctx.WorldRead.GetBlockId(ctx.X, ctx.Y, ctx.Z - 1) == id)
        {
            chestInventory = new InventoryLargeChest("Large chest", (BlockEntityChest)ctx.WorldRead.GetBlockEntity(ctx.X, ctx.Y, ctx.Z - 1), chestInventory);
        }

        if (ctx.WorldRead.GetBlockId(ctx.X, ctx.Y, ctx.Z + 1) == id)
        {
            chestInventory = new InventoryLargeChest("Large chest", chestInventory, (BlockEntityChest)ctx.WorldRead.GetBlockEntity(ctx.X, ctx.Y, ctx.Z + 1));
        }

        if (ctx.IsRemote)
        {
            return true;
        }

        ctx.Player.openChestScreen(chestInventory);
        return true;
    }

    protected override BlockEntity getBlockEntity() => new BlockEntityChest();
}
