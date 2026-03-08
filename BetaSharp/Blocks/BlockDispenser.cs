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

    public override void onPlaced(World world, int x, int y, int z)
    {
        base.onPlaced(world, x, y, z);
        updateDirection(world, x, y, z);
    }

    private void updateDirection(World world, int x, int y, int z)
    {
        if (!world.isRemote)
        {
            int blockNorth = world.getBlockId(x, y, z - 1);
            int blockSouth = world.getBlockId(x, y, z + 1);
            int blockWest = world.getBlockId(x - 1, y, z);
            int blockEast = world.getBlockId(x + 1, y, z);
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

            world.setBlockMeta(x, y, z, direction);
        }
    }

    public override int getTextureId(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        if (side == 1)
        {
            return textureId + 17;
        }

        if (side == 0)
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
        ctx.Player.openDispenserScreen(dispenser);
        return true;
    }

    private void dispense(OnTickEvt ctx)
    {
        int meta = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
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

        BlockEntityDispenser? dispenser = (BlockEntityDispenser?)ctx.WorldRead.GetBlockEntity(ctx.X, ctx.Y, ctx.Z);
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
                EntityArrow arrow = new(ctx.World, spawnX, spawnY, spawnZ);
                arrow.setArrowHeading(dirX, 0.1F, dirZ, 1.1F, 6.0F);
                arrow.doesArrowBelongToPlayer = true;
                ctx.Entities.SpawnEntity(arrow);
                ctx.Broadcaster.WorldEvent(1002, ctx.X, ctx.Y, ctx.Z, 0);
            }
            else if (itemStack.itemId == Item.Egg.id)
            {
                EntityEgg egg = new(world, spawnX, spawnY, spawnZ);
                egg.setEggHeading(dirX, 0.1F, dirZ, 1.1F, 6.0F);
                ctx.Entities.SpawnEntity(egg);
                ctx.Broadcaster.WorldEvent(1002, ctx.X, ctx.Y, ctx.Z, 0);
            }
            else if (itemStack.itemId == Item.Snowball.id)
            {
                EntitySnowball snowball = new(world, spawnX, spawnY, spawnZ);
                snowball.setSnowballHeading(dirX, 0.1F, dirZ, 1.1F, 6.0F);
                ctx.Entities.SpawnEntity(snowball);
                ctx.Broadcaster.WorldEvent(1002, ctx.X, ctx.Y, ctx.Z, 0);
            }
            else
            {
                EntityItem item = new(ctx.World, spawnX, spawnY - 0.3D, spawnZ, itemStack);
                double var20 = ctx.Random.NextDouble() * 0.1D + 0.2D;
                item.velocityX = dirX * var20;
                item.velocityY = 0.2F;
                item.velocityZ = dirZ * var20;
                item.velocityX += ctx.Random.NextGaussian() * 0.0075F * 6.0D;
                item.velocityY += ctx.Random.NextGaussian() * 0.0075F * 6.0D;
                item.velocityZ += ctx.Random.NextGaussian() * 0.0075F * 6.0D;
                ctx.Entities.SpawnEntity(item);
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
                ctx.WorldRead.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, id, getTickRate());
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

    public override void onPlaced(OnPlacedEvt ctx)
    {
        int direction = MathHelper.Floor(ctx.Placer.yaw * 4.0F / 360.0F + 0.5D) & 3;
        if (direction == 0)
        {
            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, 2);
        }

        if (direction == 1)
        {
            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, 5);
        }

        if (direction == 2)
        {
            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, 3);
        }

        if (direction == 3)
        {
            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, 4);
        }
    }

    public override void onBreak(World world, int x, int y, int z)
    {
        BlockEntityDispenser dispenser = (BlockEntityDispenser)world.getBlockEntity(x, y, z);

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
                    EntityItem entityItem = new(world, x + offsetX, y + offsetY, z + offsetZ, new ItemStack(stack.itemId, amount, stack.getDamage()));
                    float var13 = 0.05F;
                    entityItem.velocityX = (float)random.NextGaussian() * var13;
                    entityItem.velocityY = (float)random.NextGaussian() * var13 + 0.2F;
                    entityItem.velocityZ = (float)random.NextGaussian() * var13;
                    world.SpawnEntity(entityItem);
                }
            }
        }

        base.onBreak(world, x, y, z);
    }
}
