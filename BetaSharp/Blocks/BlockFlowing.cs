using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockFlowing : BlockFluid
{
    private readonly ThreadLocal<int> _adjacentSources = new(() => 0);
    private readonly ThreadLocal<int[]> _distanceToGap = new(() => new int[4]);
    private readonly ThreadLocal<bool[]> _spread = new(() => new bool[4]);

    public BlockFlowing(int id, Material material) : base(id, material)
    {
    }

    private void convertToSource(World world, int x, int y, int z)
    {
        int meta = world.getBlockMeta(x, y, z);
        world.setBlockWithoutNotifyingNeighbors(x, y, z, id + 1, meta);
        world.setBlocksDirty(x, y, z, x, y, z);
        world.blockUpdateEvent(x, y, z);
    }

    public override void onTick(OnTickEvt ctx)
    {
        int currentState = getLiquidState(ctx);
        sbyte spreadRate = 1;
        if (material == Material.Lava && !ctx.Dimension.EvaporatesWater)
        {
            spreadRate = 2;
        }

        bool convertToSource = true;
        int newLevel;
        if (currentState > 0)
        {
            int minDepth = -100;
            _adjacentSources.Value = 0;
            int lowestNeighborDepth = getLowestDepth(ctx.WorldRead, ctx.X - 1, ctx.Y, ctx.Z, minDepth);
            lowestNeighborDepth = getLowestDepth(ctx.WorldRead, ctx.X + 1, ctx.Y, ctx.Z, lowestNeighborDepth);
            lowestNeighborDepth = getLowestDepth(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z - 1, lowestNeighborDepth);
            lowestNeighborDepth = getLowestDepth(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z + 1, lowestNeighborDepth);
            newLevel = lowestNeighborDepth + spreadRate;
            if (newLevel >= 8 || lowestNeighborDepth < 0)
            {
                newLevel = -1;
            }

            if (getLiquidState(ctx.WorldRead, ctx.X, ctx.Y + 1, ctx.Z) >= 0)
            {
                int stateAbove = getLiquidState(ctx.WorldRead, ctx.X, ctx.Y + 1, ctx.Z);
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
                if (ctx.WorldRead.GetMaterial(ctx.X, ctx.Y - 1, ctx.Z).IsSolid)
                {
                    newLevel = 0;
                }
                else if (ctx.WorldRead.GetMaterial(ctx.X, ctx.Y - 1, ctx.Z) == material && ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z) == 0)
                {
                    newLevel = 0;
                }
            }

            if (material == Material.Lava && currentState < 8 && newLevel < 8 && newLevel > currentState && ctx.Random.NextInt(4) != 0)
            {
                newLevel = currentState;
                convertToSource = false;
            }

            if (newLevel != currentState)
            {
                currentState = newLevel;
                if (newLevel < 0)
                {
                    ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
                }
                else
                {
                    ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, newLevel);
                    ctx.WorldWrite.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, id, getTickRate());
                    ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z, id);
                }
            }
            else if (convertToSource)
            {
                this.convertToSource(ctx.WorldRead, ctx.WorldWrite, ctx.Broadcaster, ctx.X, ctx.Y, ctx.Z);
            }
        }
        else
        {
            this.convertToSource(ctx.WorldRead, ctx.WorldWrite, ctx.Broadcaster, ctx.X, ctx.Y, ctx.Z);
        }

        if (canSpreadTo(ctx.WorldRead, ctx.X, ctx.Y - 1, ctx.Z))
        {
            if (currentState >= 8)
            {
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y - 1, ctx.Z, id, currentState);
            }
            else
            {
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y - 1, ctx.Z, id, currentState + 8);
            }
        }
        else if (currentState >= 0 && (currentState == 0 || isLiquidBreaking(worldView, x, y - 1, z)))
        {
            bool[] spreadDirections = getSpread(worldView, x, y, z);
            newLevel = currentState + spreadRate;
            if (currentState >= 8)
            {
                newLevel = 1;
            }

            if (newLevel >= 8)
            {
                return;
            }

            if (spreadDirections[0])
            {
                spreadTo(worldView, x - 1, y, z, newLevel);
            }

            if (spreadDirections[1])
            {
                spreadTo(worldView, x + 1, y, z, newLevel);
            }

            if (spreadDirections[2])
            {
                spreadTo(worldView, x, y, z - 1, newLevel);
            }

            if (spreadDirections[3])
            {
                spreadTo(worldView, x, y, z + 1, newLevel);
            }
        }
    }

    private void spreadTo(World world, int x, int y, int z, int depth)
    {
        if (canSpreadTo(world, x, y, z))
        {
            int blockId = world.getBlockId(x, y, z);
            if (blockId > 0)
            {
                if (material == Material.Lava)
                {
                    fizz(world, x, y, z);
                }
                else
                {
                    Blocks[blockId].dropStacks(world, x, y, z, world.getBlockMeta(x, y, z));
                }
            }

            world.setBlock(x, y, z, id, depth);
        }
    }

    private int getDistanceToGap(World world, int x, int y, int z, int distance, int fromDirection)
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

                if (!isLiquidBreaking(world, neighborX, y, neighborZ) && (world.getMaterial(neighborX, y, neighborZ) != material || world.getBlockMeta(neighborX, y, neighborZ) != 0))
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

    private bool[] getSpread(World world, int x, int y, int z)
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

            if (!isLiquidBreaking(world, neighborX, y, neighborZ) && (world.getMaterial(neighborX, y, neighborZ) != material || world.getBlockMeta(neighborX, y, neighborZ) != 0))
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
        Material material = reader.GetMaterial(x, y, z);
        return material != this.material && material != Material.Lava && !isLiquidBreaking(reader, x, y, z);
    }

    public override void onPlaced(OnPlacedEvt ctx)
    {
        base.onPlaced(ctx);
        if (ctx.WorldRead.GetBlockId(ctx.X, ctx.Y, ctx.Z) == id)
        {
            ctx.WorldWrite.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, id, getTickRate());
        }
    }
}
