using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

//NOTE: CHESTS DON'T ROTATE BASED ON PLAYER ORIENTATION, THIS IS VANILLA BEHAVIOR, NOT A BUG
internal class BlockChest : BlockWithEntity
{
    private const float DropSpread = 0.05F;
    private static readonly JavaRandom s_random = new();

    public BlockChest(int id) : base(id, Material.Wood) => TextureId = 26;

    public override int GetTextureId(IBlockReader iBlockReader, int x, int y, int z, Side side)
    {
        if (side is Side.Up or Side.Down) return TextureId - 1;

        int blockNorth = iBlockReader.GetBlockId(x, y, z - 1);
        int blockSouth = iBlockReader.GetBlockId(x, y, z + 1);
        int blockWest = iBlockReader.GetBlockId(x - 1, y, z);
        int blockEast = iBlockReader.GetBlockId(x + 1, y, z);
        int textureOffset;
        int cornerBlock1;
        int cornerBlock2;
        Side facingSide;
        if (blockNorth != id && blockSouth != id)
        {
            if (blockWest != id && blockEast != id)
            {
                Side facing = Side.South;
                if (BlocksOpaque[blockNorth] && !BlocksOpaque[blockSouth])
                {
                    facing = Side.South;
                }

                if (BlocksOpaque[blockSouth] && !BlocksOpaque[blockNorth])
                {
                    facing = Side.North;
                }

                if (BlocksOpaque[blockWest] && !BlocksOpaque[blockEast])
                {
                    facing = Side.East;
                }

                if (BlocksOpaque[blockEast] && !BlocksOpaque[blockWest])
                {
                    facing = Side.West;
                }

                return side == facing ? TextureId + 1 : TextureId;
            }

            if (side is Side.West or Side.East)
            {
                return TextureId;
            }

            textureOffset = 0;
            if (blockWest == id)
            {
                textureOffset = -1;
            }

            cornerBlock1 = iBlockReader.GetBlockId(blockWest == id ? x - 1 : x + 1, y, z - 1);
            cornerBlock2 = iBlockReader.GetBlockId(blockWest == id ? x - 1 : x + 1, y, z + 1);
            if (side == Side.South)
            {
                textureOffset = -1 - textureOffset;
            }

            facingSide = Side.South;
            if ((BlocksOpaque[blockNorth] || BlocksOpaque[cornerBlock1]) && !BlocksOpaque[blockSouth] && !BlocksOpaque[cornerBlock2])
            {
                facingSide = Side.South;
            }

            if ((BlocksOpaque[blockSouth] || BlocksOpaque[cornerBlock2]) && !BlocksOpaque[blockNorth] && !BlocksOpaque[cornerBlock1])
            {
                facingSide = Side.North;
            }

            return (side == facingSide ? TextureId + 16 : TextureId + 32) + textureOffset;
        }

        if (side is Side.North or Side.South)
        {
            return TextureId;
        }

        textureOffset = 0;
        if (blockNorth == id)
        {
            textureOffset = -1;
        }

        cornerBlock1 = iBlockReader.GetBlockId(x - 1, y, blockNorth == id ? z - 1 : z + 1);
        cornerBlock2 = iBlockReader.GetBlockId(x + 1, y, blockNorth == id ? z - 1 : z + 1);
        if (side == Side.West)
        {
            textureOffset = -1 - textureOffset;
        }

        facingSide = Side.East;
        if ((BlocksOpaque[blockWest] || BlocksOpaque[cornerBlock1]) && !BlocksOpaque[blockEast] && !BlocksOpaque[cornerBlock2])
        {
            facingSide = Side.East;
        }

        if ((BlocksOpaque[blockEast] || BlocksOpaque[cornerBlock2]) && !BlocksOpaque[blockWest] && !BlocksOpaque[cornerBlock1])
        {
            facingSide = Side.West;
        }

        return (side == facingSide ? TextureId + 16 : TextureId + 32) + textureOffset;
    }

    public override int GetTexture(Side side)
    {
        return side switch
        {
            Side.Up or Side.Down => TextureId - 1,
            Side.South => TextureId + 1,
            _ => TextureId
        };
    }

    public override bool canPlaceAt(CanPlaceAtContext context)
    {
        int adjacentChestCount = 0;
        if (context.World.Reader.GetBlockId(context.X - 1, context.Y, context.Z) == id)
        {
            ++adjacentChestCount;
        }

        if (context.World.Reader.GetBlockId(context.X + 1, context.Y, context.Z) == id)
        {
            ++adjacentChestCount;
        }

        if (context.World.Reader.GetBlockId(context.X, context.Y, context.Z - 1) == id)
        {
            ++adjacentChestCount;
        }

        if (context.World.Reader.GetBlockId(context.X, context.Y, context.Z + 1) == id)
        {
            ++adjacentChestCount;
        }

        return adjacentChestCount > 1 ? false : hasNeighbor(context) ? false : hasNeighbor(context) ? false : hasNeighbor(context) ? false : !hasNeighbor(context);
    }

