using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

public class BlockBed : Block
{
    public static readonly int[][] BED_OFFSETS = [[0, 1], [-1, 0], [0, -1], [1, 0]];

    public BlockBed(int id) : base(id, 134, Material.Wool)
    {
        setDefaultShape();
    }

    public override bool onUse(OnUseContext ctx)
    {
        if (ctx.IsRemote)
        {
            return true;
        }
        else
        {
            int meta = ctx.WorldView.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
            if (!isHeadOfBed(meta))
            {
                int direction = getDirection(meta);
                ctx.X += BED_OFFSETS[direction][0];
                ctx.Z += BED_OFFSETS[direction][1];
                if (ctx.WorldView.GetBlockId(ctx.X, ctx.Y, ctx.Z) != id)
                {
                    return true;
                }

                meta = ctx.WorldView.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
            }

            if (!ctx.Dimension.HasWorldSpawn)
            {
                double posX = ctx.X + 0.5D;
                double posY = ctx.Y + 0.5D;
                double posZ = ctx.Z + 0.5D;
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
                int direction = getDirection(meta);
                ctx.X += BED_OFFSETS[direction][0];
                ctx.Z += BED_OFFSETS[direction][1];
                if (ctx.WorldView.GetBlockId(ctx.X, ctx.Y, ctx.Z) == id)
                {
                    ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
                    posX = (posX + ctx.X + 0.5) / 2.0;
                    posY = (posY + ctx.Y + 0.5) / 2.0;
                    posZ = (posZ + ctx.Z + 0.5) / 2.0;
                }

                ctx.CreateExplosion(null, ctx.X + 0.5F, ctx.Y + 0.5F, ctx.Z + 0.5F, 5.0F, true);
                return true;
            }
            else
            {
                if (isBedOccupied(meta))
                {
                    EntityPlayer? occupant = null;
                    foreach (var otherPlayer in ctx.Entities.Players) {
                        if (otherPlayer.isSleeping())
                        {
                            Vec3i sleepingPos = otherPlayer.sleepingPos;
                            if (sleepingPos.X == ctx.X && sleepingPos.Y == ctx.Y && sleepingPos.Z == ctx.Z)
                            {
                                occupant = otherPlayer;
                            }
                        }
                    }

                    if (occupant != null)
                    {
                       ctx.Player.sendMessage("tile.bed.occupied");
                        return true;
                    }

                    updateState(ctx, false);
                }

                SleepAttemptResult result = ctx.Player.trySleep(ctx.X, ctx.Y, ctx.Z);
                if (result == SleepAttemptResult.OK)
                {
                    updateState(ctx, true);
                    return true;
                }
                else
                {
                    if (result == SleepAttemptResult.NOT_POSSIBLE_NOW)
                    {
                        ctx.Player.sendMessage("tile.bed.noSleep");
                    }

                    return true;
                }
            }
        }
    }

    public override int getTexture(int side, int meta)
    {
        if (side == 0)
        {
            return Block.Planks.textureId;
        }
        else
        {
            int direction = getDirection(meta);
            int sideFacing = Facings.BED_FACINGS[direction][side];
            return isHeadOfBed(meta) ?
                (sideFacing == 2 ? textureId + 2 + 16 : (sideFacing != 5 && sideFacing != 4 ? textureId + 1 : textureId + 1 + 16)) :
                (sideFacing == 3 ? textureId - 1 + 16 : (sideFacing != 5 && sideFacing != 4 ? textureId : textureId + 16));
        }
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Bed;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z)
    {
        setDefaultShape();
    }

    public override void neighborUpdate(OnTickContext ctx)
    {
        int blockMeta = ctx.WorldView.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        int direction = getDirection(blockMeta);
        if (isHeadOfBed(blockMeta))
        {
            if (ctx.WorldView.GetBlockId(ctx.X - BED_OFFSETS[direction][0], ctx.Y, ctx.Z - BED_OFFSETS[direction][1]) != this.id)
            {
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            }
        }
        else if (ctx.WorldView.GetBlockId(ctx.X + BED_OFFSETS[direction][0], ctx.Y, ctx.Z + BED_OFFSETS[direction][1]) != this.id)
        {
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            if (!ctx.IsRemote)
            {
                dropStacks(ctx.WorldView, ctx.X, ctx.Y, ctx.Z, blockMeta);
            }
        }

    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return isHeadOfBed(blockMeta) ? 0 : Item.Bed.id;
    }

    private void setDefaultShape()
    {
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 9.0F / 16.0F, 1.0F);
    }

    public static int getDirection(int meta)
    {
        return meta & 3;
    }

    public static bool isHeadOfBed(int meta)
    {
        return (meta & 8) != 0;
    }

    public static bool isBedOccupied(int meta)
    {
        return (meta & 4) != 0;
    }

    public static void updateState(OnUseContext ctx, bool occupied)
    {
        int blockMeta = ctx.WorldView.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        if (occupied)
        {
            blockMeta |= 4;
        }
        else
        {
            blockMeta &= -5;
        }

        ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, blockMeta);
    }

    public static Vec3i? findWakeUpPosition(World world, int x, int y, int z, int skip)
    {
        int blockMeta = world.getBlockMeta(x, y, z);
        int direction = getDirection(blockMeta);

        for (int bedHalf = 0; bedHalf <= 1; ++bedHalf)
        {
            int searchMinX = x - BED_OFFSETS[direction][0] * bedHalf - 1;
            int searchMinZ = z - BED_OFFSETS[direction][1] * bedHalf - 1;
            int searchMaxX = searchMinX + 2;
            int searchMaxZ = searchMinZ + 2;

            for (int checkX = searchMinX; checkX <= searchMaxX; ++checkX)
            {
                for (int checkZ = searchMinZ; checkZ <= searchMaxZ; ++checkZ)
                {
                    if (world.shouldSuffocate(checkX, y - 1, checkZ) && world.isAir(checkX, y, checkZ) && world.isAir(checkX, y + 1, checkZ))
                    {
                        if (skip <= 0)
                        {
                            return new Vec3i(checkX, y, checkZ);
                        }

                        --skip;
                    }
                }
            }
        }

        return null;
    }

    public override void dropStacks(WorldBlockView world, int x, int y, int z, int meta, float luck)
    {
        if (!isHeadOfBed(meta))
        {
            base.dropStacks(world, x, y, z, meta, luck);
        }

    }

    public override int getPistonBehavior()
    {
        return 1;
    }
}
