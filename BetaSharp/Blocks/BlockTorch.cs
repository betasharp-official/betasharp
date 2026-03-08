using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockTorch : Block
{
    public BlockTorch(int id, int textureId) : base(id, textureId, Material.PistonBreakable) => setTickRandomly(true);

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z) => null;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Torch;

    private bool canPlaceOn(IBlockReader world, int x, int y, int z) => world.ShouldSuffocate(x, y, z) || world.GetBlockId(x, y, z) == Fence.id;

    public override bool canPlaceAt(IBlockReader world, int x, int y, int z) =>
        world.ShouldSuffocate(x - 1, y, z) ||
        world.ShouldSuffocate(x + 1, y, z) ||
        world.ShouldSuffocate(x, y, z - 1) ||
        world.ShouldSuffocate(x, y, z + 1) ||
        canPlaceOn(world, x, y - 1, z);

    public override void onPlaced(OnPlacedEvt ctx)
    {
        int meta = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        if (ctx.Direction == 1 && canPlaceOn(ctx.WorldRead, ctx.X, ctx.Y - 1, ctx.Z))
        {
            meta = 5;
        }

        if (ctx.Direction == 2 && ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1))
        {
            meta = 4;
        }

        if (ctx.Direction == 3 && ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1))
        {
            meta = 3;
        }

        if (ctx.Direction == 4 && ctx.WorldRead.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z))
        {
            meta = 2;
        }

        if (ctx.Direction == 5 && ctx.WorldRead.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z))
        {
            meta = 1;
        }

        ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, meta);
    }

    public override void onTick(OnTickEvt ctx)
    {
        base.onTick(ctx);
        if (ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z) == 0)
        {
            onPlaced(ctx);
        }
    }

    public virtual void onPlaced(OnTickEvt ctx)
    {
        if (ctx.WorldRead.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z))
        {
            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, 1);
        }
        else if (ctx.WorldRead.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z))
        {
            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, 2);
        }
        else if (ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1))
        {
            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, 3);
        }
        else if (ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1))
        {
            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, 4);
        }
        else if (canPlaceOn(ctx.WorldRead, ctx.X, ctx.Y - 1, ctx.Z))
        {
            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, 5);
        }

        breakIfCannotPlaceAt(ctx.WorldRead, ctx.WorldWrite, ctx.X, ctx.Y, ctx.Z);
    }

    public override void neighborUpdate(OnTickEvt ctx)
    {
        if (breakIfCannotPlaceAt(ctx.WorldRead, ctx.WorldWrite, ctx.X, ctx.Y, ctx.Z))
        {
            int meta = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
            bool shouldDrop = false;

            if (!ctx.WorldRead.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z) && meta == 1)
            {
                shouldDrop = true;
            }

            if (!ctx.WorldRead.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z) && meta == 2)
            {
                shouldDrop = true;
            }

            if (!ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1) && meta == 3)
            {
                shouldDrop = true;
            }

            if (!ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1) && meta == 4)
            {
                shouldDrop = true;
            }

            if (!canPlaceOn(ctx.WorldRead, ctx.X, ctx.Y - 1, ctx.Z) && meta == 5)
            {
                shouldDrop = true;
            }

            if (shouldDrop)
            {
                dropStacks(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z, ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z));
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            }
        }
    }

    private bool breakIfCannotPlaceAt(IBlockReader worldRead, IBlockWrite worldWrite, int x, int y, int z)
    {
        if (!canPlaceAt(worldRead, x, y, z))
        {
            dropStacks(worldRead, x, y, z, worldRead.GetBlockMeta(x, y, z));
            worldWrite.SetBlock(x, y, z, 0);
            return false;
        }

        return true;
    }

    public override HitResult raycast(IBlockReader world, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        int meta = world.GetBlockMeta(x, y, z) & 7;
        float torchWidth = 0.15F;
        if (meta == 1)
        {
            setBoundingBox(0.0F, 0.2F, 0.5F - torchWidth, torchWidth * 2.0F, 0.8F, 0.5F + torchWidth);
        }
        else if (meta == 2)
        {
            setBoundingBox(1.0F - torchWidth * 2.0F, 0.2F, 0.5F - torchWidth, 1.0F, 0.8F, 0.5F + torchWidth);
        }
        else if (meta == 3)
        {
            setBoundingBox(0.5F - torchWidth, 0.2F, 0.0F, 0.5F + torchWidth, 0.8F, torchWidth * 2.0F);
        }
        else if (meta == 4)
        {
            setBoundingBox(0.5F - torchWidth, 0.2F, 1.0F - torchWidth * 2.0F, 0.5F + torchWidth, 0.8F, 1.0F);
        }
        else
        {
            torchWidth = 0.1F;
            setBoundingBox(0.5F - torchWidth, 0.0F, 0.5F - torchWidth, 0.5F + torchWidth, 0.6F, 0.5F + torchWidth);
        }

        return base.raycast(world, x, y, z, startPos, endPos);
    }

    public override void randomDisplayTick(OnTickEvt ctx)
    {
        int meta = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        float flameX = ctx.X + 0.5F;
        float flameY = ctx.Y + 0.7F;
        float flameZ = ctx.Z + 0.5F;
        float yOffset = 0.22F;
        float xOffset = 0.27F;

        if (meta == 1)
        {
            ctx.Broadcaster.AddParticle("smoke", flameX - xOffset, flameY + yOffset, flameZ, 0.0D, 0.0D, 0.0D);
            ctx.Broadcaster.AddParticle("flame", flameX - xOffset, flameY + yOffset, flameZ, 0.0D, 0.0D, 0.0D);
        }
        else if (meta == 2)
        {
            ctx.Broadcaster.AddParticle("smoke", flameX + xOffset, flameY + yOffset, flameZ, 0.0D, 0.0D, 0.0D);
            ctx.Broadcaster.AddParticle("flame", flameX + xOffset, flameY + yOffset, flameZ, 0.0D, 0.0D, 0.0D);
        }
        else if (meta == 3)
        {
            ctx.Broadcaster.AddParticle("smoke", flameX, flameY + yOffset, flameZ - xOffset, 0.0D, 0.0D, 0.0D);
            ctx.Broadcaster.AddParticle("flame", flameX, flameY + yOffset, flameZ - xOffset, 0.0D, 0.0D, 0.0D);
        }
        else if (meta == 4)
        {
            ctx.Broadcaster.AddParticle("smoke", flameX, flameY + yOffset, flameZ + xOffset, 0.0D, 0.0D, 0.0D);
            ctx.Broadcaster.AddParticle("flame", flameX, flameY + yOffset, flameZ + xOffset, 0.0D, 0.0D, 0.0D);
        }
        else
        {
            ctx.Broadcaster.AddParticle("smoke", flameX, flameY, flameZ, 0.0D, 0.0D, 0.0D);
            ctx.Broadcaster.AddParticle("flame", flameX, flameY, flameZ, 0.0D, 0.0D, 0.0D);
        }
    }
}
