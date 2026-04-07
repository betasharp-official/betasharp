using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockTrapDoor : Block
{
    private const float HalfWidth = 0.5F;
    private const float FullHeight = 1.0F;
    private const float Thickness = 3.0F / 16.0F;
    public BlockTrapDoor(int id, Material material) : base(id, material)
    {
        TextureId = 84;
        if (material == Material.Metal) ++TextureId;
        setBoundingBox(0.5F - HalfWidth, 0.0F, 0.5F - HalfWidth, 0.5F + HalfWidth, FullHeight, 0.5F + HalfWidth);
    }

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Standard;

    public override Box getBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        updateBoundingBox(world, entities, x, y, z);
        return base.getBoundingBox(world, entities, x, y, z);
    }

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        updateBoundingBox(world, entities, x, y, z);
        return base.getCollisionShape(world, entities, x, y, z);
    }

    public override void updateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z) => updateBoundingBox(blockReader.GetBlockMeta(x, y, z));

    public override void setupRenderBoundingBox()
    {
        float height = 3.0F / 16.0F;
        setBoundingBox(0.0F, 0.5F - height / 2.0F, 0.0F, 1.0F, 0.5F + height / 2.0F, 1.0F);
    }

    private void updateBoundingBox(int meta)
    {
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, Thickness, 1.0F);

        if (!IsOpen(meta)) return;

        if ((meta & 3) == 0) setBoundingBox(0.0F, 0.0F, 1.0F - Thickness, 1.0F, 1.0F, 1.0F);
        if ((meta & 3) == 1) setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, Thickness);
        if ((meta & 3) == 2) setBoundingBox(1.0F - Thickness, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        if ((meta & 3) == 3) setBoundingBox(0.0F, 0.0F, 0.0F, Thickness, 1.0F, 1.0F);
    }

    private bool UpdateState(IWorldContext world, WorldEventBroadcaster broadcaster, int x, int y, int z)
    {
        if (world.IsRemote) return true;
        if (material == Material.Metal) return true;

        int meta = world.Reader.GetBlockMeta(x, y, z);
        world.Writer.SetBlockMeta(x, y, z, meta ^ 4);
        broadcaster.WorldEvent(1003, x, y, z, 0);
        return true;
    }

    public override void onBlockBreakStart(OnBlockBreakStartEvent ctx) => UpdateState(ctx.World, ctx.World.Broadcaster, ctx.X, ctx.Y, ctx.Z);


    public override bool onUse(OnUseEvent ctx) => UpdateState(ctx.World, ctx.World.Broadcaster, ctx.X, ctx.Y, ctx.Z);

    private static void SetOpen(OnTickEvent ctx, bool open)
    {
        if (ctx.World.IsRemote) return;

        (int x, int y, int z) = (ctx.X, ctx.Y, ctx.Z);
        int meta = ctx.World.Reader.GetBlockMeta(x, y, z);

        bool isOpen = (meta & 4) > 0;
        if (isOpen == open) return;

        ctx.World.Writer.SetBlockMeta(x, y, z, meta ^ 4);
        ctx.World.Broadcaster.WorldEvent(1003, x, y, z, 0);
    }

    public override void neighborUpdate(OnTickEvent ctx)
    {
        if (ctx.World.IsRemote) return;

        int meta = ctx.World.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        int xPos = ctx.X;
        int zPos = ctx.Z;
        if ((meta & 3) == 0)
        {
            zPos = ctx.Z + 1;
        }

        if ((meta & 3) == 1)
        {
            --zPos;
        }

        if ((meta & 3) == 2)
        {
            xPos = ctx.X + 1;
        }

        if ((meta & 3) == 3)
        {
            --xPos;
        }

        if (!ctx.World.Reader.ShouldSuffocate(xPos, ctx.Y, zPos))
        {
            ctx.World.Writer.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            dropStacks(new OnDropEvent(ctx.World, ctx.X, ctx.Y, ctx.Z, meta));
        }
        else
        {
            bool isPowered = ctx.World.Redstone.IsPowered(ctx.X, ctx.Y, ctx.Z);
            SetOpen(ctx, isPowered);
        }
    }

    public override HitResult raycast(IBlockReader world, EntityManager entities, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        updateBoundingBox(world, entities, x, y, z);
        return base.raycast(world, entities, x, y, z, startPos, endPos);
    }

    public override void onPlaced(OnPlacedEvent ctx)
    {
        sbyte meta = ctx.Direction switch
        {
            Side.North => 0,
            Side.South => 1,
            Side.West => 2,
            Side.East => 3,
            _ => 0
        };

        ctx.World.Writer.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, meta);
    }

    public override bool canPlaceAt(CanPlaceAtContext ctx)
    {
        (int x, int y, int z) = (ctx.X, ctx.Y, ctx.Z);

        switch (ctx.Direction)
        {
            case 0:
            case Side.Up:
                return false;
            case Side.North:
                ++z;
                break;
            case Side.South:
                --z;
                break;
            case Side.West:
                ++x;
                break;
            case Side.East:
                --x;
                break;
        }

        return ctx.World.Reader.ShouldSuffocate(x, y, z);
    }

    public static bool IsOpen(int meta) => (meta & 4) != 0;
}
