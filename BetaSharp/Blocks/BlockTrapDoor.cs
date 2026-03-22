using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockTrapDoor : Block
{
    public BlockTrapDoor(int id, Material material) : base(id, material)
    {
        textureId = 84;
        if (material == Material.Metal)
        {
            ++textureId;
        }

        float halfWidth = 0.5F;
        float fullHeight = 1.0F;
        setBoundingBox(0.5F - halfWidth, 0.0F, 0.5F - halfWidth, 0.5F + halfWidth, fullHeight, 0.5F + halfWidth);
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

    public void updateBoundingBox(int meta)
    {
        float height = 3.0F / 16.0F;
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, height, 1.0F);
        if (isOpen(meta))
        {
            if ((meta & 3) == 0)
            {
                setBoundingBox(0.0F, 0.0F, 1.0F - height, 1.0F, 1.0F, 1.0F);
            }

            if ((meta & 3) == 1)
            {
                setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, height);
            }

            if ((meta & 3) == 2)
            {
                setBoundingBox(1.0F - height, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
            }

            if ((meta & 3) == 3)
            {
                setBoundingBox(0.0F, 0.0F, 0.0F, height, 1.0F, 1.0F);
            }
        }
    }

    private bool UpdateState(IBlockReader worldRead, IBlockWrite worldWrite, WorldEventBroadcaster broadcaster, int x, int y, int z)
    {
        if (material == Material.Metal)
        {
            return true;
        }

        int meta = worldRead.GetBlockMeta(x, y, z);
        worldWrite.SetBlockMeta(x, y, z, meta ^ 4);
        broadcaster.WorldEvent(1003, x, y, z, 0);
        return true;
    }

    public override void onBlockBreakStart(OnBlockBreakStartEvent ctx) => UpdateState(ctx.World.Reader, ctx.World.Writer, ctx.World.Broadcaster, ctx.X, ctx.Y, ctx.Z);


    public override bool onUse(OnUseEvent ctx) => UpdateState(ctx.World.Reader, ctx.World.Writer, ctx.World.Broadcaster, ctx.X, ctx.Y, ctx.Z);

    public void setOpen(OnTickEvent ctx, bool open)
    {
        int x = ctx.X;
        int y = ctx.Y;
        int z = ctx.Z;
        int meta = ctx.World.Reader.GetBlockMeta(x, y, z);
        bool isOpen = (meta & 4) > 0;
        if (isOpen != open)
        {
            ctx.World.Writer.SetBlockMeta(x, y, z, meta ^ 4);
            ctx.World.Broadcaster.WorldEvent(1003, x, y, z, 0);
        }
    }

    public override void neighborUpdate(OnTickEvent ctx)
    {
        if (!ctx.World.IsRemote)
        {
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

            if (id > 0 && Blocks[id].canEmitRedstonePower())
            {
                bool isPowered = ctx.World.Redstone.IsPowered(ctx.X, ctx.Y, ctx.Z);
                setOpen(ctx, isPowered);
            }
        }
    }

    public override HitResult raycast(IBlockReader world, EntityManager entities, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        updateBoundingBox(world, entities, x, y, z);
        return base.raycast(world, entities, x, y, z, startPos, endPos);
    }

    public override void onPlaced(OnPlacedEvent ctx)
    {
        sbyte meta = 0;
        if (ctx.Direction == 2)
        {
            meta = 0;
        }

        if (ctx.Direction == 3)
        {
            meta = 1;
        }

        if (ctx.Direction == 4)
        {
            meta = 2;
        }

        if (ctx.Direction == 5)
        {
            meta = 3;
        }

        ctx.World.Writer.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, meta);
    }

    public override bool canPlaceAt(CanPlaceAtContext context)
    {
        int x = context.X;
        int y = context.Y;
        int z = context.Z;

        if (context.Direction == 0)
        {
            return false;
        }

        if (context.Direction == 1)
        {
            return false;
        }

        if (context.Direction == 2)
        {
            ++z;
        }

        if (context.Direction == 3)
        {
            --z;
        }

        if (context.Direction == 4)
        {
            ++x;
        }

        if (context.Direction == 5)
        {
            --x;
        }

        return context.World.Reader.ShouldSuffocate(x, y, z);
    }

    public static bool isOpen(int meta) => (meta & 4) != 0;
}
