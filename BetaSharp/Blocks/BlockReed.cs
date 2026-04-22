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
        setBoundingBox(0.5F - HalfWidth, 0.0F, 0.5F - HalfWidth, 0.5F + HalfWidth, 1.0F, 0.5F + HalfWidth);
        setTickRandomly(true);
    }

    public override void onTick(OnTickEvent @event)
    {
        if (!@event.World.Reader.IsAir(@event.X, @event.Y + 1, @event.Z)) return;

        int heightBelow = 1;
        while (@event.World.Reader.GetBlockId(@event.X, @event.Y - heightBelow, @event.Z) == id)
        {
            heightBelow++;
        }

        if (heightBelow >= 3) return;

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if (meta == 15)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y + 1, @event.Z, id);
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, 0);
        }
        else
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, meta + 1);
        }
    }

    public override bool canPlaceAt(CanPlaceAtContext evt)
    {
        int blockBelowId = evt.World.Reader.GetBlockId(evt.X, evt.Y - 1, evt.Z);
        return blockBelowId == id ||
               (blockBelowId == GrassBlock.id ||
                blockBelowId == Dirt.id) && (evt.World.Reader.GetMaterial(evt.X - 1, evt.Y - 1, evt.Z) == Material.Water ||
                                             evt.World.Reader.GetMaterial(evt.X + 1, evt.Y - 1, evt.Z) == Material.Water ||
                                             evt.World.Reader.GetMaterial(evt.X, evt.Y - 1, evt.Z - 1) == Material.Water ||
                                             evt.World.Reader.GetMaterial(evt.X, evt.Y - 1, evt.Z + 1) == Material.Water);
    }

    public override void neighborUpdate(OnTickEvent @event) => breakIfCannotGrow(@event);

    protected void breakIfCannotGrow(OnTickEvent @event)
    {
        if (canGrow(@event)) return;

        dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    public override bool canGrow(OnTickEvent @event) => canPlaceAt(new CanPlaceAtContext(@event.World, 0, @event.X, @event.Y, @event.Z));

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override int getDroppedItemId(int blockMeta) => Item.SugarCane.id;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Reed;
}
