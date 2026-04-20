using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Worlds.Colors;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockLeaves : BlockLeavesBase
{
    private const sbyte DecayRadius = 4;
    private const sbyte RegionSize = 32;
    private const int LoadCheckExtent = DecayRadius + 1;
    private const int PlaneSize = RegionSize * RegionSize;
    private const int CenterOffset = RegionSize / 2;
    private readonly int _spriteIndex;
    private readonly ThreadLocal<int[]?> s_decayRegion = new(() => null);

    public BlockLeaves(int id, int textureId) : base(id, textureId, Material.Leaves, false)
    {
        _spriteIndex = textureId;
        SetTickRandomly(true);
    }

    public override int GetColor(int meta) => (meta & 1) == 1 ? FoliageColors.getSpruceColor() : (meta & 2) == 2 ? FoliageColors.getBirchColor() : FoliageColors.getDefaultColor();

    public override int GetColorMultiplier(IBlockReader reader, int x, int y, int z)
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

    public override void OnBreak(OnBreakEvent @event)
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
                    if (blockId != Leaves.ID)
                    {
                        continue;
                    }

                    int leavesMeta = @event.World.Reader.GetBlockMeta(@event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ);
                    @event.World.Writer.SetBlockMetaWithoutNotifyingNeighbors(@event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ, leavesMeta | 8);
                }
            }
        }
    }

    public override void OnTick(OnTickEvent @event)
    {
        if (@event.World.IsRemote)
        {
            return;
        }

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if ((meta & 8) == 0)
        {
            return;
        }

        s_decayRegion.Value ??= new int[RegionSize * RegionSize * RegionSize];

        int[] decayRegion = s_decayRegion.Value;

        int distanceToLog;
        if (@event.World.ChunkHost.IsRegionLoaded(@event.X - LoadCheckExtent, @event.Y - LoadCheckExtent, @event.Z - LoadCheckExtent, @event.X + LoadCheckExtent, @event.Y + LoadCheckExtent, @event.Z + LoadCheckExtent))
        {
            distanceToLog = -DecayRadius;

            while (distanceToLog <= DecayRadius)
            {
                for (int dx = -DecayRadius; dx <= DecayRadius; ++dx)
                {
                    for (int dy = -DecayRadius; dy <= DecayRadius; ++dy)
                    {
                        int blockId = @event.World.Reader.GetBlockId(@event.X + distanceToLog, @event.Y + dx, @event.Z + dy);
                        if (blockId == Log.ID)
                        {
                            decayRegion[(distanceToLog + CenterOffset) * PlaneSize + (dx + CenterOffset) * RegionSize + dy + CenterOffset] = 0;
                        }
                        else if (blockId == Leaves.ID)
                        {
                            decayRegion[(distanceToLog + CenterOffset) * PlaneSize + (dx + CenterOffset) * RegionSize + dy + CenterOffset] = -2;
                        }
                        else
                        {
                            decayRegion[(distanceToLog + CenterOffset) * PlaneSize + (dx + CenterOffset) * RegionSize + dy + CenterOffset] = -1;
                        }
                    }
                }

                ++distanceToLog;
            }

            for (distanceToLog = 1; distanceToLog <= 4; ++distanceToLog)
            {
                for (int dx = -DecayRadius; dx <= DecayRadius; ++dx)
                {
                    for (int dy = -DecayRadius; dy <= DecayRadius; ++dy)
                    {
                        for (int dz = -DecayRadius; dz <= DecayRadius; ++dz)
                        {
                            if (decayRegion[(dx + CenterOffset) * PlaneSize + (dy + CenterOffset) * RegionSize + dz + CenterOffset] != distanceToLog - 1)
                            {
                                continue;
                            }

                            if (decayRegion[(dx + CenterOffset - 1) * PlaneSize + (dy + CenterOffset) * RegionSize + dz + CenterOffset] == -2)
                            {
                                decayRegion[(dx + CenterOffset - 1) * PlaneSize + (dy + CenterOffset) * RegionSize + dz + CenterOffset] = distanceToLog;
                            }

                            if (decayRegion[(dx + CenterOffset + 1) * PlaneSize + (dy + CenterOffset) * RegionSize + dz + CenterOffset] == -2)
                            {
                                decayRegion[(dx + CenterOffset + 1) * PlaneSize + (dy + CenterOffset) * RegionSize + dz + CenterOffset] = distanceToLog;
                            }

                            if (decayRegion[(dx + CenterOffset) * PlaneSize + (dy + CenterOffset - 1) * RegionSize + dz + CenterOffset] == -2)
                            {
                                decayRegion[(dx + CenterOffset) * PlaneSize + (dy + CenterOffset - 1) * RegionSize + dz + CenterOffset] = distanceToLog;
                            }

                            if (decayRegion[(dx + CenterOffset) * PlaneSize + (dy + CenterOffset + 1) * RegionSize + dz + CenterOffset] == -2)
                            {
                                decayRegion[(dx + CenterOffset) * PlaneSize + (dy + CenterOffset + 1) * RegionSize + dz + CenterOffset] = distanceToLog;
                            }

                            if (decayRegion[(dx + CenterOffset) * PlaneSize + (dy + CenterOffset) * RegionSize + (dz + CenterOffset - 1)] == -2)
                            {
                                decayRegion[(dx + CenterOffset) * PlaneSize + (dy + CenterOffset) * RegionSize + (dz + CenterOffset - 1)] = distanceToLog;
                            }

                            if (decayRegion[(dx + CenterOffset) * PlaneSize + (dy + CenterOffset) * RegionSize + dz + CenterOffset + 1] == -2)
                            {
                                decayRegion[(dx + CenterOffset) * PlaneSize + (dy + CenterOffset) * RegionSize + dz + CenterOffset + 1] = distanceToLog;
                            }
                        }
                    }
                }
            }
        }

        distanceToLog = decayRegion[CenterOffset * PlaneSize + CenterOffset * RegionSize + CenterOffset];
        if (distanceToLog >= 0)
        {
            @event.World.Writer.SetBlockMetaWithoutNotifyingNeighbors(@event.X, @event.Y, @event.Z, meta & -9);
        }
        else
        {
            BreakLeaves(@event.World, @event.X, @event.Y, @event.Z);
        }
    }

    private void BreakLeaves(IWorldContext level, int x, int y, int z)
    {
        DropStacks(new OnDropEvent(level, x, y, z, level.Reader.GetBlockMeta(x, y, z)));
        level.Writer.SetBlock(x, y, z, 0);
    }

    public override int GetDroppedItemCount() => Random.Shared.Next(20) == 0 ? 1 : 0;

    public override int GetDroppedItemId(int blockMeta) => Sapling.ID;

    public override void OnAfterBreak(OnAfterBreakEvent ctx)
    {
        if (!ctx.World.IsRemote && ctx.Player.getHand() != null && ctx.Player.getHand().ItemId == Item.Shears.id)
        {
            ctx.Player.increaseStat(Stats.Stats.MineBlockStatArray[ID], 1);
            DropStack(ctx.World, ctx.X, ctx.Y, ctx.Z, new ItemStack(Leaves.ID, 1, ctx.Meta & 3));
        }
        else
        {
            base.OnAfterBreak(ctx);
        }
    }

    protected override int GetDroppedItemMeta(int blockMeta) => blockMeta & 3;

    public override bool IsOpaque() => !GraphicsLevel;

    public override int GetTexture(Side side, int meta) => (meta & 3) == 1 ? TextureId + 80 : TextureId;

    public void SetGraphicsLevel(bool bl)
    {
        GraphicsLevel = bl;
        TextureId = _spriteIndex + (bl ? 0 : 1);
    }

    public override void OnSteppedOn(OnEntityStepEvent ctx) => base.OnSteppedOn(ctx);
}
