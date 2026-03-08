using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockCactus : Block
{
    public BlockCactus(int id, int textureId) : base(id, textureId, Material.Cactus) => setTickRandomly(true);

    public override void onTick(OnTickEvt ctx)
    {
        if (ctx.WorldRead.IsAir(ctx.X, ctx.Y + 1, ctx.Z))
        {
            int heightBelow;
            for (heightBelow = 1; ctx.WorldRead.GetBlockId(ctx.X, ctx.Y - heightBelow, ctx.Z) == id; ++heightBelow)
            {
            }

            if (heightBelow < 3)
            {
                int growthStage = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
                if (growthStage == 15)
                {
                    ctx.WorldWrite.SetBlock(ctx.X, ctx.Y + 1, ctx.Z, id);
                    ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, 0);
                }
                else
                {
                    ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, growthStage + 1);
                }
            }
        }
    }

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z)
    {
        float edgeInset = 1.0F / 16.0F;
        return new Box(x + edgeInset, y, z + edgeInset, x + 1 - edgeInset, y + 1 - edgeInset, z + 1 - edgeInset);
    }

    public override Box getBoundingBox(IBlockReader world, int x, int y, int z)
    {
        float edgeInset = 1.0F / 16.0F;
        return new Box(x + edgeInset, y, z + edgeInset, x + 1 - edgeInset, y + 1, z + 1 - edgeInset);
    }

    public override int getTexture(int side) => side == 1 ? textureId - 1 : side == 0 ? textureId + 1 : textureId;

    public override bool isFullCube() => false;

    public override bool isOpaque() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Cactus;

    public override bool canPlaceAt(CanPlaceAtCtx ctx) => !base.canPlaceAt(ctx) ? false : canGrow(new CanPlaceAtCtx(ctx.WorldRead, ctx.WorldWrite, 0, ctx.X, ctx.Y, ctx.Z));

    public override void neighborUpdate(OnTickEvt ctx)
    {
        if (!canGrow(ctx))
        {
            // TODO: Implement this
            // dropStacks(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z, ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z));
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }
    }

    public override bool canGrow(OnTickEvt ctx) => canGrow(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z);

    private static bool canGrow(WorldBlockView world, int x, int y, int z)
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
        return blockBelowId == Cactus.id || blockBelowId == Sand.id;
    }

    public override void onEntityCollision(OnEntityCollisionEvt ctx) => ctx.Entity.damage(null, 1);
}
