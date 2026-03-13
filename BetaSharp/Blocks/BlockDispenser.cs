using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
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

    public override void onPlaced(OnPlacedEvt evt)
    {
        base.onPlaced(evt);

        // If a player/entity placed it, use their yaw. Otherwise, use neighbor logic.
        if (evt.Placer != null)
        {
            int direction = MathHelper.Floor(evt.Placer.yaw * 4.0F / 360.0F + 0.5D) & 3;
            int meta = 0;

            if (direction == 0)
            {
                meta = 2;
            }
            else if (direction == 1)
            {
                meta = 5;
            }
            else if (direction == 2)
            {
                meta = 3;
            }
            else if (direction == 3)
            {
                meta = 4;
            }

            evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, meta);
        }
        else
        {
            updateDirection(evt.Level.Reader, evt.Level.BlockWriter, evt.Level.IsRemote, evt.X, evt.Y, evt.Z);
        }
    }

    private void updateDirection(WorldReader worldRead, WorldWriter worldWrite, bool isRemote, int x, int y, int z)
    {
        if (!isRemote)
        {
            int blockNorth = worldRead.GetBlockId(x, y, z - 1);
            int blockSouth = worldRead.GetBlockId(x, y, z + 1);
            int blockWest = worldRead.GetBlockId(x - 1, y, z);
            int blockEast = worldRead.GetBlockId(x + 1, y, z);

            sbyte direction = 3;
            if (BlocksOpaque[blockNorth] && !BlocksOpaque[blockSouth])
            {
                direction = 3;
            }

            if (BlocksOpaque[blockSouth] && !BlocksOpaque[blockNorth])
            {
                direction = 2;
            }

            if (BlocksOpaque[blockWest] && !BlocksOpaque[blockEast])
            {
                direction = 5;
            }

            if (BlocksOpaque[blockEast] && !BlocksOpaque[blockWest])
            {
                direction = 4;
            }

            worldWrite.SetBlockMeta(x, y, z, direction);
        }
    }

    public override int getTextureId(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        if (side == 1 || side == 0)
        {
            return textureId + 17;
        }

        int meta = iBlockReader.GetBlockMeta(x, y, z);
        return side != meta ? textureId : textureId + 1;
    }

    public override int getTexture(int side) => side == 1 ? textureId + 17 : side == 0 ? textureId + 17 : side == 3 ? textureId + 1 : textureId;

    public override bool onUse(OnUseEvt evt)
    {
        if (evt.Level.IsRemote)
        {
            return true;
        }

        BlockEntityDispenser? dispenser = (BlockEntityDispenser?)evt.Level.Reader.GetBlockEntity(evt.X, evt.Y, evt.Z);
        if (dispenser != null)
        {
            evt.Player.openDispenserScreen(dispenser);
        }

        return true;
    }

    private void dispense(OnTickEvt evt)
    {
        int meta = evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z);
        int dirX = 0;
        int dirZ = 0;

        if (meta == 3)
        {
            dirZ = 1;
        }
        else if (meta == 2)
        {
            dirZ = -1;
        }
        else if (meta == 5)
        {
            dirX = 1;
        }
        else
        {
            dirX = -1;
        }

        BlockEntityDispenser? dispenser = (BlockEntityDispenser?)evt.Level.Reader.GetBlockEntity(evt.X, evt.Y, evt.Z);
        if (dispenser == null)
        {
            return;
        }

        ItemStack itemStack = dispenser.getItemToDispose();
        double spawnX = evt.X + dirX * 0.6D + 0.5D;
        double spawnY = evt.Y + 0.5D;
        double spawnZ = evt.Z + dirZ * 0.6D + 0.5D;

        if (itemStack == null)
        {
            evt.Level.Broadcaster.WorldEvent(1001, evt.X, evt.Y, evt.Z, 0);
        }
        else
        {
            if (itemStack.itemId == Item.ARROW.id)
            {
                EntityArrow arrow = new(evt.Level, spawnX, spawnY, spawnZ);
                arrow.setArrowHeading(dirX, 0.1F, dirZ, 1.1F, 6.0F);
                arrow.doesArrowBelongToPlayer = true;
                evt.Level.Entities.SpawnEntity(arrow);
                evt.Level.Broadcaster.WorldEvent(1002, evt.X, evt.Y, evt.Z, 0);
            }
            else if (itemStack.itemId == Item.Egg.id)
            {
                EntityEgg egg = new(evt.Level, spawnX, spawnY, spawnZ);
                egg.setEggHeading(dirX, 0.1F, dirZ, 1.1F, 6.0F);
                evt.Level.Entities.SpawnEntity(egg);
                evt.Level.Broadcaster.WorldEvent(1002, evt.X, evt.Y, evt.Z, 0);
            }
            else if (itemStack.itemId == Item.Snowball.id)
            {
                EntitySnowball snowball = new(evt.Level, spawnX, spawnY, spawnZ);
                snowball.setSnowballHeading(dirX, 0.1F, dirZ, 1.1F, 6.0F);
                evt.Level.Entities.SpawnEntity(snowball);
                evt.Level.Broadcaster.WorldEvent(1002, evt.X, evt.Y, evt.Z, 0);
            }
            else
            {
                EntityItem item = new(evt.Level, spawnX, spawnY - 0.3D, spawnZ, itemStack);
                double randomVelocity = Random.Shared.NextDouble() * 0.1D + 0.2D;
                item.velocityX = dirX * randomVelocity;
                item.velocityY = 0.2F;
                item.velocityZ = dirZ * randomVelocity;

                // EntityItem velocity usually takes doubles in newer Beta engines
                item.velocityX += evt.Level.random.NextGaussian() * 0.0075D * 6.0D;
                item.velocityY += evt.Level.random.NextGaussian() * 0.0075D * 6.0D;
                item.velocityZ += evt.Level.random.NextGaussian() * 0.0075D * 6.0D;

                evt.Level.Entities.SpawnEntity(item);
                evt.Level.Broadcaster.WorldEvent(1000, evt.X, evt.Y, evt.Z, 0);
            }

            evt.Level.Broadcaster.WorldEvent(2000, evt.X, evt.Y, evt.Z, dirX + 1 + (dirZ + 1) * 3);
        }
    }

    public override void neighborUpdate(OnTickEvt evt)
    {
        if (evt.BlockId > 0 && Blocks[evt.BlockId].canEmitRedstonePower())
        {
            bool isPowered = evt.Level.Redstone.IsPowered(evt.X, evt.Y, evt.Z) || evt.Level.Redstone.IsPowered(evt.X, evt.Y + 1, evt.Z);
            if (isPowered)
            {
                evt.Level.TickScheduler.ScheduleBlockUpdate(evt.X, evt.Y, evt.Z, id, getTickRate());
            }
        }
    }

    public override void onTick(OnTickEvt evt)
    {
        if (evt.Level.Redstone.IsPowered(evt.X, evt.Y, evt.Z) || evt.Level.Redstone.IsPowered(evt.X, evt.Y + 1, evt.Z))
        {
            dispense(evt);
        }
    }

    protected override BlockEntity getBlockEntity() => new BlockEntityDispenser();

    public override void onBreak(OnBreakEvt evt)
    {
        BlockEntityDispenser? dispenser = (BlockEntityDispenser?)evt.Level.Reader.GetBlockEntity(evt.X, evt.Y, evt.Z);

        if (dispenser != null)
        {
            JavaRandom random = s_random.Value!;

            for (int slotIndex = 0; slotIndex < dispenser.size(); ++slotIndex)
            {
                ItemStack stack = dispenser.getStack(slotIndex);
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
                        EntityItem entityItem = new(evt.Level, evt.X + offsetX, evt.Y + offsetY, evt.Z + offsetZ, new ItemStack(stack.itemId, amount, stack.getDamage()));
                        float floatVar = 0.05F;

                        entityItem.velocityX = (float)random.NextGaussian() * floatVar;
                        entityItem.velocityY = (float)random.NextGaussian() * floatVar + 0.2F;
                        entityItem.velocityZ = (float)random.NextGaussian() * floatVar;

                        evt.Level.Entities.SpawnEntity(entityItem);
                    }
                }
            }
        }

        base.onBreak(evt);
    }
}
