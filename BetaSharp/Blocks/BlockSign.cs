using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockSign : BlockWithEntity
{
    private readonly Type _blockEntityType;
    private readonly bool _standing;

    public BlockSign(int id, Type blockEntityType, bool standing) : base(id, Material.Wood)
    {
        _standing = standing;
        textureId = 4;
        _blockEntityType = blockEntityType;
        float width = 0.25F;
        float height = 1.0F;
        setBoundingBox(0.5F - width, 0.0F, 0.5F - width, 0.5F + width, height, 0.5F + width);
    }

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z) => null;

    public override Box getBoundingBox(IBlockReader world, int x, int y, int z)
    {
        updateBoundingBox(world, x, y, z);
        return base.getBoundingBox(world, x, y, z);
    }

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z)
    {
        if (!_standing)
        {
            int facing = iBlockReader.GetBlockMeta(x, y, z);
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

    public override BlockRendererType getRenderType() => BlockRendererType.Entity;

    public override bool isFullCube() => false;

    public override bool isOpaque() => false;

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

    public override int getDroppedItemId(int blockMeta) => Item.Sign.id;

    public override void neighborUpdate(OnTickEvt evt)
    {
        bool shouldBreak = false;
        if (_standing)
        {
            if (!evt.Level.Reader.GetMaterial(evt.X, evt.Y - 1, evt.Z).IsSolid)
            {
                shouldBreak = true;
            }
        }
        else
        {
            int facing = evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z);
            shouldBreak = true;
            if (facing == 2 && evt.Level.Reader.GetMaterial(evt.X, evt.Y, evt.Z + 1).IsSolid)
            {
                shouldBreak = false;
            }

            if (facing == 3 && evt.Level.Reader.GetMaterial(evt.X, evt.Y, evt.Z - 1).IsSolid)
            {
                shouldBreak = false;
            }

            if (facing == 4 && evt.Level.Reader.GetMaterial(evt.X + 1, evt.Y, evt.Z).IsSolid)
            {
                shouldBreak = false;
            }

            if (facing == 5 && evt.Level.Reader.GetMaterial(evt.X - 1, evt.Y, evt.Z).IsSolid)
            {
                shouldBreak = false;
            }
        }

        if (shouldBreak)
        {
            dropStacks(new OnDropEvt(evt.Level, evt.X, evt.Y, evt.Z, evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z)));
            evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, 0);
        }

        base.neighborUpdate(evt);
    }
}
