using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockFlowing : BlockFluid
{
    private readonly ThreadLocal<int> _adjacentSources = new(() => 0);
    private readonly ThreadLocal<int[]> _distanceToGap = new(() => new int[4]);
    private readonly ThreadLocal<bool[]> _spread = new(() => new bool[4]);

    public BlockFlowing(int id, Material material) : base(id, material)
    {
    }

    private void convertToSource(IWorldContext world, int x, int y, int z)
    {
        int meta = world.Reader.GetBlockMeta(x, y, z);
        world.BlockWriter.SetBlockWithoutNotifyingNeighbors(x, y, z, id + 1, meta, notifyBlockPlaced: false);
        world.Broadcaster.SetBlocksDirty(x, y, z, x, y, z);
        world.Broadcaster.BlockUpdateEvent(x, y, z);
    }

    public override void onTick(OnTickEvt ctx)
    {
        int currentState = getLiquidState(ctx.Level.Reader, ctx.X, ctx.Y, ctx.Z);
        sbyte spreadRate = 1;
        if (material == Material.Lava && !ctx.Level.dimension.EvaporatesWater)
        {
            spreadRate = 2;
        }

        bool convertToSource = true;
        int newLevel;
        if (currentState > 0)
        {
            int minDepth = -100;
            _adjacentSources.Value = 0;
            int lowestNeighborDepth = getLowestDepth(ctx.Level.Reader, ctx.X - 1, ctx.Y, ctx.Z, minDepth);
            lowestNeighborDepth = getLowestDepth(ctx.Level.Reader, ctx.X + 1, ctx.Y, ctx.Z, lowestNeighborDepth);
            lowestNeighborDepth = getLowestDepth(ctx.Level.Reader, ctx.X, ctx.Y, ctx.Z - 1, lowestNeighborDepth);
            lowestNeighborDepth = getLowestDepth(ctx.Level.Reader, ctx.X, ctx.Y, ctx.Z + 1, lowestNeighborDepth);
            newLevel = lowestNeighborDepth + spreadRate;
            if (newLevel >= 8 || lowestNeighborDepth < 0)
            {
                newLevel = -1;
            }

            if (getLiquidState(ctx.Level.Reader, ctx.X, ctx.Y + 1, ctx.Z) >= 0)
            {
                int stateAbove = getLiquidState(ctx.Level.Reader, ctx.X, ctx.Y + 1, ctx.Z);
                if (stateAbove >= 8)
                {
                    newLevel = stateAbove;
                }
                else
                {
                    newLevel = stateAbove + 8;
                }
            }

            if (_adjacentSources.Value >= 2 && material == Material.Water)
            {
                if (ctx.Level.Reader.GetMaterial(ctx.X, ctx.Y - 1, ctx.Z).IsSolid)
                {
                    newLevel = 0;
                }
                else if (ctx.Level.Reader.GetMaterial(ctx.X, ctx.Y - 1, ctx.Z) == material && ctx.Level.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z) == 0)
                {
                    newLevel = 0;
                }
            }

            if (material == Material.Lava && currentState < 8 && newLevel < 8 && newLevel > currentState && ctx.Level.random.NextInt(4) != 0)
            {
                newLevel = currentState;
                convertToSource = false;
            }

            if (newLevel != currentState)
            {
                currentState = newLevel;
                if (newLevel < 0)
                {
                    ctx.Level.BlockWriter.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
                }
                else
                {
                    ctx.Level.BlockWriter.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, newLevel);
                    ctx.Level.TickScheduler.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, id, getTickRate());
                    ctx.Level.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z, id);
                }
            }
            else if (convertToSource)
            {
                this.convertToSource(ctx.Level, ctx.X, ctx.Y, ctx.Z);
            }
            else
            {
                ctx.Level.TickScheduler.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, id, getTickRate());
            }
        }
        else
        {
            int minDepth = -100;
            _adjacentSources.Value = 0;
            getLowestDepth(ctx.Level.Reader, ctx.X - 1, ctx.Y, ctx.Z, minDepth);
            getLowestDepth(ctx.Level.Reader, ctx.X + 1, ctx.Y, ctx.Z, minDepth);
            getLowestDepth(ctx.Level.Reader, ctx.X, ctx.Y, ctx.Z - 1, minDepth);
            getLowestDepth(ctx.Level.Reader, ctx.X, ctx.Y, ctx.Z + 1, minDepth);
        }

        bool canSpreadDown = canSpreadTo(ctx.Level.Reader, ctx.X, ctx.Y - 1, ctx.Z);
        if (canSpreadDown)
        {
            if (currentState >= 8)
            {
                ctx.Level.BlockWriter.SetBlock(ctx.X, ctx.Y - 1, ctx.Z, id, currentState);
            }
            else
            {
                ctx.Level.BlockWriter.SetBlock(ctx.X, ctx.Y - 1, ctx.Z, id, currentState + 8);
            }
        }
        else if (currentState >= 0 && (currentState == 0 || isLiquidBreaking(ctx.Level.Reader, ctx.X, ctx.Y - 1, ctx.Z)))
        {
            newLevel = currentState + spreadRate;
            if (currentState >= 8)
            {
                newLevel = 1;
            }

            bool[] spreadArray = getSpread(ctx.Level.Reader, ctx.X, ctx.Y, ctx.Z);

            if (newLevel < 8)
            {
                if (spreadArray[0])
                    spreadTo(ctx.Level, ctx.X - 1, ctx.Y, ctx.Z, newLevel);
                if (spreadArray[1])
                    spreadTo(ctx.Level, ctx.X + 1, ctx.Y, ctx.Z, newLevel);
                if (spreadArray[2])
                    spreadTo(ctx.Level, ctx.X, ctx.Y, ctx.Z - 1, newLevel);
                if (spreadArray[3])
                    spreadTo(ctx.Level, ctx.X, ctx.Y, ctx.Z + 1, newLevel);
            }
        }

        if (currentState == 0 && ctx.Level.Reader.GetBlockId(ctx.X, ctx.Y, ctx.Z) == id)
        {
            this.convertToSource(ctx.Level, ctx.X, ctx.Y, ctx.Z);
        }
    }

    private void spreadTo(IWorldContext world, int x, int y, int z, int depth)
    {
        if (canSpreadTo(world.Reader, x, y, z))
        {
            int currentId = world.Reader.GetBlockId(x, y, z);
            if (currentId > 0)
            {
                if (material == Material.Lava)
                {
                    fizz(world.Broadcaster, x, y, z);
                }
                else
                {
                    Blocks[currentId].dropStacks(new OnDropEvt(world, x, y, z, world.Reader.GetBlockMeta(x, y, z)));
                }
            }

            world.BlockWriter.SetBlock(x, y, z, id, depth);
        }
    }

    private int getDistanceToGap(IBlockReader world, int x, int y, int z, int distance, int fromDirection)
    {
        int minDistance = 1000;

        for (int direction = 0; direction < 4; ++direction)
        {
            if ((direction != 0 || fromDirection != 1) && (direction != 1 || fromDirection != 0) && (direction != 2 || fromDirection != 3) && (direction != 3 || fromDirection != 2))
            {
                int neighborX = x;
                int neighborZ = z;
                if (direction == 0)
                {
                    neighborX = x - 1;
                }

                if (direction == 1)
                {
                    ++neighborX;
                }

                if (direction == 2)
                {
                    neighborZ = z - 1;
                }

                if (direction == 3)
                {
                    ++neighborZ;
                }

                if (!isLiquidBreaking(world, neighborX, y, neighborZ) && (world.GetMaterial(neighborX, y, neighborZ) != material || world.GetBlockMeta(neighborX, y, neighborZ) != 0))
                {
                    if (!isLiquidBreaking(world, neighborX, y - 1, neighborZ))
                    {
                        return distance;
                    }

                    if (distance < 4)
                    {
                        int childDistance = getDistanceToGap(world, neighborX, y, neighborZ, distance + 1, direction);
                        if (childDistance < minDistance)
                        {
                            minDistance = childDistance;
                        }
                    }
                }
            }
        }

        return minDistance;
    }

    private bool[] getSpread(IBlockReader world, int x, int y, int z)
    {
        int direction;
        int neighborX;
        int[] distanceToGap = _distanceToGap.Value!;
        for (direction = 0; direction < 4; ++direction)
        {
            distanceToGap[direction] = 1000;
            neighborX = x;
            int neighborZ = z;
            if (direction == 0)
            {
                neighborX = x - 1;
            }

            if (direction == 1)
            {
                ++neighborX;
            }

            if (direction == 2)
            {
                neighborZ = z - 1;
            }

            if (direction == 3)
            {
                ++neighborZ;
            }

            if (!isLiquidBreaking(world, neighborX, y, neighborZ) && (world.GetMaterial(neighborX, y, neighborZ) != material || world.GetBlockMeta(neighborX, y, neighborZ) != 0))
            {
                if (!isLiquidBreaking(world, neighborX, y - 1, neighborZ))
                {
                    distanceToGap[direction] = 0;
                }
                else
                {
                    distanceToGap[direction] = getDistanceToGap(world, neighborX, y, neighborZ, 1, direction);
                }
            }
        }

        direction = distanceToGap[0];

        for (neighborX = 1; neighborX < 4; ++neighborX)
        {
            if (distanceToGap[neighborX] < direction)
            {
                direction = distanceToGap[neighborX];
            }
        }

        bool[] spread = _spread.Value!;
        for (neighborX = 0; neighborX < 4; ++neighborX)
        {
            spread[neighborX] = distanceToGap[neighborX] == direction;
        }

        return spread;
    }

    private bool isLiquidBreaking(IBlockReader reader, int x, int y, int z)
    {
        int blockId = reader.GetBlockId(x, y, z);
        if (blockId != Door.id && blockId != IronDoor.id && blockId != Sign.id && blockId != Ladder.id && blockId != SugarCane.id)
        {
            if (blockId == 0)
            {
                return false;
            }

            Material material = Blocks[blockId].material;
            return material.BlocksMovement;
        }

        return true;
    }

    protected int getLowestDepth(IBlockReader reader, int x, int y, int z, int depth)
    {
        int liquidState = getLiquidState(reader, x, y, z);
        if (liquidState < 0)
        {
            return depth;
        }

        if (liquidState == 0)
        {
            _adjacentSources.Value++;
        }

        if (liquidState >= 8)
        {
            liquidState = 0;
        }

        return depth >= 0 && liquidState >= depth ? depth : liquidState;
    }

    private bool canSpreadTo(IBlockReader reader, int x, int y, int z)
    {
        int blockId = reader.GetBlockId(x, y, z);
        if (blockId == 0)
        {
            return true;
        }

        Material mat = reader.GetMaterial(x, y, z);
        return mat != this.material && mat != Material.Lava && !isLiquidBreaking(reader, x, y, z);
    }

    public override void neighborUpdate(OnTickEvt evt)
    {
        base.neighborUpdate(evt);
        if (evt.Level.Reader.GetBlockId(evt.X, evt.Y, evt.Z) == id)
        {
            evt.Level.TickScheduler.ScheduleBlockUpdate(evt.X, evt.Y, evt.Z, id, getTickRate());
        }
    }

    public override void onPlaced(OnPlacedEvt ctx)
    {
        base.onPlaced(ctx);
        int placedId = ctx.Level.Reader.GetBlockId(ctx.X, ctx.Y, ctx.Z);
        if (placedId == id && !ctx.Level.IsRemote)
        {
            ctx.Level.TickScheduler.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, id, getTickRate());
        }
    }
}
