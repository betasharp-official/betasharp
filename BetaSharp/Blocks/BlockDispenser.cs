using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockDispenser : BlockWithEntity
{
    private static readonly ThreadLocal<JavaRandom> s_random = new(() => new JavaRandom());

    public BlockDispenser(int id) : base(id, Material.Stone)
    {
        textureId = 45;
    }

    public override int getTickRate()
    {
        return 4;
    }

    public override int getDroppedItemId(int blockMeta)
    {
        return Dispenser.id;
    }

    public override void onPlaced(OnPlacedEvent @event)
    {
        base.onPlaced(@event);
        if (@event.Placer != null)
        {
            int direction = MathHelper.Floor(@event.Placer.yaw * 4.0F / 360.0F + 0.5D) & 3;
            int meta = direction switch
            {
                0 => 2,
                1 => 5,
                2 => 3,
                3 => 4,
                _ => 2
            };
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, meta);
            if (!@event.World.IsRemote)
            {
                @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, meta);
            }
        }
        else
        {
            updateDirection(@event);
        }
    }

    private void updateDirection(OnPlacedEvent @event)
    {
        if (@event.World.IsRemote) return;

        var reader = @event.World.Reader;
        int x = @event.X, y = @event.Y, z = @event.Z;

        bool isNorthOpaque = BlocksOpaque[reader.GetBlockId(x, y, z - 1)];
        bool isSouthOpaque = BlocksOpaque[reader.GetBlockId(x, y, z + 1)];
        bool isWestOpaque = BlocksOpaque[reader.GetBlockId(x - 1, y, z)];
        bool isEastOpaque = BlocksOpaque[reader.GetBlockId(x + 1, y, z)];

        byte direction = 3;
        if (isNorthOpaque && !isSouthOpaque)
        {
            direction = 3;
        }
        else if (isSouthOpaque && !isNorthOpaque)
        {
            direction = 2;
        }

        if (isWestOpaque && !isEastOpaque)
        {
            direction = 5;
        }
        else if (isEastOpaque && !isWestOpaque)
        {
            direction = 4;
        }

        @event.World.Writer.SetBlockMeta(x, y, z, direction);
    }

    public override int getTextureId(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        if (side is 1 or 0)
        {
            return textureId + 17;
        }

        int meta = iBlockReader.GetBlockMeta(x, y, z);
        return side != meta ? textureId : textureId + 1;
    }

    public override int getTexture(int side) => side == 1 ? textureId + 17 : side == 0 ? textureId + 17 : side == 3 ? textureId + 1 : textureId;

    public override bool onUse(OnUseEvent @event)
    {
        if (@event.World.IsRemote)
        {
            return true;
        }

        BlockEntityDispenser? dispenser = @event.World.Entities.GetBlockEntity<BlockEntityDispenser>(@event.X, @event.Y, @event.Z);
        if (dispenser != null)
        {
            @event.Player.openDispenserScreen(dispenser);
        }

        return true;
    }

    private void dispense(OnTickEvent @event)
    {
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        int dirX = 0;
        int dirZ = 0;

        switch (meta)
        {
            case 3:
                dirZ = 1;
                break;
            case 2:
                dirZ = -1;
                break;
            case 5:
                dirX = 1;
                break;
            default:
                dirX = -1;
                break;
        }

        BlockEntityDispenser? dispenser = @event.World.Entities.GetBlockEntity<BlockEntityDispenser>(@event.X, @event.Y, @event.Z);
        if (dispenser == null)
        {
            return;
        }

        ItemStack? itemStack = dispenser.getItemToDispose();
        double spawnX = @event.X + dirX * 0.6D + 0.5D;
        double spawnY = @event.Y + 0.5D;
        double spawnZ = @event.Z + dirZ * 0.6D + 0.5D;

        if (itemStack == null)
        {
            @event.World.Broadcaster.WorldEvent(1001, @event.X, @event.Y, @event.Z, 0);
            return;
        }

        if (itemStack.itemId == Item.ARROW.id)
        {
            EntityArrow arrow = new(@event.World, spawnX, spawnY, spawnZ);
            arrow.setArrowHeading(dirX, 0.1F, dirZ, 1.1F, 6.0F);
            arrow.doesArrowBelongToPlayer = true;
            @event.World.Entities.SpawnEntity(arrow);
            @event.World.Broadcaster.WorldEvent(1002, @event.X, @event.Y, @event.Z, 0);
        }
        else if (itemStack.itemId == Item.Egg.id)
        {
            EntityEgg egg = new(@event.World, spawnX, spawnY, spawnZ);
            egg.setHeading(dirX, 0.1F, dirZ, 1.1F, 6.0F);
            @event.World.Entities.SpawnEntity(egg);
            @event.World.Broadcaster.WorldEvent(1002, @event.X, @event.Y, @event.Z, 0);
        }
        else if (itemStack.itemId == Item.Snowball.id)
        {
            EntitySnowball snowball = new(@event.World, spawnX, spawnY, spawnZ);
            snowball.setHeading(dirX, 0.1F, dirZ, 1.1F, 6.0F);
            @event.World.Entities.SpawnEntity(snowball);
            @event.World.Broadcaster.WorldEvent(1002, @event.X, @event.Y, @event.Z, 0);
        }
        else
        {
            EntityItem item = new(@event.World, spawnX, spawnY - 0.3D, spawnZ, itemStack);
            double randomVelocity = Random.Shared.NextDouble() * 0.1D + 0.2D;
            item.velocityX = dirX * randomVelocity;
            item.velocityY = 0.2F;
            item.velocityZ = dirZ * randomVelocity;

            item.velocityX += @event.World.Random.NextGaussian() * 0.0075D * 6.0D;
            item.velocityY += @event.World.Random.NextGaussian() * 0.0075D * 6.0D;
            item.velocityZ += @event.World.Random.NextGaussian() * 0.0075D * 6.0D;

            @event.World.Entities.SpawnEntity(item);
            @event.World.Broadcaster.WorldEvent(1000, @event.X, @event.Y, @event.Z, 0);
        }

        @event.World.Broadcaster.WorldEvent(2000, @event.X, @event.Y, @event.Z, dirX + 1 + (dirZ + 1) * 3);
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        if (@event.BlockId > 0 && Blocks[@event.BlockId].canEmitRedstonePower())
        {
            bool isPowered = @event.World.Redstone.IsPowered(@event.X, @event.Y, @event.Z) || @event.World.Redstone.IsPowered(@event.X, @event.Y + 1, @event.Z);
            if (isPowered)
            {
                @event.World.TickScheduler.ScheduleBlockUpdate(@event.X, @event.Y, @event.Z, id, getTickRate());
            }
        }
    }

    public override void onTick(OnTickEvent @event)
    {
        if (@event.World.Redstone.IsPowered(@event.X, @event.Y, @event.Z) || @event.World.Redstone.IsPowered(@event.X, @event.Y + 1, @event.Z))
        {
            dispense(@event);
        }
    }

    public override BlockEntity getBlockEntity() => new BlockEntityDispenser();

    public override void onBreak(OnBreakEvent @event)
    {
        BlockEntityDispenser? dispenser = @event.World.Entities.GetBlockEntity<BlockEntityDispenser>(@event.X, @event.Y, @event.Z);

        if (dispenser != null)
        {
            JavaRandom random = s_random.Value!;

            for (int slotIndex = 0; slotIndex < dispenser.size(); ++slotIndex)
            {
                ItemStack? stack = dispenser.getStack(slotIndex);
                if (stack == null) continue;

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
                    EntityItem entityItem = new(@event.World, @event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ, new ItemStack(stack.itemId, amount, stack.getDamage()));
                    float floatVar = 0.05F;

                    entityItem.velocityX = (float)random.NextGaussian() * floatVar;
                    entityItem.velocityY = (float)random.NextGaussian() * floatVar + 0.2F;
                    entityItem.velocityZ = (float)random.NextGaussian() * floatVar;

                    @event.World.Entities.SpawnEntity(entityItem);
                }
            }
        }

        base.onBreak(@event);
    }
}
