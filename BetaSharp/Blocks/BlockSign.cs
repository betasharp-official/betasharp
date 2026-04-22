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

    private const float Width = 0.25F;
    private const float Height = 1.0F;
    private const float TopOffset = 9.0F / 32.0F;
    private const float BottomOffset = 25.0F / 32.0F;
    private const float MinExtent = 0.0F;
    private const float MaxExtent = 1.0F;
    private const float Thickness = 2.0F / 16.0F;

    public BlockSign(int id, Type blockEntityType, bool standing) : base(id, Material.Wood)
    {
        _standing = standing;
        TextureId = BlockTextures.OakPlanks;
        _blockEntityType = blockEntityType;
        setBoundingBox(0.5F - Width, 0.0F, 0.5F - Width, 0.5F + Width, Height, 0.5F + Width);
    }

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override Box getBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        updateBoundingBox(world, x, y, z);
        return base.getBoundingBox(world, entities, x, y, z);
    }

    public override void updateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        if (_standing) return;

        Side facing = blockReader.GetBlockMeta(x, y, z).ToSide();

        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        switch (facing)
        {
            case Side.North:
                setBoundingBox(MinExtent, TopOffset, 1.0F - Thickness, MaxExtent, BottomOffset, 1.0F);
                break;
            case Side.South:
                setBoundingBox(MinExtent, TopOffset, 0.0F, MaxExtent, BottomOffset, Thickness);
                break;
            case Side.West:
                setBoundingBox(1.0F - Thickness, TopOffset, MinExtent, 1.0F, BottomOffset, MaxExtent);
                break;
            case Side.East:
                setBoundingBox(0.0F, TopOffset, MinExtent, Thickness, BottomOffset, MaxExtent);
                break;
        }
    }

    public override BlockRendererType getRenderType() => BlockRendererType.Entity;

    public override bool isFullCube() => false;

    public override bool isOpaque() => false;

    public override BlockEntity getBlockEntity()
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

    public override void neighborUpdate(OnTickEvent @event)
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
            Side facing = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z).ToSide();
            shouldBreak = true;
            switch (facing)
            {
                case Side.North when @event.World.Reader.GetMaterial(@event.X, @event.Y, @event.Z + 1).IsSolid:
                case Side.South when @event.World.Reader.GetMaterial(@event.X, @event.Y, @event.Z - 1).IsSolid:
                case Side.West when @event.World.Reader.GetMaterial(@event.X + 1, @event.Y, @event.Z).IsSolid:
                case Side.East when @event.World.Reader.GetMaterial(@event.X - 1, @event.Y, @event.Z).IsSolid:
                    shouldBreak = false;
                    break;
            }
        }

        if (shouldBreak)
        {
            dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }

        base.neighborUpdate(@event);
    }
}
