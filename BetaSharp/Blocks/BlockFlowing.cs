using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockFlowing : BlockFluid
{
    private readonly ThreadLocal<int> _adjacentSources = new(() => 0);
    private readonly ThreadLocal<bool[]> _spread = new(() => new bool[4]);
    private readonly ThreadLocal<int[]> _distanceToGap = new(() => new int[4]);

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

    public override void onTick(OnTickContext ctx)
    {
        int currentState = getLiquidState(ctx);
        sbyte spreadRate = 1;
        if (material == Material.Lava && !worldView.dimension.EvaporatesWater)
        {
            spreadRate = 2;
        }

        bool convertToSource = true;
        int newLevel;
        if (currentState > 0)
        {
            int minDepth = -100;
            _adjacentSources.Value = 0;
            int lowestNeighborDepth = getLowestDepth(worldView, x - 1, y, z, minDepth);
            lowestNeighborDepth = getLowestDepth(worldView, x + 1, y, z, lowestNeighborDepth);
            lowestNeighborDepth = getLowestDepth(worldView, x, y, z - 1, lowestNeighborDepth);
            lowestNeighborDepth = getLowestDepth(worldView, x, y, z + 1, lowestNeighborDepth);
            newLevel = lowestNeighborDepth + spreadRate;
            if (newLevel >= 8 || lowestNeighborDepth < 0)
            {
                newLevel = -1;
            }

            if (getLiquidState(worldView, x, y + 1, z) >= 0)
            {
                int stateAbove = getLiquidState(worldView, x, y + 1, z);
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
                if (worldView.getMaterial(x, y - 1, z).IsSolid)
                {
                    newLevel = 0;
                }
                else if (worldView.getMaterial(x, y - 1, z) == material && worldView.getBlockMeta(x, y, z) == 0)
                {
                    newLevel = 0;
                }
            }

            if (material == Material.Lava && currentState < 8 && newLevel < 8 && newLevel > currentState && random.NextInt(4) != 0)
            {
                newLevel = currentState;
                convertToSource = false;
            }

            if (newLevel != currentState)
            {
                currentState = newLevel;
                if (newLevel < 0)
                {
                    worldView.setBlock(x, y, z, 0);
                }
                else
                {
                    worldView.setBlockMeta(x, y, z, newLevel);
                    worldView.ScheduleBlockUpdate(x, y, z, id, getTickRate());
                    worldView.NotifyNeighbors(x, y, z, id);
                }
            }
            else if (convertToSource)
            {
                this.convertToSource(worldView, x, y, z);
            }
        }
        else
        {
            this.convertToSource(worldView, x, y, z);
        }

        if (canSpreadTo(worldView, x, y - 1, z))
        {
            if (currentState >= 8)
            {
                worldView.setBlock(x, y - 1, z, id, currentState);
            }
            else
            {
                worldView.setBlock(x, y - 1, z, id, currentState + 8);
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
                    Block.Blocks[blockId].dropStacks(world, x, y, z, world.getBlockMeta(x, y, z));
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

    private bool isLiquidBreaking(World world, int x, int y, int z)
    {
        int blockId = world.getBlockId(x, y, z);
        if (blockId != Block.Door.id && blockId != Block.IronDoor.id && blockId != Block.Sign.id && blockId != Block.Ladder.id && blockId != Block.SugarCane.id)
        {
            if (blockId == 0)
            {
                return false;
            }
            else
            {
                Material material = Block.Blocks[blockId].material;
                return material.BlocksMovement;
            }
        }
        else
        {
            return true;
        }
    }

    protected int getLowestDepth(World world, int x, int y, int z, int depth)
    {
        int liquidState = getLiquidState(world, x, y, z);
        if (liquidState < 0)
        {
            return depth;
        }
        else
        {
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
    }

    private bool canSpreadTo(World world, int x, int y, int z)
    {
        Material material = world.getMaterial(x, y, z);
        return material != base.material && (material != Material.Lava && !isLiquidBreaking(world, x, y, z));
    }

    public override void onPlaced(World world, int x, int y, int z)
    {
        base.onPlaced(world, x, y, z);
        if (world.getBlockId(x, y, z) == id)
        {
            world.ScheduleBlockUpdate(x, y, z, id, getTickRate());
        }

    }
}
