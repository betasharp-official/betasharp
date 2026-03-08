using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

public class BlockBed : Block
{
    public static readonly int[][] BED_OFFSETS = [[0, 1], [-1, 0], [0, -1], [1, 0]];

    public BlockBed(int id) : base(id, 134, Material.Wool) => setDefaultShape();

    public override bool onUse(OnUseEvt ctx)
    {
        if (ctx.IsRemote)
        {
            return true;
        }

        // Extract to local variables so we don't mutate the event struct
        int x = ctx.X;
        int y = ctx.Y;
        int z = ctx.Z;

        int meta = ctx.WorldRead.GetBlockMeta(x, y, z);
        if (!isHeadOfBed(meta))
        {
            int direction = getDirection(meta);
            x += BED_OFFSETS[direction][0];
            z += BED_OFFSETS[direction][1];

            if (ctx.WorldRead.GetBlockId(x, y, z) != id)
            {
                return true;
            }

            meta = ctx.WorldRead.GetBlockMeta(x, y, z);
        }

        if (!ctx.Dimension.HasWorldSpawn)
        {
            double posX = x + 0.5D;
            double posY = y + 0.5D;
            double posZ = z + 0.5D;
            ctx.WorldWrite.SetBlock(x, y, z, 0);

            int direction = getDirection(meta);
            x += BED_OFFSETS[direction][0];
            z += BED_OFFSETS[direction][1];

            if (ctx.WorldRead.GetBlockId(x, y, z) == id)
            {
                ctx.WorldWrite.SetBlock(x, y, z, 0);
                posX = (posX + x + 0.5D) / 2.0D;
                posY = (posY + y + 0.5D) / 2.0D;
                posZ = (posZ + z + 0.5D) / 2.0D;
            }

            //ctx.Broadcaster.CreateExplosion(null, x + 0.5F, y + 0.5F, z + 0.5F, 5.0F, true);
            return true;
        }

        if (isBedOccupied(meta))
        {
            EntityPlayer? occupant = null;
            foreach (EntityPlayer otherPlayer in ctx.Entities.Players)
            {
                if (otherPlayer.isSleeping())
                {
                    Vec3i sleepingPos = otherPlayer.sleepingPos;
                    if (sleepingPos.X == x && sleepingPos.Y == y && sleepingPos.Z == z)
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

            updateState(ctx.WorldWrite, x, y, z, meta, false);
        }

        SleepAttemptResult result = ctx.Player.trySleep(x, y, z);
        if (result == SleepAttemptResult.OK)
        {
            updateState(ctx.WorldWrite, x, y, z, meta, true);
            return true;
        }

        if (result == SleepAttemptResult.NOT_POSSIBLE_NOW)
        {
            ctx.Player.sendMessage("tile.bed.noSleep");
        }

        return true;
    }

    public override int getTexture(int side, int meta)
    {
        if (side == 0)
        {
            return Planks.textureId;
        }

        int direction = getDirection(meta);
        int sideFacing = Facings.BED_FACINGS[direction][side];
        return isHeadOfBed(meta) ? sideFacing == 2 ? textureId + 2 + 16 : sideFacing != 5 && sideFacing != 4 ? textureId + 1 : textureId + 1 + 16 :
            sideFacing == 3 ? textureId - 1 + 16 :
            sideFacing != 5 && sideFacing != 4 ? textureId : textureId + 16;
    }

    public override BlockRendererType getRenderType() => BlockRendererType.Bed;

    public override bool isFullCube() => false;

    public override bool isOpaque() => false;

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z) => setDefaultShape();

    public override void neighborUpdate(OnTickEvt ctx)
    {
        int blockMeta = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        int direction = getDirection(blockMeta);

        if (isHeadOfBed(blockMeta))
        {
            if (ctx.WorldRead.GetBlockId(ctx.X - BED_OFFSETS[direction][0], ctx.Y, ctx.Z - BED_OFFSETS[direction][1]) != id)
            {
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            }
        }
        else if (ctx.WorldRead.GetBlockId(ctx.X + BED_OFFSETS[direction][0], ctx.Y, ctx.Z + BED_OFFSETS[direction][1]) != id)
        {
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            if (!ctx.IsRemote)
            {
                // Explicitly pass 1.0f for luck to avoid missing parameter issues
                dropStacks(new OnDropEvt(ctx.WorldRead, ctx.Broadcaster, ctx.Rules, ctx.IsRemote, ctx.X, ctx.Y, ctx.Z, blockMeta));
            }
        }
    }

    public override int getDroppedItemId(int blockMeta) => isHeadOfBed(blockMeta) ? 0 : Item.Bed.id;

    private void setDefaultShape() => setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 9.0F / 16.0F, 1.0F);

    public static int getDirection(int meta) => meta & 3;

    public static bool isHeadOfBed(int meta) => (meta & 8) != 0;

    public static bool isBedOccupied(int meta) => (meta & 4) != 0;

    // Changed signature to take specific coordinates and writers instead of the OnUseEvt context
    public static void updateState(WorldBlockWrite worldWrite, int x, int y, int z, int meta, bool occupied)
    {
        if (occupied)
        {
            meta |= 4;
        }
        else
        {
            meta &= -5;
        }

        worldWrite.SetBlockMeta(x, y, z, meta);
    }

    // Updated 'World' to 'IBlockReader' 
    public static Vec3i? findWakeUpPosition(IBlockReader world, int x, int y, int z, int skip)
    {
        int blockMeta = world.GetBlockMeta(x, y, z);
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
                    if (world.ShouldSuffocate(checkX, y - 1, checkZ) && world.IsAir(checkX, y, checkZ) && world.IsAir(checkX, y + 1, checkZ))
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

    public override void dropStacks(OnDropEvt ctx)
    {
        if (!isHeadOfBed(ctx.Meta))
        {
            base.dropStacks(ctx);
        }
    }

    public override int getPistonBehavior() => 1;
}
