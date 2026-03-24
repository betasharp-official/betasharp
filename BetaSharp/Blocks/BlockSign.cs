using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockSign : BlockWithEntity
{
    private readonly Type _blockEntityType;
    private readonly bool _standing;

    public BlockSign(int id, Type blockEntityType, bool standing) : base(id, Material.Wood)
    {
        const float width = 0.25F;
        const float height = 1.0F;
        _standing = standing;
        TextureId = 4;
        _blockEntityType = blockEntityType;
        SetBoundingBox(0.5F - width, 0.0F, 0.5F - width, 0.5F + width, height, 0.5F + width);
    }

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override Box GetBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        UpdateBoundingBox(world, x, y, z);
        return base.GetBoundingBox(world, entities, x, y, z);
    }

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        if (_standing)
        {
            return;
        }

        int facing = blockReader.GetBlockMeta(x, y, z);
        const float topOffset = 9.0F / 32.0F;
        const float bottomOffset = 25.0F / 32.0F;
        const float minExtent = 0.0F;
        const float maxExtent = 1.0F;
        const float thickness = 2.0F / 16.0F;
        SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        switch (facing)
        {
            case (int)Side.North:
                SetBoundingBox(minExtent, topOffset, 1.0F - thickness, maxExtent, bottomOffset, 1.0F);
                break;
            case (int)Side.South:
                SetBoundingBox(minExtent, topOffset, 0.0F, maxExtent, bottomOffset, thickness);
                break;
            case (int)Side.West:
                SetBoundingBox(1.0F - thickness, topOffset, minExtent, 1.0F, bottomOffset, maxExtent);
                break;
            case (int)Side.East:
                SetBoundingBox(0.0F, topOffset, minExtent, thickness, bottomOffset, maxExtent);
                break;
        }
    }

    public override BlockRendererType GetRenderType() => BlockRendererType.Entity;

    public override bool IsFullCube() => false;

    public override bool IsOpaque() => false;

    public override BlockEntity? getBlockEntity()
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

    public override int GetDroppedItemId(int blockMeta) => Item.Sign.id;

    public override void NeighborUpdate(OnTickEvent @event)
    {
        bool shouldBreak = false;
        if (_standing)
        {
            if (!@event.World.Reader.GetMaterial(@event.X, @event.Y - 1, @event.Z).IsSolid)
            {
                shouldBreak = true;
            }
        }
        else
        {
            int facing = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
            shouldBreak = true;
            if (facing == 2 && @event.World.Reader.GetMaterial(@event.X, @event.Y, @event.Z + 1).IsSolid)
            {
                shouldBreak = false;
            }

            if (facing == 3 && @event.World.Reader.GetMaterial(@event.X, @event.Y, @event.Z - 1).IsSolid)
            {
                shouldBreak = false;
            }

            if (facing == 4 && @event.World.Reader.GetMaterial(@event.X + 1, @event.Y, @event.Z).IsSolid)
            {
                shouldBreak = false;
            }

            if (facing == 5 && @event.World.Reader.GetMaterial(@event.X - 1, @event.Y, @event.Z).IsSolid)
            {
                shouldBreak = false;
            }
        }

        if (shouldBreak)
        {
            DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }

        base.NeighborUpdate(@event);
    }
}
