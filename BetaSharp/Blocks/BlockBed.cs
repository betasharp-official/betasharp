using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockBed : Block
{
    public static readonly int[][] BED_OFFSETS = [[0, 1], [-1, 0], [0, -1], [1, 0]];

    public BlockBed(int id) : base(id, 134, Material.Wool)
    {
        setDefaultShape();
    }

    public override bool onUse(OnUseEvt evt)
    {
        if (evt.Level.IsRemote)
        {
            return true;
        }

        // Extract to local variables so we don't mutate the event struct
        int x = evt.X;
        int y = evt.Y;
        int z = evt.Z;

        int meta = evt.Level.Reader.GetBlockMeta(x, y, z);
        if (!isHeadOfBed(meta))
        {
            int direction = getDirection(meta);
            x += BED_OFFSETS[direction][0];
            z += BED_OFFSETS[direction][1];

            if (evt.Level.Reader.GetBlockId(x, y, z) != id)
            {
                return true;
            }

            meta = evt.Level.Reader.GetBlockMeta(x, y, z);
        }

        if (!evt.Level.dimension.HasWorldSpawn)
        {
            double posX = x + 0.5D;
            double posY = y + 0.5D;
            double posZ = z + 0.5D;
            evt.Level.BlockWriter.SetBlock(x, y, z, 0);

            int direction = getDirection(meta);
            x += BED_OFFSETS[direction][0];
            z += BED_OFFSETS[direction][1];

            if (evt.Level.Reader.GetBlockId(x, y, z) == id)
            {
                evt.Level.BlockWriter.SetBlock(x, y, z, 0);
                posX = (posX + x + 0.5D) / 2.0D;
                posY = (posY + y + 0.5D) / 2.0D;
                posZ = (posZ + z + 0.5D) / 2.0D;
            }

            evt.Level.CreateExplosion(null, x + 0.5F, y + 0.5F, z + 0.5F, 5.0F, true);
            return true;
        }

        if (isBedOccupied(meta))
        {
            EntityPlayer? occupant = null;
            foreach (EntityPlayer otherPlayer in evt.Level.Entities.Players)
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
                evt.Player.sendMessage("tile.bed.occupied");
                return true;
            }

            updateState(evt.Level.BlockWriter, x, y, z, meta, false);
        }

        SleepAttemptResult result = evt.Player.trySleep(x, y, z);
        if (result == SleepAttemptResult.OK)
        {
            updateState(evt.Level.BlockWriter, x, y, z, meta, true);
            return true;
        }

        if (result == SleepAttemptResult.NOT_POSSIBLE_NOW)
        {
            evt.Player.sendMessage("tile.bed.noSleep");
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

    public override void neighborUpdate(OnTickEvt ctx)
    {
        int blockMeta = ctx.Level.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        int direction = getDirection(blockMeta);

        if (isHeadOfBed(blockMeta))
        {
            if (ctx.Level.Reader.GetBlockId(ctx.X - BED_OFFSETS[direction][0], ctx.Y, ctx.Z - BED_OFFSETS[direction][1]) != id)
            {
                ctx.Level.BlockWriter.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            }
        }
        else if (ctx.Level.Reader.GetBlockId(ctx.X + BED_OFFSETS[direction][0], ctx.Y, ctx.Z + BED_OFFSETS[direction][1]) != id)
        {
            ctx.Level.BlockWriter.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            if (!ctx.Level.IsRemote)
            {
                dropStacks(new OnDropEvt(ctx.Level, ctx.X, ctx.Y, ctx.Z, blockMeta));
            }
        }
    }

    public override int getDroppedItemId(int blockMeta)
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

    public static void updateState(WorldWriter worldWrite, int x, int y, int z, int meta, bool occupied)
    {
        if (occupied)
        {
            meta |= 4;
        }
        else
        {
            meta &= ~4;
        }

        worldWrite.SetBlockMeta(x, y, z, meta);
    }

    // Updated 'World' to 'IBlockReader'
    public static Vec3i? findWakeUpPosition(IBlockReader reader, int x, int y, int z, int skip)
    {
        int blockMeta = reader.GetBlockMeta(x, y, z);
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
                    if (reader.ShouldSuffocate(checkX, y - 1, checkZ) && reader.IsAir(checkX, y, checkZ) && reader.IsAir(checkX, y + 1, checkZ))
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

    public override void dropStacks(OnDropEvt evt)
    {
        if (!isHeadOfBed(evt.Meta))
        {
            base.dropStacks(evt);
        }
    }

    public override int getPistonBehavior()
    {
        return 1;
    }
}
