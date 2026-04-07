using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Worlds.Colors;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockLeaves : BlockLeavesBase
{
    private readonly ThreadLocal<int[]?> s_decayRegion = new(() => null);
    private readonly int spriteIndex;

    const sbyte decayRadius = 4;
    const sbyte regionSize = 32;
    const int loadCheckExtent = decayRadius + 1;
    const int planeSize = regionSize * regionSize;
    const int centerOffset = regionSize / 2;

    public BlockLeaves(int id, int textureId) : base(id, textureId, Material.Leaves, false)
    {
        spriteIndex = textureId;
        setTickRandomly(true);
    }

    public override int getColor(int meta) => (meta & 1) == 1 ? FoliageColors.getSpruceColor() : (meta & 2) == 2 ? FoliageColors.getBirchColor() : FoliageColors.getDefaultColor();

    public override int getColorMultiplier(IBlockReader reader, int x, int y, int z)
    {
        int meta = reader.GetBlockMeta(x, y, z);
        if ((meta & 1) == 1) return FoliageColors.getSpruceColor();
        if ((meta & 2) == 2) return FoliageColors.getBirchColor();
        reader.GetBiomeSource().GetBiomesInArea(x, z, 1, 1);
        double temperature = reader.GetBiomeSource().TemperatureMap[0];
        double downfall = reader.GetBiomeSource().DownfallMap[0];
        return FoliageColors.getFoliageColor(temperature, downfall);
    }

    public override void onBreak(OnBreakEvent @event)
    {
        const sbyte searchRadius = 1;
        int loadCheckExtent = searchRadius + 1;
        if (!@event.World.ChunkHost.IsRegionLoaded(@event.X - loadCheckExtent, @event.Y - loadCheckExtent, @event.Z - loadCheckExtent, @event.X + loadCheckExtent, @event.Y + loadCheckExtent, @event.Z + loadCheckExtent))
        {
            return;
        }

        for (int offsetX = -searchRadius; offsetX <= searchRadius; ++offsetX)
        {
            for (int offsetY = -searchRadius; offsetY <= searchRadius; ++offsetY)
            {
                for (int offsetZ = -searchRadius; offsetZ <= searchRadius; ++offsetZ)
                {
                    int blockId = @event.World.Reader.GetBlockId(@event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ);
                    if (blockId != Leaves.id) continue;

                    int leavesMeta = @event.World.Reader.GetBlockMeta(@event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ);
                    @event.World.Writer.SetBlockMetaWithoutNotifyingNeighbors(@event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ, leavesMeta | 8);
                }
            }
        }
    }

    public override void onTick(OnTickEvent @event)
    {
        if (@event.World.IsRemote) return;
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if ((meta & 8) == 0) return;
        s_decayRegion.Value ??= new int[regionSize * regionSize * regionSize];

        int[] decayRegion = s_decayRegion.Value;

        int distanceToLog;
        if (@event.World.ChunkHost.IsRegionLoaded(@event.X - loadCheckExtent, @event.Y - loadCheckExtent, @event.Z - loadCheckExtent, @event.X + loadCheckExtent, @event.Y + loadCheckExtent, @event.Z + loadCheckExtent))
        {
            distanceToLog = -decayRadius;

            while (distanceToLog <= decayRadius)
            {
                for (int dx = -decayRadius; dx <= decayRadius; ++dx)
                {
                    for (int dy = -decayRadius; dy <= decayRadius; ++dy)
                    {
                        int blockId = @event.World.Reader.GetBlockId(@event.X + distanceToLog, @event.Y + dx, @event.Z + dy);
                        if (blockId == Log.id)
                        {
                            decayRegion[(distanceToLog + centerOffset) * planeSize + (dx + centerOffset) * regionSize + dy + centerOffset] = 0;
                        }
                        else if (blockId == Leaves.id)
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
                for (int dx = -decayRadius; dx <= decayRadius; ++dx)
                {
                    for (int dy = -decayRadius; dy <= decayRadius; ++dy)
                    {
                        for (int dz = -decayRadius; dz <= decayRadius; ++dz)
                        {
                            if (decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset) * regionSize + dz + centerOffset] != distanceToLog - 1)
                            {
                                continue;
                            }

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

        distanceToLog = decayRegion[centerOffset * planeSize + centerOffset * regionSize + centerOffset];
        if (distanceToLog >= 0)
        {
            @event.World.Writer.SetBlockMetaWithoutNotifyingNeighbors(@event.X, @event.Y, @event.Z, meta & -9);
        }
        else
        {
            breakLeaves(@event.World, @event.X, @event.Y, @event.Z);
        }
    }

    private void breakLeaves(IWorldContext level, int x, int y, int z)
    {
        dropStacks(new OnDropEvent(level, x, y, z, level.Reader.GetBlockMeta(x, y, z)));
        level.Writer.SetBlock(x, y, z, 0);
    }

    public override int getDroppedItemCount() => Random.Shared.Next(20) == 0 ? 1 : 0;

    public override int getDroppedItemId(int blockMeta) => Sapling.id;

    public override void onAfterBreak(OnAfterBreakEvent ctx)
    {
        if (!ctx.World.IsRemote && ctx.Player.getHand() != null && ctx.Player.getHand().itemId == Item.Shears.id)
        {
            ctx.Player.increaseStat(Stats.Stats.MineBlockStatArray[id], 1);
            dropStack(ctx.World, ctx.X, ctx.Y, ctx.Z, new ItemStack(Leaves.id, 1, ctx.Meta & 3));
        }
        else
        {
            base.onAfterBreak(ctx);
        }
    }

    protected override int getDroppedItemMeta(int blockMeta) => blockMeta & 3;

    public override bool isOpaque() => !GraphicsLevel;

    public override int GetTexture(Side side, int meta) => (meta & 3) == 1 ? TextureId + 80 : TextureId;

    public void setGraphicsLevel(bool bl)
    {
        GraphicsLevel = bl;
        TextureId = spriteIndex + (bl ? 0 : 1);
    }

    public override void onSteppedOn(OnEntityStepEvent ctx) => base.onSteppedOn(ctx);
}
