using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
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

    public override Box getBoundingBox(IBlockReader world, int x, int y, int z)
    {
        updateBoundingBox(world, x, y, z);
        return base.getBoundingBox(world, x, y, z);
    }

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z)
    {
        updateBoundingBox(world, x, y, z);
        return base.getCollisionShape(world, x, y, z);
    }

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z) => updateBoundingBox(iBlockReader.GetBlockMeta(x, y, z));

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

    public override void onBlockBreakStart(OnBlockBreakStartEvt ctx) => UpdateState(ctx.Level.Reader, ctx.Level.BlockWriter, ctx.Level.Broadcaster, ctx.X, ctx.Y, ctx.Z);


    public override bool onUse(OnUseEvt ctx) => UpdateState(ctx.Level.Reader, ctx.Level.BlockWriter, ctx.Level.Broadcaster, ctx.X, ctx.Y, ctx.Z);

    public void setOpen(OnTickEvt ctx, bool open)
    {
        int meta = ctx.Level.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        bool isOpen = (meta & 4) > 0;
        if (isOpen != open)
        {
            ctx.Level.BlockWriter.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, meta ^ 4);
            ctx.Level.Broadcaster.WorldEvent(1003, ctx.X, ctx.Y, ctx.Z, 0);
        }
    }

    public override void neighborUpdate(OnTickEvt ctx)
    {
        if (!ctx.Level.IsRemote)
        {
            int meta = ctx.Level.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
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

            if (!ctx.Level.Reader.ShouldSuffocate(xPos, ctx.Y, zPos))
            {
                ctx.Level.BlockWriter.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
                dropStacks(new OnDropEvt(ctx.Level, ctx.X, ctx.Y, ctx.Z, meta));
            }

            if (id > 0 && Blocks[id].canEmitRedstonePower())
            {
                bool isPowered = ctx.Level.Redstone.IsPowered(ctx.X, ctx.Y, ctx.Z);
                setOpen(ctx, isPowered);
            }
        }
    }

    public override HitResult raycast(IBlockReader world, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        updateBoundingBox(world, x, y, z);
        return base.raycast(world, x, y, z, startPos, endPos);
    }

    public override void onPlaced(OnPlacedEvt ctx)
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

        ctx.Level.BlockWriter.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, meta);
    }

    public override bool canPlaceAt(CanPlaceAtCtx ctx)
    {
        if (ctx.Direction == 0)
        {
            return false;
        }

        if (ctx.Direction == 1)
        {
            return false;
        }

        if (ctx.Direction == 2)
        {
            ++ctx.Z;
        }

        if (ctx.Direction == 3)
        {
            --ctx.Z;
        }

        if (ctx.Direction == 4)
        {
            ++ctx.X;
        }

        if (ctx.Direction == 5)
        {
            --ctx.X;
        }

        return ctx.Level.Reader.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z);
    }

    public static bool isOpen(int meta) => (meta & 4) != 0;
}
