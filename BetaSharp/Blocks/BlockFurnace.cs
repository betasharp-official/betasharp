using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

public class BlockFurnace : BlockWithEntity
{

    private readonly JavaRandom _random = new();
    private bool lit = false;
    private static readonly ThreadLocal<bool> s_ignoreBlockRemoval = new(() => false);

    private int[] Textures = [44, 45, 45+17];

    private int meltSpeed = 200;
    /// <summary>
    /// Texture array
    /// [0] = front
    /// [1] = side
    /// [2] = top
    /// </summary>
    /// <param name="id"></param>
    /// <param name="Textures"></param>
    public BlockFurnace(int id, bool lit = false, int[]? Textures = null) : base(id, Material.Stone)
    {
        this.Textures = Textures ?? this.Textures;
        textureId = this.Textures[0];
    }

    public BlockFurnace setMeltSpeed(int speed)
    {
        this.meltSpeed = speed;
        return this;
    }

    public int getMeltSpeed()
    {
        return meltSpeed;
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return id;
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
            int westBlockId = world.getBlockId(x - 1, y, z);
            int eastBlockId = world.getBlockId(x + 1, y, z);
            sbyte direction = 3;
            if (Block.BlocksOpaque[blockNorth] && !Block.BlocksOpaque[blockSouth])
            {
                direction = 3;
            }

            if (Block.BlocksOpaque[blockSouth] && !Block.BlocksOpaque[blockNorth])
            {
                direction = 2;
            }

            if (Block.BlocksOpaque[westBlockId] && !Block.BlocksOpaque[eastBlockId])
            {
                direction = 5;
            }

            if (Block.BlocksOpaque[eastBlockId] && !Block.BlocksOpaque[westBlockId])
            {
                direction = 4;
            }

            world.setBlockMeta(x, y, z, direction);
        }
    }

    public override int getTextureId(BlockView blockView, int x, int y, int z, int side)
    {
        if (side == 0 || side == 1)
        {
            return Textures[2];
        }
        else
        {
            int meta = blockView.getBlockMeta(x, y, z);
            if (side != meta)
            {
                return Textures[1];
            }

            return Textures[0];
        }     
    }

    public override void randomDisplayTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (lit)
        {
            int meta = world.getBlockMeta(x, y, z);
            float particleX = (float)x + 0.5F;
            float particleY = (float)y + 0.0F + random.NextFloat() * 6.0F / 16.0F;
            float particleZ = (float)z + 0.5F;
            float flameOffset = 0.52F;
            float randomOffset = random.NextFloat() * 0.6F - 0.3F;
            if (meta == 4)
            {
                world.addParticle("smoke", (double)(particleX - flameOffset), (double)particleY, (double)(particleZ + randomOffset), 0.0D, 0.0D, 0.0D);
                world.addParticle("flame", (double)(particleX - flameOffset), (double)particleY, (double)(particleZ + randomOffset), 0.0D, 0.0D, 0.0D);
            }
            else if (meta == 5)
            {
                world.addParticle("smoke", (double)(particleX + flameOffset), (double)particleY, (double)(particleZ + randomOffset), 0.0D, 0.0D, 0.0D);
                world.addParticle("flame", (double)(particleX + flameOffset), (double)particleY, (double)(particleZ + randomOffset), 0.0D, 0.0D, 0.0D);
            }
            else if (meta == 2)
            {
                world.addParticle("smoke", (double)(particleX + randomOffset), (double)particleY, (double)(particleZ - flameOffset), 0.0D, 0.0D, 0.0D);
                world.addParticle("flame", (double)(particleX + randomOffset), (double)particleY, (double)(particleZ - flameOffset), 0.0D, 0.0D, 0.0D);
            }
            else if (meta == 3)
            {
                world.addParticle("smoke", (double)(particleX + randomOffset), (double)particleY, (double)(particleZ + flameOffset), 0.0D, 0.0D, 0.0D);
                world.addParticle("flame", (double)(particleX + randomOffset), (double)particleY, (double)(particleZ + flameOffset), 0.0D, 0.0D, 0.0D);
            }

        }
    }

    public override int getTexture(int side)
    {
        if (side == 0 || side == 1)
        {
            return Textures[2];
        } 
        if (side == 3)
        {
            return Textures[0];
        }
        return Textures[1];
    }

    public override bool onUse(World world, int x, int y, int z, EntityPlayer player)
    {
        if (world.isRemote)
        {
            return true;
        }
        else
        {
            
            BlockEntityFurnace furnace = (BlockEntityFurnace)world.getBlockEntity(x, y, z);
            furnace.setMeltSpeed(meltSpeed);
            player.openFurnaceScreen(furnace);
            return true;
        }
    }

    public static void updateLitState(bool lit, World world, int x, int y, int z)
    {
        int meta = world.getBlockMeta(x, y, z);
        int id = world.getBlockId(x,y,z);

        BlockEntity furnace = world.getBlockEntity(x, y, z);
        s_ignoreBlockRemoval.Value = true;
        if (lit)
        {
            world.setBlock(x, y, z, id+1);
        }
        else
        {
            world.setBlock(x, y, z, id-1);
        }

        s_ignoreBlockRemoval.Value = false;

        world.setBlockMeta(x, y, z, meta);
        furnace.cancelRemoval();                        
        world.setBlockEntity(x, y, z, furnace);
    }

    protected override BlockEntity getBlockEntity()
    {
        return new BlockEntityFurnace();
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
        if (!s_ignoreBlockRemoval.Value)
        {
            BlockEntityFurnace furnace = (BlockEntityFurnace)world.getBlockEntity(x, y, z);

            for (int slotIndex = 0; slotIndex < furnace.size(); ++slotIndex)
            {
                ItemStack stack = furnace.getStack(slotIndex);
                if (stack != null)
                {
                    float offsetX = _random.NextFloat() * 0.8F + 0.1F;
                    float offsetY = _random.NextFloat() * 0.8F + 0.1F;
                    float offsetZ = _random.NextFloat() * 0.8F + 0.1F;

                    while (stack.count > 0)
                    {
                        int ran = _random.NextInt(21) + 10;
                        if (ran > stack.count)
                        {
                            ran = stack.count;
                        }

                        stack.count -= ran;
                        EntityItem droppedItem = new EntityItem(world, 
                                                (double)((float)x + offsetX), 
                                                (double)((float)y + offsetY), 
                                                (double)((float)z + offsetZ), 
                                                new ItemStack(stack.itemId, ran, stack.getDamage()));
                        float var13 = 0.05F;
                        droppedItem.velocityX = (double)((float)_random.NextGaussian() * var13);
                        droppedItem.velocityY = (double)((float)_random.NextGaussian() * var13 + 0.2F);
                        droppedItem.velocityZ = (double)((float)_random.NextGaussian() * var13);
                        world.SpawnEntity(droppedItem);
                    }
                }
            }
        }

        base.onBreak(world, x, y, z);
    }
}
