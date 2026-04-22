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

    public BlockChest(int id) : base(id, Material.Wood) => TextureId = BlockTextures.ChestSingleSide;

    public override int GetTextureId(IBlockReader iBlockReader, int x, int y, int z, Side side)
    {
        if (side is Side.Up or Side.Down) return BlockTextures.ChestTopBottom;

        int blockNorth = iBlockReader.GetBlockId(x, y, z - 1);
        int blockSouth = iBlockReader.GetBlockId(x, y, z + 1);
        int blockWest = iBlockReader.GetBlockId(x - 1, y, z);
        int blockEast = iBlockReader.GetBlockId(x + 1, y, z);

        bool isDoubleEw = blockWest == id || blockEast == id;
        bool isDoubleNs = blockNorth == id || blockSouth == id;

        if (!isDoubleNs && !isDoubleEw)
        {
            Side facing = Side.South;
            if (BlocksOpaque[blockNorth] && !BlocksOpaque[blockSouth]) facing = Side.South;
            if (BlocksOpaque[blockSouth] && !BlocksOpaque[blockNorth]) facing = Side.North;
            if (BlocksOpaque[blockWest] && !BlocksOpaque[blockEast]) facing = Side.East;
            if (BlocksOpaque[blockEast] && !BlocksOpaque[blockWest]) facing = Side.West;

            return side == facing ? BlockTextures.ChestSingleFront : BlockTextures.ChestSingleSide;
        }

        if (isDoubleEw)
        {
            if (side is Side.West or Side.East) return BlockTextures.ChestSingleSide;

            bool isWestPartner = blockWest == id;
            int corner1 = iBlockReader.GetBlockId(isWestPartner ? x - 1 : x + 1, y, z - 1);
            int corner2 = iBlockReader.GetBlockId(isWestPartner ? x - 1 : x + 1, y, z + 1);

            Side facing = Side.South;
            if ((BlocksOpaque[blockNorth] || BlocksOpaque[corner1]) && !BlocksOpaque[blockSouth] && !BlocksOpaque[corner2]) facing = Side.South;
            if ((BlocksOpaque[blockSouth] || BlocksOpaque[corner2]) && !BlocksOpaque[blockNorth] && !BlocksOpaque[corner1]) facing = Side.North;

            bool isRightHalf = facing == Side.South ? isWestPartner : !isWestPartner;

            return GetDoubleChestTexture(side, facing, isRightHalf);
        }

        if (isDoubleNs)
        {
            if (side is Side.North or Side.South) return BlockTextures.ChestSingleSide;

            bool isNorthPartner = blockNorth == id;
            int corner1 = iBlockReader.GetBlockId(x - 1, y, isNorthPartner ? z - 1 : z + 1);
            int corner2 = iBlockReader.GetBlockId(x + 1, y, isNorthPartner ? z - 1 : z + 1);

            Side facing = Side.East;
            if ((BlocksOpaque[blockWest] || BlocksOpaque[corner1]) && !BlocksOpaque[blockEast] && !BlocksOpaque[corner2]) facing = Side.East;
            if ((BlocksOpaque[blockEast] || BlocksOpaque[corner2]) && !BlocksOpaque[blockWest] && !BlocksOpaque[corner1]) facing = Side.West;

            bool isRightHalf = facing == Side.East ? isNorthPartner : !isNorthPartner;

            return GetDoubleChestTexture(side, facing, isRightHalf);
        }

        return BlockTextures.ChestSingleSide;
    }

    private static int GetDoubleChestTexture(Side renderSide, Side frontFacing, bool isRightHalf)
    {
        bool isFront = renderSide == frontFacing;
        if (isFront) return isRightHalf ? BlockTextures.ChestDoubleFrontRight : BlockTextures.ChestDoubleFrontLeft;
        return isRightHalf ? BlockTextures.ChestDoubleBackLeft : BlockTextures.ChestDoubleBackRight;
    }

    public override int GetTexture(Side side)
    {
        return side switch
        {
            Side.Up or Side.Down => BlockTextures.ChestSingleSide,
            Side.South => BlockTextures.ChestSingleFront,
            _ => BlockTextures.ChestSingleSide
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

        for (int slot = 0; slot < chest.Size; ++slot)
        {
            ItemStack? stack = chest.GetStack(slot);
            if (stack == null) continue;

            float offsetX = s_random.NextFloat() * 0.8F + 0.1F;
            float offsetY = s_random.NextFloat() * 0.8F + 0.1F;
            float offsetZ = s_random.NextFloat() * 0.8F + 0.1F;

            while (stack.Count > 0)
            {
                int amount = s_random.NextInt(21) + 10;
                if (amount > stack.Count)
                {
                    amount = stack.Count;
                }

                stack.Count -= amount;
                EntityItem entityItem = new(@event.World, @event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ, new ItemStack(stack.ItemId, amount, stack.getDamage()));

                entityItem.VelocityX = s_random.NextGaussian() * DropSpread;
                entityItem.VelocityY = s_random.NextGaussian() * DropSpread + 0.2F;
                entityItem.VelocityZ = s_random.NextGaussian() * DropSpread;
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
