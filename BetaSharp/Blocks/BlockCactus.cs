using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockCactus : Block
{
    private const float EdgeInset = 1.0F / 16.0F;

    public BlockCactus(int id, int textureId) : base(id, textureId, Material.Cactus) => SetTickRandomly(true);

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

        int growthStage = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if (growthStage == 15)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y + 1, @event.Z, ID);
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, 0);
        }
        else
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, growthStage + 1);
        }
    }

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => new Box(x + EdgeInset, y, z + EdgeInset, x + 1 - EdgeInset, y + 1 - EdgeInset, z + 1 - EdgeInset);

    public override Box GetBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z) => new(x + EdgeInset, y, z + EdgeInset, x + 1 - EdgeInset, y + 1, z + 1 - EdgeInset);

    public override int GetTexture(Side side) => side switch
    {
        Side.Up => BlockTextures.CactusTop,
        Side.Down => BlockTextures.CactusBottom,
        _ => BlockTextures.CactusSide
    };

    public override bool IsFullCube() => false;

    public override bool IsOpaque() => false;

    public override BlockRendererType GetRenderType() => BlockRendererType.Cactus;

    public override bool CanPlaceAt(CanPlaceAtContext evt) => base.CanPlaceAt(evt) && CanGrow(evt.World.Reader, evt.X, evt.Y, evt.Z);


    public override void NeighborUpdate(OnTickEvent @event)
    {
        if (CanGrow(@event))
        {
            return;
        }

        DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    public override bool CanGrow(OnTickEvent @event) => CanGrow(@event.World.Reader, @event.X, @event.Y, @event.Z);

    private static bool CanGrow(IBlockReader world, int x, int y, int z)
    {
        if (world.GetMaterial(x - 1, y, z).IsSolid)
        {
            return false;
        }

        if (world.GetMaterial(x + 1, y, z).IsSolid)
        {
            return false;
        }

        if (world.GetMaterial(x, y, z - 1).IsSolid)
        {
            return false;
        }

        if (world.GetMaterial(x, y, z + 1).IsSolid)
        {
            return false;
        }

        int blockBelowId = world.GetBlockId(x, y - 1, z);
        return blockBelowId == Cactus.ID || blockBelowId == Sand.ID;
    }

    public override void OnEntityCollision(OnEntityCollisionEvent ctx) => ctx.Entity.Damage(null, 1);
}
