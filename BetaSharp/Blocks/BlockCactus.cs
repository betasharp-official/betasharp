using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockCactus : Block
{
    private const float EdgeInset = 1.0F / 16.0F;

    public BlockCactus(int id, int textureId) : base(id, textureId, Material.Cactus) => setTickRandomly(true);

    public override void onTick(OnTickEvent @event)
    {
        if (!@event.World.Reader.IsAir(@event.X, @event.Y + 1, @event.Z)) return;

        int heightBelow = 1;
        while (@event.World.Reader.GetBlockId(@event.X, @event.Y - heightBelow, @event.Z) == id) heightBelow++;

        if (heightBelow >= 3) return;

        int growthStage = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if (growthStage == 15)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y + 1, @event.Z, id);
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, 0);
        }
        else
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, growthStage + 1);
        }
    }

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => new Box(x + EdgeInset, y, z + EdgeInset, x + 1 - EdgeInset, y + 1 - EdgeInset, z + 1 - EdgeInset);

    public override Box getBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z) => new(x + EdgeInset, y, z + EdgeInset, x + 1 - EdgeInset, y + 1, z + 1 - EdgeInset);

    public override int GetTexture(Side side) => side switch
    {
        Side.Up => BlockTextures.CactusTop,
        Side.Down => BlockTextures.CactusBottom,
        _ => BlockTextures.CactusSide
    };

    public override bool isFullCube() => false;

    public override bool isOpaque() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Cactus;

    public override bool canPlaceAt(CanPlaceAtContext evt) => base.canPlaceAt(evt) && canGrow(evt.World.Reader, evt.X, evt.Y, evt.Z);


    public override void neighborUpdate(OnTickEvent @event)
    {
        if (canGrow(@event)) return;
        dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    public override bool canGrow(OnTickEvent @event) => canGrow(@event.World.Reader, @event.X, @event.Y, @event.Z);

    private static bool canGrow(IBlockReader world, int x, int y, int z)
    {
        if (world.GetMaterial(x - 1, y, z).IsSolid) return false;
        if (world.GetMaterial(x + 1, y, z).IsSolid) return false;
        if (world.GetMaterial(x, y, z - 1).IsSolid) return false;
        if (world.GetMaterial(x, y, z + 1).IsSolid) return false;
        int blockBelowId = world.GetBlockId(x, y - 1, z);
        return blockBelowId == Cactus.id || blockBelowId == Sand.id;
    }

    public override void onEntityCollision(OnEntityCollisionEvent ctx) => ctx.Entity.Damage(null, 1);
}
