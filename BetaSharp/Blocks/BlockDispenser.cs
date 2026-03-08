using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockDispenser : BlockWithEntity
{
    private static readonly ThreadLocal<JavaRandom> s_random = new(() => new JavaRandom());

    public BlockDispenser(int id) : base(id, Material.Stone) => textureId = 45;

    public override int getTickRate() => 4;

    public override int getDroppedItemId(int blockMeta) => Dispenser.id;

    public override void onPlaced(OnPlacedEvt ctx)
    {
        base.onPlaced(ctx);
        
        // If a player/entity placed it, use their yaw. Otherwise, use neighbor logic.
        if (ctx.Placer != null)
        {
            int direction = MathHelper.Floor(ctx.Placer.yaw * 4.0F / 360.0F + 0.5D) & 3;
            int meta = 0;
            
            if (direction == 0) meta = 2;
            else if (direction == 1) meta = 5;
            else if (direction == 2) meta = 3;
            else if (direction == 3) meta = 4;

            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, meta);
        }
        else
        {
            updateDirection(ctx.WorldRead, ctx.WorldWrite, ctx.IsRemote, ctx.X, ctx.Y, ctx.Z);
        }
    }

    private void updateDirection(WorldBlockView worldRead, WorldBlockWrite worldWrite, bool isRemote, int x, int y, int z)
    {
        if (!isRemote)
        {
            int blockNorth = worldRead.GetBlockId(x, y, z - 1);
            int blockSouth = worldRead.GetBlockId(x, y, z + 1);
            int blockWest = worldRead.GetBlockId(x - 1, y, z);
            int blockEast = worldRead.GetBlockId(x + 1, y, z);
            
            sbyte direction = 3;
            if (BlocksOpaque[blockNorth] && !BlocksOpaque[blockSouth]) direction = 3;
            if (BlocksOpaque[blockSouth] && !BlocksOpaque[blockNorth]) direction = 2;
            if (BlocksOpaque[blockWest] && !BlocksOpaque[blockEast]) direction = 5;
            if (BlocksOpaque[blockEast] && !BlocksOpaque[blockWest]) direction = 4;

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

    public override bool onUse(OnUseEvt ctx)
    {
        if (ctx.IsRemote)
        {
            return true;
        }

        BlockEntityDispenser? dispenser = (BlockEntityDispenser?)ctx.WorldRead.GetBlockEntity(ctx.X, ctx.Y, ctx.Z);
        if (dispenser != null)
        {
            ctx.Player.openDispenserScreen(dispenser);
        }
        return true;
    }

    private void dispense(OnTickEvt ctx)
    {
        int meta = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        int dirX = 0;
        int dirZ = 0;
        
        if (meta == 3) dirZ = 1;
        else if (meta == 2) dirZ = -1;
        else if (meta == 5) dirX = 1;
        else dirX = -1;

        BlockEntityDispenser? dispenser = (BlockEntityDispenser?)ctx.WorldRead.GetBlockEntity(ctx.X, ctx.Y, ctx.Z);
        if (dispenser == null) return;

        ItemStack itemStack = dispenser.getItemToDispose();
        double spawnX = ctx.X + dirX * 0.6D + 0.5D;
        double spawnY = ctx.Y + 0.5D;
        double spawnZ = ctx.Z + dirZ * 0.6D + 0.5D;

        if (itemStack == null)
        {
            ctx.Broadcaster.WorldEvent(1001, ctx.X, ctx.Y, ctx.Z, 0);
        }
        else
        {
            if (itemStack.itemId == Item.ARROW.id)
            {
                // TODO: Implement this
                // EntityArrow arrow = new(ctx.WorldRead, spawnX, spawnY, spawnZ);
                // arrow.setArrowHeading(dirX, 0.1F, dirZ, 1.1F, 6.0F);
                // arrow.doesArrowBelongToPlayer = true;
                // ctx.Entities.SpawnEntity(arrow);
                ctx.Broadcaster.WorldEvent(1002, ctx.X, ctx.Y, ctx.Z, 0);
            }
            else if (itemStack.itemId == Item.Egg.id)
            {
                // TODO: Implement this
                // EntityEgg egg = new(ctx.WorldRead, spawnX, spawnY, spawnZ);
                // egg.setEggHeading(dirX, 0.1F, dirZ, 1.1F, 6.0F);
                // ctx.Entities.SpawnEntity(egg);
                ctx.Broadcaster.WorldEvent(1002, ctx.X, ctx.Y, ctx.Z, 0);
            }
            else if (itemStack.itemId == Item.Snowball.id)
            {
               // TODO: Implement this
               // EntitySnowball snowball = new(ctx.WorldRead, spawnX, spawnY, spawnZ);
               // snowball.setSnowballHeading(dirX, 0.1F, dirZ, 1.1F, 6.0F);
               // ctx.Entities.SpawnEntity(snowball);
                ctx.Broadcaster.WorldEvent(1002, ctx.X, ctx.Y, ctx.Z, 0);
            }
            else
            {
                // TODO: Implement this
                // EntityItem item = new(ctx.WorldRead, spawnX, spawnY - 0.3D, spawnZ, itemStack);
                // double randomVelocity = ctx.Random.NextDouble() * 0.1D + 0.2D;
                // item.velocityX = dirX * randomVelocity;
                // item.velocityY = 0.2F;
                // item.velocityZ = dirZ * randomVelocity;
                // 
                // // EntityItem velocity usually takes doubles in newer Beta engines
                // item.velocityX += ctx.Random.NextGaussian() * 0.0075D * 6.0D;
                // item.velocityY += ctx.Random.NextGaussian() * 0.0075D * 6.0D;
                // item.velocityZ += ctx.Random.NextGaussian() * 0.0075D * 6.0D;

                // ctx.Entities.SpawnEntity(item);
                ctx.Broadcaster.WorldEvent(1000, ctx.X, ctx.Y, ctx.Z, 0);
            }

            ctx.Broadcaster.WorldEvent(2000, ctx.X, ctx.Y, ctx.Z, dirX + 1 + (dirZ + 1) * 3);
        }
    }

    public override void neighborUpdate(OnTickEvt ctx)
    {
        if (ctx.BlockId > 0 && Blocks[ctx.BlockId].canEmitRedstonePower())
        {
            bool isPowered = ctx.Redstone.IsPowered(ctx.X, ctx.Y, ctx.Z) || ctx.Redstone.IsPowered(ctx.X, ctx.Y + 1, ctx.Z);
            if (isPowered)
            {
                // TODO: Implement this
               //  ctx.WorldWrite.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, id, getTickRate());
            }
        }
    }

    public override void onTick(OnTickEvt ctx)
    {
        if (ctx.Redstone.IsPowered(ctx.X, ctx.Y, ctx.Z) || ctx.Redstone.IsPowered(ctx.X, ctx.Y + 1, ctx.Z))
        {
            dispense(ctx);
        }
    }

    protected override BlockEntity getBlockEntity() => new BlockEntityDispenser();

    public override void onBreak(OnBreakEvt ctx)
    {
        BlockEntityDispenser? dispenser = (BlockEntityDispenser?)ctx.WorldRead.GetBlockEntity(ctx.X, ctx.Y, ctx.Z);

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
                        // TODO: Implement this
                        // EntityItem entityItem = new(ctx.WorldRead, ctx.X + offsetX, ctx.Y + offsetY, ctx.Z + offsetZ, new ItemStack(stack.itemId, amount, stack.getDamage()));
                        // float floatVar = 0.05F;
                        // 
                        // entityItem.velocityX = (float)random.NextGaussian() * floatVar;
                        // entityItem.velocityY = (float)random.NextGaussian() * floatVar + 0.2F;
                        // entityItem.velocityZ = (float)random.NextGaussian() * floatVar;
                        // 
                        // ctx.Entities.SpawnEntity(entityItem);
                    }
                }
            }
        }

        base.onBreak(ctx);
    }
}