    private bool hasNeighbor(CanPlaceAtContext evt) => evt.World.Reader.GetBlockId(evt.X, evt.Y, evt.Z) != id ? false :
        evt.World.Reader.GetBlockId(evt.X - 1, evt.Y, evt.Z) == id ? true :
        evt.World.Reader.GetBlockId(evt.X + 1, evt.Y, evt.Z) == id ? true :
        evt.World.Reader.GetBlockId(evt.X, evt.Y, evt.Z - 1) == id ? true : evt.World.Reader.GetBlockId(evt.X, evt.Y, evt.Z + 1) == id;

    public override void onBreak(OnBreakEvent @event)
    {
        BlockEntityChest? chest = @event.World.Entities.GetBlockEntity<BlockEntityChest>(@event.X, @event.Y, @event.Z);

        if (chest == null)
        {
            return;
        }

        for (int slot = 0; slot < chest.size(); ++slot)
        {
            ItemStack? stack = chest.getStack(slot);
            if (stack == null) continue;

            float offsetX = s_random.NextFloat() * 0.8F + 0.1F;
            float offsetY = s_random.NextFloat() * 0.8F + 0.1F;
            float offsetZ = s_random.NextFloat() * 0.8F + 0.1F;

            while (stack.count > 0)
            {
                int amount = s_random.NextInt(21) + 10;
                if (amount > stack.count)
                {
                    amount = stack.count;
                }

                stack.count -= amount;
                EntityItem entityItem = new(@event.World, @event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ, new ItemStack(stack.itemId, amount, stack.getDamage()));

                entityItem.velocityX = s_random.NextGaussian() * DropSpread;
                entityItem.velocityY = s_random.NextGaussian() * DropSpread + 0.2F;
                entityItem.velocityZ = s_random.NextGaussian() * DropSpread;
                @event.World.Entities.SpawnEntity(entityItem);
            }
        }

        base.onBreak(@event);
    }

    public override bool onUse(OnUseEvent @event)
    {
        IInventory? chestInventory = @event.World.Entities.GetBlockEntity<BlockEntityChest>(@event.X, @event.Y, @event.Z);
        if (@event.World.Reader.ShouldSuffocate(@event.X, @event.Y + 1, @event.Z))
        {
            return true;
        }

        if (@event.World.Reader.GetBlockId(@event.X - 1, @event.Y, @event.Z) == id && @event.World.Reader.ShouldSuffocate(@event.X - 1, @event.Y + 1, @event.Z))
        {
            return true;
        }

        if (@event.World.Reader.GetBlockId(@event.X + 1, @event.Y, @event.Z) == id && @event.World.Reader.ShouldSuffocate(@event.X + 1, @event.Y + 1, @event.Z))
        {
            return true;
        }

        if (@event.World.Reader.GetBlockId(@event.X, @event.Y, @event.Z - 1) == id && @event.World.Reader.ShouldSuffocate(@event.X, @event.Y + 1, @event.Z - 1))
        {
            return true;
        }

        if (@event.World.Reader.GetBlockId(@event.X, @event.Y, @event.Z + 1) == id && @event.World.Reader.ShouldSuffocate(@event.X, @event.Y + 1, @event.Z + 1))
        {
            return true;
        }

        if (@event.World.Reader.GetBlockId(@event.X - 1, @event.Y, @event.Z) == id)
        {
            chestInventory = new InventoryLargeChest("Large chest", @event.World.Entities.GetBlockEntity<BlockEntityChest>(@event.X - 1, @event.Y, @event.Z), chestInventory);
        }

        if (@event.World.Reader.GetBlockId(@event.X + 1, @event.Y, @event.Z) == id)
        {
            chestInventory = new InventoryLargeChest("Large chest", chestInventory, @event.World.Entities.GetBlockEntity<BlockEntityChest>(@event.X + 1, @event.Y, @event.Z));
        }

        if (@event.World.Reader.GetBlockId(@event.X, @event.Y, @event.Z - 1) == id)
        {
            chestInventory = new InventoryLargeChest("Large chest", @event.World.Entities.GetBlockEntity<BlockEntityChest>(@event.X, @event.Y, @event.Z - 1), chestInventory);
        }

        if (@event.World.Reader.GetBlockId(@event.X, @event.Y, @event.Z + 1) == id)
        {
            chestInventory = new InventoryLargeChest("Large chest", chestInventory, @event.World.Entities.GetBlockEntity<BlockEntityChest>(@event.X, @event.Y, @event.Z + 1));
        }

        if (@event.World.IsRemote)
        {
            return true;
        }

        @event.Player.openChestScreen(chestInventory);
        return true;
    }

    public override BlockEntity getBlockEntity() => new BlockEntityChest();
}
