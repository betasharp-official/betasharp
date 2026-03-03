using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockDoor : Block
{
    public BlockDoor(int id, Material material) : base(id, material)
    {
        textureId = 97;
        if (material == Material.Metal)
        {
            ++textureId;
        }

        float halfWidth = 0.5F;
        float height = 1.0F;
        setBoundingBox(0.5F - halfWidth, 0.0F, 0.5F - halfWidth, 0.5F + halfWidth, height, 0.5F + halfWidth);
    }

    public override int getTexture(int side, int meta)
    {
        if (side != 0 && side != 1)
        {
            int facing = setOpen(meta);
            if ((facing == 0 || facing == 2) ^ side <= 3)
            {
                return textureId;
            }
            else
            {
                int textureIndex = facing / 2 + (side & 1 ^ facing);
                textureIndex += (meta & 4) / 4;
                int textureId = base.textureId - (meta & 8) * 2;
                if ((textureIndex & 1) != 0)
                {
                    textureId = -textureId;
                }

                return textureId;
            }
        }
        else
        {
            return textureId;
        }
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Door;
    }

    public override Box getBoundingBox(World world, int x, int y, int z)
    {
        updateBoundingBox(world, x, y, z);
        return base.getBoundingBox(world, x, y, z);
    }

    public override Box? getCollisionShape(World world, int x, int y, int z)
    {
        updateBoundingBox(world, x, y, z);
        return base.getCollisionShape(world, x, y, z);
    }

    public override void updateBoundingBox(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        rotate(setOpen(iBlockAccess.getBlockMeta(x, y, z)));
    }

    public void rotate(int meta)
    {
        float thickness = 3.0F / 16.0F;
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 2.0F, 1.0F);
        if (meta == 0)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, thickness);
        }

        if (meta == 1)
        {
            setBoundingBox(1.0F - thickness, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        }

        if (meta == 2)
        {
            setBoundingBox(0.0F, 0.0F, 1.0F - thickness, 1.0F, 1.0F, 1.0F);
        }

        if (meta == 3)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, thickness, 1.0F, 1.0F);
        }

    }

    public override void onBlockBreakStart(World world, int x, int y, int z, EntityPlayer player)
    {
        onUse(world, x, y, z, player);
    }

    public override bool onUse(World world, int x, int y, int z, EntityPlayer player)
    {
        if (material == Material.Metal)
        {
            return true;
        }
        else
        {
            int meta = world.getBlockMeta(x, y, z);
            if ((meta & 8) != 0)
            {
                if (world.getBlockId(x, y - 1, z) == id)
                {
                    onUse(world, x, y - 1, z, player);
                }

                return true;
            }
            else
            {
                if (world.getBlockId(x, y + 1, z) == id)
                {
                    world.setBlockMeta(x, y + 1, z, (meta ^ 4) + 8);
                }

                world.setBlockMeta(x, y, z, meta ^ 4);
                world.setBlocksDirty(x, y - 1, z, x, y, z);
                world.worldEvent(player, 1003, x, y, z, 0);
                return true;
            }
        }
    }

    public void setOpen(World world, int x, int y, int z, bool open)
    {
        int meta = world.getBlockMeta(x, y, z);
        if ((meta & 8) != 0)
        {
            if (world.getBlockId(x, y - 1, z) == id)
            {
                setOpen(world, x, y - 1, z, open);
            }

        }
        else
        {
            bool isOpen = (world.getBlockMeta(x, y, z) & 4) > 0;
            if (isOpen != open)
            {
                if (world.getBlockId(x, y + 1, z) == id)
                {
                    world.setBlockMeta(x, y + 1, z, (meta ^ 4) + 8);
                }

                world.setBlockMeta(x, y, z, meta ^ 4);
                world.setBlocksDirty(x, y - 1, z, x, y, z);
                world.worldEvent((EntityPlayer)null, 1003, x, y, z, 0);
            }
        }
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        int meta = world.getBlockMeta(x, y, z);
        if ((meta & 8) != 0)
        {
            if (world.getBlockId(x, y - 1, z) != base.id)
            {
                world.setBlock(x, y, z, 0);
            }

            if (id > 0 && Block.Blocks[id].canEmitRedstonePower())
            {
                neighborUpdate(world, x, y - 1, z, id);
            }
        }
        else
        {
            bool wasBroken = false;
            if (world.getBlockId(x, y + 1, z) != base.id)
            {
                world.setBlock(x, y, z, 0);
                wasBroken = true;
            }

            if (!world.shouldSuffocate(x, y - 1, z))
            {
                world.setBlock(x, y, z, 0);
                wasBroken = true;
                if (world.getBlockId(x, y + 1, z) == base.id)
                {
                    world.setBlock(x, y + 1, z, 0);
                }
            }

            if (wasBroken)
            {
                if (!world.isRemote)
                {
                    dropStacks(world, x, y, z, meta);
                }
            }
            else if (id > 0 && Block.Blocks[id].canEmitRedstonePower())
            {
                bool isPowered = world.isPowered(x, y, z) || world.isPowered(x, y + 1, z);
                setOpen(world, x, y, z, isPowered);
            }
        }

    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return (blockMeta & 8) != 0 ? 0 : (material == Material.Metal ? Item.IronDoor.id : Item.WoodenDoor.id);
    }

    public override HitResult raycast(World world, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        updateBoundingBox(world, x, y, z);
        return base.raycast(world, x, y, z, startPos, endPos);
    }

    public int setOpen(int meta)
    {
        return (meta & 4) == 0 ? meta - 1 & 3 : meta & 3;
    }

    public override bool canPlaceAt(World world, int x, int y, int z)
    {
        return y >= 127 ? false : world.shouldSuffocate(x, y - 1, z) && base.canPlaceAt(world, x, y, z) && base.canPlaceAt(world, x, y + 1, z);
    }

    public static bool isOpen(int meta)
    {
        return (meta & 4) != 0;
    }

    public override int getPistonBehavior()
    {
        return 1;
    }
}
