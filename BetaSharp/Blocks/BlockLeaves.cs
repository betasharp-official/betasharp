using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Worlds.ClientData.Colors;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockLeaves : BlockLeavesBase
{
    private readonly ThreadLocal<int[]?> s_decayRegion = new(() => null);
    private readonly int spriteIndex;

    public BlockLeaves(int id, int textureId) : base(id, textureId, Material.Leaves, false)
    {
        spriteIndex = textureId;
        setTickRandomly(true);
    }

    public override int getColor(int meta) => (meta & 1) == 1 ? FoliageColors.getSpruceColor() : (meta & 2) == 2 ? FoliageColors.getBirchColor() : FoliageColors.getDefaultColor();

    public override int getColorMultiplier(IBlockReader reader, int x, int y, int z)
    {
        int meta = reader.GetBlockMeta(x, y, z);
        if ((meta & 1) == 1)
        {
            return FoliageColors.getSpruceColor();
        }

        if ((meta & 2) == 2)
        {
            return FoliageColors.getBirchColor();
        }

        reader.GetBiomeSource().GetBiomesInArea(x, z, 1, 1);
        double temperature = reader.GetBiomeSource().TemperatureMap[0];
        double downfall = reader.GetBiomeSource().DownfallMap[0];
        return FoliageColors.getFoliageColor(temperature, downfall);
    }

    public override void onBreak(OnBreakEvt evt)
    {
        sbyte searchRadius = 1;
        int loadCheckExtent = searchRadius + 1;
        if (evt.Level.BlockHost.IsRegionLoaded(evt.X - loadCheckExtent, evt.Y - loadCheckExtent, evt.Z - loadCheckExtent, evt.X + loadCheckExtent, evt.Y + loadCheckExtent, evt.Z + loadCheckExtent))
        {
            for (int offsetX = -searchRadius; offsetX <= searchRadius; ++offsetX)
            {
                for (int offsetY = -searchRadius; offsetY <= searchRadius; ++offsetY)
                {
                    for (int offsetZ = -searchRadius; offsetZ <= searchRadius; ++offsetZ)
                    {
                        int blockId = evt.Level.Reader.GetBlockId(evt.X + offsetX, evt.Y + offsetY, evt.Z + offsetZ);
                        if (blockId == Leaves.id)
                        {
                            int leavesMeta = evt.Level.Reader.GetBlockMeta(evt.X + offsetX, evt.Y + offsetY, evt.Z + offsetZ);
                            evt.Level.BlockWriter.SetBlockMetaWithoutNotifyingNeighbors(evt.X + offsetX, evt.Y + offsetY, evt.Z + offsetZ, leavesMeta | 8);
                        }
                    }
                }
            }
        }
    }

    public override void onTick(OnTickEvt evt)
    {
        if (!evt.Level.IsRemote)
        {
            int meta = evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z);
            if ((meta & 8) != 0)
            {
                sbyte decayRadius = 4;
                int loadCheckExtent = decayRadius + 1;
                sbyte regionSize = 32;
                int planeSize = regionSize * regionSize;
                int centerOffset = regionSize / 2;
                if (s_decayRegion.Value == null)
                {
                    s_decayRegion.Value = new int[regionSize * regionSize * regionSize];
                }

                int[] decayRegion = s_decayRegion.Value;

                int distanceToLog;
                if (evt.Level.BlockHost.IsRegionLoaded(evt.X - loadCheckExtent, evt.Y - loadCheckExtent, evt.Z - loadCheckExtent, evt.X + loadCheckExtent, evt.Y + loadCheckExtent, evt.Z + loadCheckExtent))
                {
                    distanceToLog = -decayRadius;

                    while (distanceToLog <= decayRadius)
                    {
                        int dx;
                        int dy;
                        int dz;

                        for (dx = -decayRadius; dx <= decayRadius; ++dx)
                        {
                            for (dy = -decayRadius; dy <= decayRadius; ++dy)
                            {
                                dz = evt.Level.Reader.GetBlockId(evt.X + distanceToLog, evt.Y + dx, evt.Z + dy);
                                if (dz == Log.id)
                                {
                                    decayRegion[(distanceToLog + centerOffset) * planeSize + (dx + centerOffset) * regionSize + dy + centerOffset] = 0;
                                }
                                else if (dz == Leaves.id)
                                {
                                    decayRegion[(distanceToLog + centerOffset) * planeSize + (dx + centerOffset) * regionSize + dy + centerOffset] = -2;
                                }
                                else
                                {
                                    decayRegion[(distanceToLog + centerOffset) * planeSize + (dx + centerOffset) * regionSize + dy + centerOffset] = -1;
                                }
                            }
                        }

                        ++distanceToLog;
                    }

                    for (distanceToLog = 1; distanceToLog <= 4; ++distanceToLog)
                    {
                        int dx;
                        int dy;
                        int dz;

                        for (dx = -decayRadius; dx <= decayRadius; ++dx)
                        {
                            for (dy = -decayRadius; dy <= decayRadius; ++dy)
                            {
                                for (dz = -decayRadius; dz <= decayRadius; ++dz)
                                {
                                    if (decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset) * regionSize + dz + centerOffset] == distanceToLog - 1)
                                    {
                                        if (decayRegion[(dx + centerOffset - 1) * planeSize + (dy + centerOffset) * regionSize + dz + centerOffset] == -2)
                                        {
                                            decayRegion[(dx + centerOffset - 1) * planeSize + (dy + centerOffset) * regionSize + dz + centerOffset] = distanceToLog;
                                        }

                                        if (decayRegion[(dx + centerOffset + 1) * planeSize + (dy + centerOffset) * regionSize + dz + centerOffset] == -2)
                                        {
                                            decayRegion[(dx + centerOffset + 1) * planeSize + (dy + centerOffset) * regionSize + dz + centerOffset] = distanceToLog;
                                        }

                                        if (decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset - 1) * regionSize + dz + centerOffset] == -2)
                                        {
                                            decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset - 1) * regionSize + dz + centerOffset] = distanceToLog;
                                        }

                                        if (decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset + 1) * regionSize + dz + centerOffset] == -2)
                                        {
                                            decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset + 1) * regionSize + dz + centerOffset] = distanceToLog;
                                        }

                                        if (decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset) * regionSize + (dz + centerOffset - 1)] == -2)
                                        {
                                            decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset) * regionSize + (dz + centerOffset - 1)] = distanceToLog;
                                        }

                                        if (decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset) * regionSize + dz + centerOffset + 1] == -2)
                                        {
                                            decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset) * regionSize + dz + centerOffset + 1] = distanceToLog;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                distanceToLog = decayRegion[centerOffset * planeSize + centerOffset * regionSize + centerOffset];
                if (distanceToLog >= 0)
                {
                    evt.Level.BlockWriter.SetBlockMetaWithoutNotifyingNeighbors(evt.X, evt.Y, evt.Z, meta & -9);
                }
                else
                {
                    breakLeaves(evt.Level, evt.X, evt.Y, evt.Z);
                }
            }
        }
    }

    private void breakLeaves(IWorldContext level, int x, int y, int z)
    {
        dropStacks(new OnDropEvt(level, x, y, z, level.Reader.GetBlockMeta(x, y, z)));
        level.BlockWriter.SetBlock(x, y, z, 0);
    }

    public override int getDroppedItemCount() => Random.Shared.Next(20) == 0 ? 1 : 0;

    public override int getDroppedItemId(int blockMeta) => Sapling.id;

    public override void onAfterBreak(OnAfterBreakEvt ctx)
    {
        if (!ctx.Level.IsRemote && ctx.Player.getHand() != null && ctx.Player.getHand().itemId == Item.Shears.id)
        {
            ctx.Player.increaseStat(Stats.Stats.MineBlockStatArray[id], 1);
            dropStack(ctx.Level, ctx.X, ctx.Y, ctx.Z, new ItemStack(Leaves.id, 1, ctx.Meta & 3));
        }
        else
        {
            base.onAfterBreak(ctx);
        }
    }

    protected override int getDroppedItemMeta(int blockMeta) => blockMeta & 3;

    public override bool isOpaque() => !graphicsLevel;

    public override int getTexture(int side, int meta) => (meta & 3) == 1 ? textureId + 80 : textureId;

    public void setGraphicsLevel(bool bl)
    {
        graphicsLevel = bl;
        textureId = spriteIndex + (bl ? 0 : 1);
    }

    public override void onSteppedOn(OnEntityStepEvt ctx) => base.onSteppedOn(ctx);
}
