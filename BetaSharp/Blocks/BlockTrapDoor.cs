using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockTrapDoor : Block
{

    public BlockTrapDoor(int id, Material material) : base(id, material)
    {
        textureId = 84;
        if (material == Material.Metal)
        {
            ++textureId;
        }

        float halfWidth = 0.5F;
        float fullHeight = 1.0F;
        setBoundingBox(0.5F - halfWidth, 0.0F, 0.5F - halfWidth, 0.5F + halfWidth, fullHeight, 0.5F + halfWidth);
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
        return BlockRendererType.Standard;
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
        updateBoundingBox(iBlockAccess.getBlockMeta(x, y, z));
    }

    public override void setupRenderBoundingBox()
    {
        float height = 3.0F / 16.0F;
        setBoundingBox(0.0F, 0.5F - height / 2.0F, 0.0F, 1.0F, 0.5F + height / 2.0F, 1.0F);
    }

    public void updateBoundingBox(int meta)
    {
        float height = 3.0F / 16.0F;
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, height, 1.0F);
        if (isOpen(meta))
        {
            if ((meta & 3) == 0)
            {
                setBoundingBox(0.0F, 0.0F, 1.0F - height, 1.0F, 1.0F, 1.0F);
            }

            if ((meta & 3) == 1)
            {
                setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, height);
            }

            if ((meta & 3) == 2)
            {
                setBoundingBox(1.0F - height, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
            }

            if ((meta & 3) == 3)
            {
                setBoundingBox(0.0F, 0.0F, 0.0F, height, 1.0F, 1.0F);
            }
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
            world.setBlockMeta(x, y, z, meta ^ 4);
            world.worldEvent(player, 1003, x, y, z, 0);
            return true;
        }
    }

    public void setOpen(World world, int x, int y, int z, bool open)
    {
        int meta = world.getBlockMeta(x, y, z);
        bool isOpen = (meta & 4) > 0;
        if (isOpen != open)
        {
            world.setBlockMeta(x, y, z, meta ^ 4);
            world.worldEvent((EntityPlayer)null, 1003, x, y, z, 0);
        }
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        if (!world.isRemote)
        {
            int meta = world.getBlockMeta(x, y, z);
            int xPos = x;
            int zPos = z;
            if ((meta & 3) == 0)
            {
                zPos = z + 1;
            }

            if ((meta & 3) == 1)
            {
                --zPos;
            }

            if ((meta & 3) == 2)
            {
                xPos = x + 1;
            }

            if ((meta & 3) == 3)
            {
                --xPos;
            }

            if (!world.shouldSuffocate(xPos, y, zPos))
            {
                world.setBlock(x, y, z, 0);
                dropStacks(world, x, y, z, meta);
            }

            if (id > 0 && Block.Blocks[id].canEmitRedstonePower())
            {
                bool isPowered = world.isPowered(x, y, z);
                setOpen(world, x, y, z, isPowered);
            }

        }
    }

    public override HitResult raycast(World world, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        updateBoundingBox(world, x, y, z);
        return base.raycast(world, x, y, z, startPos, endPos);
    }

    public override void onPlaced(World world, int x, int y, int z, int direction)
    {
        sbyte meta = 0;
        if (direction == 2)
        {
            meta = 0;
        }

        if (direction == 3)
        {
            meta = 1;
        }

        if (direction == 4)
        {
            meta = 2;
        }

        if (direction == 5)
        {
            meta = 3;
        }

        world.setBlockMeta(x, y, z, meta);
    }

    public override bool canPlaceAt(World world, int x, int y, int z, int side)
    {
        if (side == 0)
        {
            return false;
        }
        else if (side == 1)
        {
            return false;
        }
        else
        {
            if (side == 2)
            {
                ++z;
            }

            if (side == 3)
            {
                --z;
            }

            if (side == 4)
            {
                ++x;
            }

            if (side == 5)
            {
                --x;
            }

            return world.shouldSuffocate(x, y, z);
        }
    }

    public static bool isOpen(int meta)
    {
        return (meta & 4) != 0;
    }
}
