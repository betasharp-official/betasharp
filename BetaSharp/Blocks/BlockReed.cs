using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockReed : Block
{
    private const float HalfWidth = 6.0F / 16.0F;

    public BlockReed(int id, int textureId) : base(id, Material.Plant)
    {
        TextureId = textureId;
        SetBoundingBox(0.5F - HalfWidth, 0.0F, 0.5F - HalfWidth, 0.5F + HalfWidth, 1.0F, 0.5F + HalfWidth);
        SetTickRandomly(true);
    }

    public override void OnTick(OnTickEvent @event)
    {
        if (!@event.World.Reader.IsAir(@event.X, @event.Y + 1, @event.Z))
        {
            return;
        }

        int heightBelow = 1;
        while (@event.World.Reader.GetBlockId(@event.X, @event.Y - heightBelow, @event.Z) == ID)
        {
            heightBelow++;
        }

        if (heightBelow >= 3)
        {
            return;
        }

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if (meta == 15)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y + 1, @event.Z, ID);
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, 0);
        }
        else
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, meta + 1);
        }
    }

    public override bool CanPlaceAt(CanPlaceAtContext evt)
    {
        int blockBelowId = evt.World.Reader.GetBlockId(evt.X, evt.Y - 1, evt.Z);
        return blockBelowId == ID ||
               ((blockBelowId == GrassBlock.ID ||
                 blockBelowId == Dirt.ID) && (evt.World.Reader.GetMaterial(evt.X - 1, evt.Y - 1, evt.Z) == Material.Water ||
                                              evt.World.Reader.GetMaterial(evt.X + 1, evt.Y - 1, evt.Z) == Material.Water ||
                                              evt.World.Reader.GetMaterial(evt.X, evt.Y - 1, evt.Z - 1) == Material.Water ||
                                              evt.World.Reader.GetMaterial(evt.X, evt.Y - 1, evt.Z + 1) == Material.Water));
    }

    public override void NeighborUpdate(OnTickEvent @event) => BreakIfCannotGrow(@event);

    protected void BreakIfCannotGrow(OnTickEvent @event)
    {
        if (CanGrow(@event))
        {
            return;
        }

        DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    public override bool CanGrow(OnTickEvent @event) => CanPlaceAt(new CanPlaceAtContext(@event.World, 0, @event.X, @event.Y, @event.Z));

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override int GetDroppedItemId(int blockMeta) => Item.SugarCane.id;

    public override bool IsOpaque() => false;

    public override bool IsFullCube() => false;

    public override BlockRendererType GetRenderType() => BlockRendererType.Reed;
}
