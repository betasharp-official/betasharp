using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockFlowing(int id, Material material) : BlockFluid(id, material)
{
    private readonly ThreadLocal<int> _adjacentSources = new(() => 0);
    private readonly ThreadLocal<int[]> _distanceToGap = new(() => new int[4]);
    private readonly ThreadLocal<bool[]> _spread = new(() => new bool[4]);

    private void ConvertToSource(IWorldContext world, int x, int y, int z)
    {
        int meta = world.Reader.GetBlockMeta(x, y, z);
        world.Writer.SetBlockWithoutNotifyingNeighbors(x, y, z, ID + 1, meta, false);
        world.Broadcaster.SetBlocksDirty(x, y, z, x, y, z);
        world.Broadcaster.BlockUpdateEvent(x, y, z);
    }

    public override void OnTick(OnTickEvent ctx)
    {
        int currentState = GetLiquidState(ctx.World.Reader, ctx.X, ctx.Y, ctx.Z);
        sbyte spreadRate = 1;
        if (Material == Material.Lava && !ctx.World.Dimension.EvaporatesWater)
        {
            spreadRate = 2;
        }

        bool convertToSource = true;
        int newLevel;
        if (currentState > 0)
        {
            const int minDepth = -100;
            _adjacentSources.Value = 0;
            int lowestNeighborDepth = GetLowestDepth(ctx.World.Reader, ctx.X - 1, ctx.Y, ctx.Z, minDepth);
            lowestNeighborDepth = GetLowestDepth(ctx.World.Reader, ctx.X + 1, ctx.Y, ctx.Z, lowestNeighborDepth);
            lowestNeighborDepth = GetLowestDepth(ctx.World.Reader, ctx.X, ctx.Y, ctx.Z - 1, lowestNeighborDepth);
            lowestNeighborDepth = GetLowestDepth(ctx.World.Reader, ctx.X, ctx.Y, ctx.Z + 1, lowestNeighborDepth);
            newLevel = lowestNeighborDepth + spreadRate;
            if (newLevel >= 8 || lowestNeighborDepth < 0)
            {
                newLevel = -1;
            }

            if (GetLiquidState(ctx.World.Reader, ctx.X, ctx.Y + 1, ctx.Z) >= 0)
            {
                int stateAbove = GetLiquidState(ctx.World.Reader, ctx.X, ctx.Y + 1, ctx.Z);
                if (stateAbove >= 8)
                {
                    newLevel = stateAbove;
                }
                else
                {
                    newLevel = stateAbove + 8;
                }
            }

            if (_adjacentSources.Value >= 2 && Material == Material.Water)
            {
                if (ctx.World.Reader.GetMaterial(ctx.X, ctx.Y - 1, ctx.Z).IsSolid || (ctx.World.Reader.GetMaterial(ctx.X, ctx.Y - 1, ctx.Z) == Material && ctx.World.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z) == 0))
                {
                    newLevel = 0;
                }
            }

            if (Material == Material.Lava && currentState < 8 && newLevel < 8 && newLevel > currentState && ctx.World.Random.NextInt(4) != 0)
            {
                newLevel = currentState;
                convertToSource = false;
            }

            if (newLevel != currentState)
            {
                currentState = newLevel;
                if (newLevel < 0)
                {
                    ctx.World.Writer.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
                }
                else
                {
                    ctx.World.Writer.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, newLevel);
                    ctx.World.TickScheduler.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, ID, GetTickRate());
                    ctx.World.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z, ID);
                }
            }
            else if (convertToSource)
            {
                ConvertToSource(ctx.World, ctx.X, ctx.Y, ctx.Z);
            }
            else
            {
                ctx.World.TickScheduler.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, ID, GetTickRate());
            }
        }
        else
        {
            const int minDepth = -100;
            _adjacentSources.Value = 0;
            GetLowestDepth(ctx.World.Reader, ctx.X - 1, ctx.Y, ctx.Z, minDepth);
            GetLowestDepth(ctx.World.Reader, ctx.X + 1, ctx.Y, ctx.Z, minDepth);
            GetLowestDepth(ctx.World.Reader, ctx.X, ctx.Y, ctx.Z - 1, minDepth);
            GetLowestDepth(ctx.World.Reader, ctx.X, ctx.Y, ctx.Z + 1, minDepth);
        }

        if (currentState < 0)
        {
            return;
        }

        bool canSpreadDown = canSpreadTo(ctx.World, ctx.X, ctx.Y - 1, ctx.Z);
        if (canSpreadDown)
        {
            if (currentState >= 8)
            {
                ctx.World.Writer.SetBlock(ctx.X, ctx.Y - 1, ctx.Z, ID, currentState);
            }
            else
            {
                ctx.World.Writer.SetBlock(ctx.X, ctx.Y - 1, ctx.Z, ID, currentState + 8);
            }
        }

        if (currentState == 0 || IsLiquidBreaking(ctx.World, ctx.X, ctx.Y - 1, ctx.Z))
        {
            newLevel = currentState + spreadRate;
            if (currentState >= 8)
            {
                newLevel = 1;
            }

            bool[] spreadArray = getSpread(ctx.World, ctx.X, ctx.Y, ctx.Z);

            if (newLevel < 8)
            {
                if (spreadArray[0])
                {
                    SpreadTo(ctx.World, ctx.X - 1, ctx.Y, ctx.Z, newLevel);
                }

                if (spreadArray[1])
                {
                    SpreadTo(ctx.World, ctx.X + 1, ctx.Y, ctx.Z, newLevel);
                }

                if (spreadArray[2])
                {
                    SpreadTo(ctx.World, ctx.X, ctx.Y, ctx.Z - 1, newLevel);
                }

                if (spreadArray[3])
                {
                    SpreadTo(ctx.World, ctx.X, ctx.Y, ctx.Z + 1, newLevel);
                }
            }
        }

        if (currentState == 0 && ctx.World.Reader.GetBlockId(ctx.X, ctx.Y, ctx.Z) == ID)
        {
            ConvertToSource(ctx.World, ctx.X, ctx.Y, ctx.Z);
        }
    }

    private void SpreadTo(IWorldContext world, int x, int y, int z, int depth)
    {
        if (!canSpreadTo(world, x, y, z)) return;

        int currentId = world.Reader.GetBlockId(x, y, z);
        if (currentId > 0)
        {
            if (Material == Material.Lava)
            {
                Fizz(world.Broadcaster, x, y, z);
            }
            else
            {
                Blocks[currentId].DropStacks(new OnDropEvent(world, x, y, z, world.Reader.GetBlockMeta(x, y, z)));
            }
        }

        world.Writer.SetBlock(x, y, z, ID, depth);
    }

    private int GetDistanceToGap(IWorldContext world, int x, int y, int z, int distance, int fromDirection)
    {
        int minDistance = 1000;

        for (int direction = 0; direction < 4; ++direction)
        {
            if ((direction == 0 && fromDirection == 1) ||
                (direction == 1 && fromDirection == 0) ||
                (direction == 2 && fromDirection == 3) ||
                (direction == 3 && fromDirection == 2))
            {
                continue;
            }

            int neighborX = x;
            int neighborZ = z;
            switch (direction)
            {
                case 0:
                    neighborX = x - 1;
                    break;
                case 1:
                    ++neighborX;
                    break;
                case 2:
                    neighborZ = z - 1;
                    break;
                case 3:
                    ++neighborZ;
                    break;
            }

            if (IsLiquidBreaking(world, neighborX, y, neighborZ) || (world.Reader.GetMaterial(neighborX, y, neighborZ) == material && world.Reader.GetBlockMeta(neighborX, y, neighborZ) == 0))
            {
                continue;
            }

            if (!IsLiquidBreaking(world, neighborX, y - 1, neighborZ))
            {
                return distance;
            }

            if (distance >= 4)
            {
                continue;
            }

            int childDistance = GetDistanceToGap(world, neighborX, y, neighborZ, distance + 1, direction);
            if (childDistance < minDistance)
            {
                minDistance = childDistance;
            }
        }

        return minDistance;
    }

    private bool[] getSpread(IWorldContext world, int x, int y, int z)
    {
        int direction;
        int neighborX;
        int[] distanceToGap = _distanceToGap.Value!;
        for (direction = 0; direction < 4; ++direction)
        {
            distanceToGap[direction] = 1000;
            neighborX = x;
            int neighborZ = z;
            switch (direction)
            {
                case 0:
                    neighborX = x - 1;
                    break;
                case 1:
                    ++neighborX;
                    break;
                case 2:
                    neighborZ = z - 1;
                    break;
                case 3:
                    ++neighborZ;
                    break;
            }

            if (IsLiquidBreaking(world, neighborX, y, neighborZ) || (world.Reader.GetMaterial(neighborX, y, neighborZ) == material && world.Reader.GetBlockMeta(neighborX, y, neighborZ) == 0))
            {
                continue;
            }

            if (!IsLiquidBreaking(world, neighborX, y - 1, neighborZ))
            {
                distanceToGap[direction] = 0;
            }
            else
            {
                distanceToGap[direction] = GetDistanceToGap(world, neighborX, y, neighborZ, 1, direction);
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

    private static bool IsLiquidBreaking(IWorldContext world, int x, int y, int z)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000 || y < 0 || y >= ChuckFormat.WorldHeight) return false;

        if (!world.Reader.IsPosLoaded(x, y, z)) return true;

        int blockId = world.Reader.GetBlockId(x, y, z);
        if (blockId == Door.ID || blockId == IronDoor.ID || blockId == Sign.ID || blockId == Ladder.ID || blockId == SugarCane.ID) return true;

        if (blockId == 0)
        {
            return false;
        }

        Material mat = Blocks[blockId].Material;
        return mat.BlocksMovement;
    }

    private int GetLowestDepth(IBlockReader reader, int x, int y, int z, int depth)
    {
        int liquidState = GetLiquidState(reader, x, y, z);
        switch (liquidState)
        {
            case < 0:
                return depth;
            case 0:
                _adjacentSources.Value++;
                break;
            case >= 8:
                liquidState = 0;
                break;
        }

        return depth >= 0 && liquidState >= depth ? depth : liquidState;
    }

    private bool canSpreadTo(IWorldContext world, int x, int y, int z)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000 || y < 0 || y >= ChuckFormat.WorldHeight) return false;

        if (!world.Reader.IsPosLoaded(x, y, z)) return false;

        int blockId = world.Reader.GetBlockId(x, y, z);
        if (blockId == 0) return true;

        Material mat = world.Reader.GetMaterial(x, y, z);
        return mat != material && mat != Material.Lava && !IsLiquidBreaking(world, x, y, z);
    }

    public override void NeighborUpdate(OnTickEvent @event)
    {
        base.NeighborUpdate(@event);
        if (@event.World.Reader.GetBlockId(@event.X, @event.Y, @event.Z) == ID)
        {
            @event.World.TickScheduler.ScheduleBlockUpdate(@event.X, @event.Y, @event.Z, ID, GetTickRate());
        }
    }

    public override void OnPlaced(OnPlacedEvent ctx)
    {
        base.OnPlaced(ctx);
        int placedId = ctx.World.Reader.GetBlockId(ctx.X, ctx.Y, ctx.Z);
        if (placedId == ID && !ctx.World.IsRemote)
        {
            ctx.World.TickScheduler.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, ID, GetTickRate());
        }
    }
}
