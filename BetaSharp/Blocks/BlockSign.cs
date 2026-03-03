using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockSign : BlockWithEntity
{
    private readonly Type _blockEntityType;
    private readonly bool _standing;

    public BlockSign(int id, Type blockEntityType, bool standing) : base(id, Material.Wood)
    {
        this._standing = standing;
        textureId = 4;
        this._blockEntityType = blockEntityType;
        float width = 0.25F;
        float height = 1.0F;
        setBoundingBox(0.5F - width, 0.0F, 0.5F - width, 0.5F + width, height, 0.5F + width);
    }

    public override Box? getCollisionShape(World world, int x, int y, int z)
    {
        return null;
    }

    public override Box getBoundingBox(World world, int x, int y, int z)
    {
        updateBoundingBox(world, x, y, z);
        return base.getBoundingBox(world, x, y, z);
    }

    public override void updateBoundingBox(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        if (!_standing)
        {
            int facing = iBlockAccess.getBlockMeta(x, y, z);
            float topOffset = 9.0F / 32.0F;
            float bottomOffset = 25.0F / 32.0F;
            float minExtent = 0.0F;
            float maxExtent = 1.0F;
            float thickness = 2.0F / 16.0F;
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
            if (facing == 2)
            {
                setBoundingBox(minExtent, topOffset, 1.0F - thickness, maxExtent, bottomOffset, 1.0F);
            }

            if (facing == 3)
            {
                setBoundingBox(minExtent, topOffset, 0.0F, maxExtent, bottomOffset, thickness);
            }

            if (facing == 4)
            {
                setBoundingBox(1.0F - thickness, topOffset, minExtent, 1.0F, bottomOffset, maxExtent);
            }

            if (facing == 5)
            {
                setBoundingBox(0.0F, topOffset, minExtent, thickness, bottomOffset, maxExtent);
            }
        }
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Entity;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override bool isOpaque()
    {
        return false;
    }

    protected override BlockEntity getBlockEntity()
    {
        try
        {
            return Activator.CreateInstance(_blockEntityType) as BlockEntity;
        }
        catch (Exception exception)
        {
            throw new Exception("Unable to get new block entity", exception);
        }
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return Item.Sign.id;
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        bool shouldBreak = false;
        if (_standing)
        {
            if (!world.getMaterial(x, y - 1, z).IsSolid)
            {
                shouldBreak = true;
            }
        }
        else
        {
            int facing = world.getBlockMeta(x, y, z);
            shouldBreak = true;
            if (facing == 2 && world.getMaterial(x, y, z + 1).IsSolid)
            {
                shouldBreak = false;
            }

            if (facing == 3 && world.getMaterial(x, y, z - 1).IsSolid)
            {
                shouldBreak = false;
            }

            if (facing == 4 && world.getMaterial(x + 1, y, z).IsSolid)
            {
                shouldBreak = false;
            }

            if (facing == 5 && world.getMaterial(x - 1, y, z).IsSolid)
            {
                shouldBreak = false;
            }
        }

        if (shouldBreak)
        {
            dropStacks(world, x, y, z, world.getBlockMeta(x, y, z));
            world.setBlock(x, y, z, 0);
        }

        base.neighborUpdate(world, x, y, z, id);
    }
}
