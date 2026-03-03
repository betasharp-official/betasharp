using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockDispenser : BlockWithEntity
{
    private static readonly ThreadLocal<JavaRandom> s_random = new(() => new());

    public BlockDispenser(int id) : base(id, Material.Stone)
    {
        textureId = 45;
    }

    public override int getTickRate()
    {
        return 4;
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return Block.Dispenser.id;
    }

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
            if (Block.BlocksOpaque[blockNorth] && !Block.BlocksOpaque[blockSouth])
            {
                direction = 3;
            }

            if (Block.BlocksOpaque[blockSouth] && !Block.BlocksOpaque[blockNorth])
            {
                direction = 2;
            }

            if (Block.BlocksOpaque[blockWest] && !Block.BlocksOpaque[blockEast])
            {
                direction = 5;
            }

            if (Block.BlocksOpaque[blockEast] && !Block.BlocksOpaque[blockWest])
            {
                direction = 4;
            }

            world.setBlockMeta(x, y, z, direction);
        }
    }

    public override int getTextureId(IBlockAccess iBlockAccess, int x, int y, int z, int side)
    {
        if (side == 1)
        {
            return textureId + 17;
        }
        else if (side == 0)
        {
            return textureId + 17;
        }
        else
        {
            int meta = iBlockAccess.getBlockMeta(x, y, z);
            return side != meta ? textureId : textureId + 1;
        }
    }

    public override int getTexture(int side)
    {
        return side == 1 ? textureId + 17 : (side == 0 ? textureId + 17 : (side == 3 ? textureId + 1 : textureId));
    }

    public override bool onUse(World world, int x, int y, int z, EntityPlayer player)
    {
        if (world.isRemote)
        {
            return true;
        }
        else
        {
            BlockEntityDispenser dispenser = (BlockEntityDispenser)world.getBlockEntity(x, y, z);
            player.openDispenserScreen(dispenser);
            return true;
        }
    }

    private void dispense(World world, int x, int y, int z, JavaRandom random)
    {
        int meta = world.getBlockMeta(x, y, z);
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

        BlockEntityDispenser dispenser = (BlockEntityDispenser)world.getBlockEntity(x, y, z);
        ItemStack itemStack = dispenser.getItemToDispose();
        double spawnX = (double)x + (double)dirX * 0.6D + 0.5D;
        double spawnY = (double)y + 0.5D;
        double spawnZ = (double)z + (double)dirZ * 0.6D + 0.5D;
        if (itemStack == null)
        {
            world.worldEvent(1001, x, y, z, 0);
        }
        else
        {
            if (itemStack.itemId == Item.ARROW.id)
            {
                EntityArrow arrow = new EntityArrow(world, spawnX, spawnY, spawnZ);
                arrow.setArrowHeading((double)dirX, (double)0.1F, (double)dirZ, 1.1F, 6.0F);
                arrow.doesArrowBelongToPlayer = true;
                world.SpawnEntity(arrow);
                world.worldEvent(1002, x, y, z, 0);
            }
            else if (itemStack.itemId == Item.Egg.id)
            {
                EntityEgg egg = new EntityEgg(world, spawnX, spawnY, spawnZ);
                egg.setEggHeading((double)dirX, (double)0.1F, (double)dirZ, 1.1F, 6.0F);
                world.SpawnEntity(egg);
                world.worldEvent(1002, x, y, z, 0);
            }
            else if (itemStack.itemId == Item.Snowball.id)
            {
                EntitySnowball snowball = new EntitySnowball(world, spawnX, spawnY, spawnZ);
                snowball.setSnowballHeading((double)dirX, (double)0.1F, (double)dirZ, 1.1F, 6.0F);
                world.SpawnEntity(snowball);
                world.worldEvent(1002, x, y, z, 0);
            }
            else
            {
                EntityItem item = new EntityItem(world, spawnX, spawnY - 0.3D, spawnZ, itemStack);
                double var20 = random.NextDouble() * 0.1D + 0.2D;
                item.velocityX = (double)dirX * var20;
                item.velocityY = (double)0.2F;
                item.velocityZ = (double)dirZ * var20;
                item.velocityX += random.NextGaussian() * (double)0.0075F * 6.0D;
                item.velocityY += random.NextGaussian() * (double)0.0075F * 6.0D;
                item.velocityZ += random.NextGaussian() * (double)0.0075F * 6.0D;
                world.SpawnEntity(item);
                world.worldEvent(1000, x, y, z, 0);
            }

            world.worldEvent(2000, x, y, z, dirX + 1 + (dirZ + 1) * 3);
        }

    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        if (id > 0 && Block.Blocks[id].canEmitRedstonePower())
        {
            bool isPowered = world.isPowered(x, y, z) || world.isPowered(x, y + 1, z);
            if (isPowered)
            {
                world.ScheduleBlockUpdate(x, y, z, base.id, getTickRate());
            }
        }

    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (world.isPowered(x, y, z) || world.isPowered(x, y + 1, z))
        {
            dispense(world, x, y, z, random);
        }

    }

    protected override BlockEntity getBlockEntity()
    {
        return new BlockEntityDispenser();
    }

    public override void onPlaced(World world, int x, int y, int z, EntityLiving placer)
    {
        int direction = MathHelper.Floor((double)(placer.yaw * 4.0F / 360.0F) + 0.5D) & 3;
        if (direction == 0)
        {
            world.setBlockMeta(x, y, z, 2);
        }

        if (direction == 1)
        {
            world.setBlockMeta(x, y, z, 5);
        }

        if (direction == 2)
        {
            world.setBlockMeta(x, y, z, 3);
        }

        if (direction == 3)
        {
            world.setBlockMeta(x, y, z, 4);
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
                    EntityItem entityItem = new(world, (double)((float)x + offsetX), (double)((float)y + offsetY), (double)((float)z + offsetZ), new ItemStack(stack.itemId, amount, stack.getDamage()));
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
}
