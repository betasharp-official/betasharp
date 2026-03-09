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

    public override bool canPlaceAt(CanPlaceAtCtx ctx) =>
        ctx.Level.BlocksReader.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z) ||
        ctx.Level.BlocksReader.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z) ||
        ctx.Level.BlocksReader.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1) ||
        ctx.Level.BlocksReader.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1) ||
        canPlaceOn(ctx.Level.BlocksReader, ctx.X, ctx.Y - 1, ctx.Z);

    public override void onPlaced(OnPlacedEvt evt)
    {
        int meta = evt.Level.BlocksReader.GetBlockMeta(evt.X, evt.Y, evt.Z);
        if (evt.Direction == 1 && canPlaceOn(evt.Level.BlocksReader, evt.X, evt.Y - 1, evt.Z))
        {
            meta = 5;
        }

        if (evt.Direction == 2 && evt.Level.BlocksReader.ShouldSuffocate(evt.X, evt.Y, evt.Z + 1))
        {
            meta = 4;
        }

        if (evt.Direction == 3 && evt.Level.BlocksReader.ShouldSuffocate(evt.X, evt.Y, evt.Z - 1))
        {
            meta = 3;
        }

        if (evt.Direction == 4 && evt.Level.BlocksReader.ShouldSuffocate(evt.X + 1, evt.Y, evt.Z))
        {
            meta = 2;
        }

        if (evt.Direction == 5 && evt.Level.BlocksReader.ShouldSuffocate(evt.X - 1, evt.Y, evt.Z))
        {
            meta = 1;
        }

        evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, meta);
    }

    public override void onTick(OnTickEvt evt)
    {
        base.onTick(evt);
        if (evt.Level.BlocksReader.GetBlockMeta(evt.X, evt.Y, evt.Z) == 0)
        {
            onPlaced(evt);
        }
    }

    public virtual void onPlaced(OnTickEvt evt)
    {
        if (evt.Level.BlocksReader.ShouldSuffocate(evt.X - 1, evt.Y, evt.Z))
        {
            evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, 1);
        }
        else if (evt.Level.BlocksReader.ShouldSuffocate(evt.X + 1, evt.Y, evt.Z))
        {
            evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, 2);
        }
        else if (evt.Level.BlocksReader.ShouldSuffocate(evt.X, evt.Y, evt.Z - 1))
        {
            evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, 3);
        }
        else if (evt.Level.BlocksReader.ShouldSuffocate(evt.X, evt.Y, evt.Z + 1))
        {
            evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, 4);
        }
        else if (canPlaceOn(evt.Level.BlocksReader, evt.X, evt.Y - 1, evt.Z))
        {
            evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, 5);
        }

        breakIfCannotPlaceAt(evt, evt.X, evt.Y, evt.Z);
    }

    public override void neighborUpdate(OnTickEvt evt)
    {
        if (breakIfCannotPlaceAt(evt, evt.X, evt.Y, evt.Z))
        {
            int meta = evt.Level.BlocksReader.GetBlockMeta(evt.X, evt.Y, evt.Z);
            bool shouldDrop = false;

            if (!evt.Level.BlocksReader.ShouldSuffocate(evt.X - 1, evt.Y, evt.Z) && meta == 1)
            {
                shouldDrop = true;
            }

            if (!evt.Level.BlocksReader.ShouldSuffocate(evt.X + 1, evt.Y, evt.Z) && meta == 2)
            {
                shouldDrop = true;
            }

            if (!evt.Level.BlocksReader.ShouldSuffocate(evt.X, evt.Y, evt.Z - 1) && meta == 3)
            {
                shouldDrop = true;
            }

            if (!evt.Level.BlocksReader.ShouldSuffocate(evt.X, evt.Y, evt.Z + 1) && meta == 4)
            {
                shouldDrop = true;
            }

            if (!canPlaceOn(evt.Level.BlocksReader, evt.X, evt.Y - 1, evt.Z) && meta == 5)
            {
                shouldDrop = true;
            }

            if (shouldDrop)
            {
                dropStacks(new OnDropEvt(evt.Level, evt.X, evt.Y, evt.Z, evt.Level.BlocksReader.GetBlockMeta(evt.X, evt.Y, evt.Z)));
                evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, 0);
            }
        }
    }

    private bool breakIfCannotPlaceAt(OnTickEvt evt, int x, int y, int z)
    {
        if (!canPlaceAt(new CanPlaceAtCtx(evt.Level, 0, x, y, z)))
        {
            dropStacks(new OnDropEvt(evt.Level, x, y, z, evt.Level.BlocksReader.GetBlockMeta(x, y, z)));
            evt.Level.BlockWriter.SetBlock(x, y, z, 0);
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

    public override void randomDisplayTick(OnTickEvt evt)
    {
        int meta = evt.Level.BlocksReader.GetBlockMeta(evt.X, evt.Y, evt.Z);
        float flameX = evt.X + 0.5F;
        float flameY = evt.Y + 0.7F;
        float flameZ = evt.Z + 0.5F;
        float yOffset = 0.22F;
        float xOffset = 0.27F;

        if (meta == 1)
        {
            evt.Level.Broadcaster.AddParticle("smoke", flameX - xOffset, flameY + yOffset, flameZ, 0.0D, 0.0D, 0.0D);
            evt.Level.Broadcaster.AddParticle("flame", flameX - xOffset, flameY + yOffset, flameZ, 0.0D, 0.0D, 0.0D);
        }
        else if (meta == 2)
        {
            evt.Level.Broadcaster.AddParticle("smoke", flameX + xOffset, flameY + yOffset, flameZ, 0.0D, 0.0D, 0.0D);
            evt.Level.Broadcaster.AddParticle("flame", flameX + xOffset, flameY + yOffset, flameZ, 0.0D, 0.0D, 0.0D);
        }
        else if (meta == 3)
        {
            evt.Level.Broadcaster.AddParticle("smoke", flameX, flameY + yOffset, flameZ - xOffset, 0.0D, 0.0D, 0.0D);
            evt.Level.Broadcaster.AddParticle("flame", flameX, flameY + yOffset, flameZ - xOffset, 0.0D, 0.0D, 0.0D);
        }
        else if (meta == 4)
        {
            evt.Level.Broadcaster.AddParticle("smoke", flameX, flameY + yOffset, flameZ + xOffset, 0.0D, 0.0D, 0.0D);
            evt.Level.Broadcaster.AddParticle("flame", flameX, flameY + yOffset, flameZ + xOffset, 0.0D, 0.0D, 0.0D);
        }
        else
        {
            evt.Level.Broadcaster.AddParticle("smoke", flameX, flameY, flameZ, 0.0D, 0.0D, 0.0D);
            evt.Level.Broadcaster.AddParticle("flame", flameX, flameY, flameZ, 0.0D, 0.0D, 0.0D);
        }
    }
}
