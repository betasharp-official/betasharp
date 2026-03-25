using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockTrapDoor : Block
{
    public BlockTrapDoor(int id, Material material) : base(id, material)
    {
        const float halfWidth = 0.5F;
        const float fullHeight = 1.0F;
        TextureId = 84;
        if (material == Material.Metal)
        {
            ++TextureId;
        }

        SetBoundingBox(0.5F - halfWidth, 0.0F, 0.5F - halfWidth, 0.5F + halfWidth, fullHeight, 0.5F + halfWidth);
    }

    public override bool IsOpaque() => false;

    public override bool IsFullCube() => false;

    public override BlockRendererType GetRenderType() => BlockRendererType.Standard;

    public override Box GetBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        UpdateBoundingBox(world, entities, x, y, z);
        return base.GetBoundingBox(world, entities, x, y, z);
    }

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        UpdateBoundingBox(world, entities, x, y, z);
        return base.GetCollisionShape(world, entities, x, y, z);
    }

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z) => updateBoundingBox(blockReader.GetBlockMeta(x, y, z));

    public override void SetupRenderBoundingBox()
    {
        float height = 3.0F / 16.0F;
        SetBoundingBox(0.0F, 0.5F - height / 2.0F, 0.0F, 1.0F, 0.5F + height / 2.0F, 1.0F);
    }

    public void updateBoundingBox(int meta)
    {
        const float height = 3.0F / 16.0F;
        SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, height, 1.0F);
        if (!isOpen(meta))
        {
            return;
        }

        if ((meta & 3) == 0)
        {
            SetBoundingBox(0.0F, 0.0F, 1.0F - height, 1.0F, 1.0F, 1.0F);
        }

        if ((meta & 3) == 1)
        {
            SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, height);
        }

        if ((meta & 3) == 2)
        {
            SetBoundingBox(1.0F - height, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        }

        if ((meta & 3) == 3)
        {
            SetBoundingBox(0.0F, 0.0F, 0.0F, height, 1.0F, 1.0F);
        }
    }

    private bool UpdateState(IBlockReader worldRead, IBlockWriter worldWriter, WorldEventBroadcaster broadcaster, int x, int y, int z)
    {
        if (Material == Material.Metal)
        {
            return true;
        }

        int meta = worldRead.GetBlockMeta(x, y, z);
        worldWriter.SetBlockMeta(x, y, z, meta ^ 4);
        broadcaster.WorldEvent(1003, x, y, z, 0);
        return true;
    }

    public override void OnBlockBreakStart(OnBlockBreakStartEvent ctx) => UpdateState(ctx.World.Reader, ctx.World.Writer, ctx.World.Broadcaster, ctx.X, ctx.Y, ctx.Z);


    public override bool OnUse(OnUseEvent ctx) => UpdateState(ctx.World.Reader, ctx.World.Writer, ctx.World.Broadcaster, ctx.X, ctx.Y, ctx.Z);

    public void setOpen(OnTickEvent ctx, bool open)
    {
        int x = ctx.X;
        int y = ctx.Y;
        int z = ctx.Z;
        int meta = ctx.World.Reader.GetBlockMeta(x, y, z);
        bool isOpen = (meta & 4) > 0;
        if (isOpen == open)
        {
            return;
        }

        ctx.World.Writer.SetBlockMeta(x, y, z, meta ^ 4);
        ctx.World.Broadcaster.WorldEvent(1003, x, y, z, 0);
    }

    public override void NeighborUpdate(OnTickEvent ctx)
    {
        if (ctx.World.IsRemote)
        {
            return;
        }

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
            DropStacks(new OnDropEvent(ctx.World, ctx.X, ctx.Y, ctx.Z, meta));
        }

        if (Id <= 0 || !Blocks[Id]!.CanEmitRedstonePower())
        {
            return;
        }

        bool isPowered = ctx.World.Redstone.IsPowered(ctx.X, ctx.Y, ctx.Z);
        setOpen(ctx, isPowered);
    }

    public override HitResult Raycast(IBlockReader world, EntityManager entities, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        UpdateBoundingBox(world, entities, x, y, z);
        return base.Raycast(world, entities, x, y, z, startPos, endPos);
    }

    public override void OnPlaced(OnPlacedEvent ctx)
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

    public override bool CanPlaceAt(CanPlaceAtContext context)
    {
        int x = context.X;
        int y = context.Y;
        int z = context.Z;

        switch (context.Direction)
        {
            case Side.Down:
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

        return context.World.Reader.ShouldSuffocate(x, y, z);
    }

    public static bool isOpen(int meta) => (meta & 4) != 0;
}
