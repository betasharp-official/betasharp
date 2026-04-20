using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class LargeOakTreeFeature : Feature
{
    private static readonly sbyte[] MINOR_AXES = [2, 0, 0, 1, 2, 1];
    private readonly double branchSlope = 0.381D;
    private readonly int[] origin = [0, 0, 0];
    private readonly double trunkScale = 0.618D;
    private readonly int trunkWidth = 1;
    private IWorldContext _level;
    private int[][] branches;
    private double branchLengthScale = 1.0D;
    private int foliageClusterHeight = 4;
    private double foliageDensity = 1.0D;
    private int height;
    private int maxTrunkHeight = 12;
    private int trunkHeight;

    private void makeBranches()
    {
        trunkHeight = (int)(height * trunkScale);
        if (trunkHeight >= height)
        {
            trunkHeight = height - 1;
        }

        int branchCountTarget = (int)(1.382D + Math.Pow(foliageDensity * height / 13.0D, 2.0D));
        if (branchCountTarget < 1)
        {
            branchCountTarget = 1;
        }

        //int[][] branchCandidates = new int[branchCountTarget * this.field_878_e][4];
        int[][] branchCandidates = new int[branchCountTarget * height][];
        for (int i = 0; i < branchCandidates.Length; i++)
        {
            branchCandidates[i] = new int[4];
        }

        int foliageY = origin[1] + height - foliageClusterHeight;
        int branchCount = 1;
        int trunkTopY = origin[1] + trunkHeight;
        int foliageOffset = foliageY - origin[1];
        branchCandidates[0][0] = origin[0];
        branchCandidates[0][1] = foliageY;
        branchCandidates[0][2] = origin[2];
        branchCandidates[0][3] = trunkTopY;
        --foliageY;

        while (true)
        {
            while (foliageOffset >= 0)
            {
                int attemptIndex = 0;
                float treeShapeRadius = getTreeShape(foliageOffset);
                if (treeShapeRadius < 0.0F)
                {
                    --foliageY;
                    --foliageOffset;
                }
                else
                {
                    for (double coordinateBias = 0.5D; attemptIndex < branchCountTarget; ++attemptIndex)
                    {
                        double branchDistance = branchLengthScale * treeShapeRadius * (Random.Shared.NextSingle() + 0.328D);
                        double branchAngle = Random.Shared.NextSingle() * 2.0D * 3.14159D;
                        int branchX = MathHelper.Floor(branchDistance * Math.Sin(branchAngle) + origin[0] + coordinateBias);
                        int branchZ = MathHelper.Floor(branchDistance * Math.Cos(branchAngle) + origin[2] + coordinateBias);
                        int[] branchBasePos = [branchX, foliageY, branchZ];
                        int[] foliageTopPos = [branchX, foliageY + foliageClusterHeight, branchZ];
                        if (tryBranch(branchBasePos, foliageTopPos) == -1)
                        {
                            int[] trunkAttachPos = [origin[0], origin[1], origin[2]];
                            double horizontalDistance = Math.Sqrt(Math.Pow(Math.Abs(origin[0] - branchBasePos[0]), 2.0D) + Math.Pow(Math.Abs(origin[2] - branchBasePos[2]), 2.0D));
                            double verticalOffset = horizontalDistance * branchSlope;
                            if (branchBasePos[1] - verticalOffset > trunkTopY)
                            {
                                trunkAttachPos[1] = trunkTopY;
                            }
                            else
                            {
                                trunkAttachPos[1] = (int)(branchBasePos[1] - verticalOffset);
                            }

                            if (tryBranch(trunkAttachPos, branchBasePos) == -1)
                            {
                                branchCandidates[branchCount][0] = branchX;
                                branchCandidates[branchCount][1] = foliageY;
                                branchCandidates[branchCount][2] = branchZ;
                                branchCandidates[branchCount][3] = trunkAttachPos[1];
                                ++branchCount;
                            }
                        }
                    }

                    --foliageY;
                    --foliageOffset;
                }
            }

            //this.field_868_o = new int[branchCount][4];
            branches = new int[branchCount][];
            for (int i = 0; i < branches.Length; i++)
            {
                branches[i] = new int[4];
            }
            Array.Copy(branchCandidates, 0, branches, 0, branchCount);
            return;
        }
    }

    private void placeCluster(int x, int y, int z, float radius, sbyte axis, int blockId)
    {
        int radiusInt = (int)(radius + 0.618D);
        sbyte firstMinorAxis = MINOR_AXES[axis];
        sbyte secondMinorAxis = MINOR_AXES[axis + 3];
        int[] centerPos = [x, y, z];
        int[] currentPos = [0, 0, 0];
        int primaryOffset = -radiusInt;
        int secondaryOffset = -radiusInt;

        for (currentPos[axis] = centerPos[axis]; primaryOffset <= radiusInt; ++primaryOffset)
        {
            currentPos[firstMinorAxis] = centerPos[firstMinorAxis] + primaryOffset;
            secondaryOffset = -radiusInt;

            while (secondaryOffset <= radiusInt)
            {
                double distanceFromCenter = Math.Sqrt(
                    Math.Pow(Math.Abs(primaryOffset) + 0.5D, 2.0D) +
                    Math.Pow(Math.Abs(secondaryOffset) + 0.5D, 2.0D)
                );

                if (distanceFromCenter > radius)
                {
                    ++secondaryOffset;
                    continue;
                }

                currentPos[secondMinorAxis] = centerPos[secondMinorAxis] + secondaryOffset;
                int currentBlockId = _level.Reader.GetBlockId(currentPos[0], currentPos[1], currentPos[2]);

                if (currentBlockId != 0 && currentBlockId != 18)
                {
                    ++secondaryOffset;
                    continue;
                }

                _level.Writer.SetBlock(currentPos[0], currentPos[1], currentPos[2], blockId, 0, false);
                ++secondaryOffset;
            }
        }
    }

    private float getTreeShape(int foliageOffset)
    {
        if (foliageOffset < (float)height * 0.3D)
        {
            return -1.618F;
        }

        float halfHeight = height / 2.0F;
        float distanceFromCenter = height / 2.0F - foliageOffset;
        float shapeRadius;
        if (distanceFromCenter == 0.0F)
        {
            shapeRadius = halfHeight;
        }
        else if (Math.Abs(distanceFromCenter) >= halfHeight)
        {
            shapeRadius = 0.0F;
        }
        else
        {
            shapeRadius = (float)Math.Sqrt(Math.Pow(Math.Abs(halfHeight), 2.0D) - Math.Pow(Math.Abs(distanceFromCenter), 2.0D));
        }

        shapeRadius *= 0.5F;
        return shapeRadius;
    }

    private float getClusterShape(int clusterLayer) => clusterLayer >= 0 && clusterLayer < foliageClusterHeight ? clusterLayer != 0 && clusterLayer != foliageClusterHeight - 1 ? 3.0F : 2.0F : -1.0F;

    private void placeFoliageCluster(int x, int y, int z)
    {
        int leafY = y;

        for (int clusterTopY = y + foliageClusterHeight; leafY < clusterTopY; ++leafY)
        {
            float clusterRadius = getClusterShape(leafY - y);
            placeCluster(x, leafY, z, clusterRadius, 1, 18);
        }
    }

    private void placeBranch(int[] fromPos, int[] toPos, int blockId)
    {
        int[] delta = [0, 0, 0];
        sbyte axis = 0;

        sbyte dominantAxis;
        for (dominantAxis = 0; axis < 3; ++axis)
        {
            delta[axis] = toPos[axis] - fromPos[axis];
            if (Math.Abs(delta[axis]) > Math.Abs(delta[dominantAxis]))
            {
                dominantAxis = axis;
            }
        }

        if (delta[dominantAxis] != 0)
        {
            sbyte firstMinorAxis = MINOR_AXES[dominantAxis];
            sbyte secondMinorAxis = MINOR_AXES[dominantAxis + 3];
            sbyte stepDirection;
            if (delta[dominantAxis] > 0)
            {
                stepDirection = 1;
            }
            else
            {
                stepDirection = -1;
            }

            double firstMinorSlope = delta[firstMinorAxis] / (double)delta[dominantAxis];
            double secondMinorSlope = delta[secondMinorAxis] / (double)delta[dominantAxis];
            int[] currentPos = [0, 0, 0];
            int step = 0;

            for (int endStep = delta[dominantAxis] + stepDirection; step != endStep; step += stepDirection)
            {
                currentPos[dominantAxis] = MathHelper.Floor(fromPos[dominantAxis] + step + 0.5D);
                currentPos[firstMinorAxis] = MathHelper.Floor(fromPos[firstMinorAxis] + step * firstMinorSlope + 0.5D);
                currentPos[secondMinorAxis] = MathHelper.Floor(fromPos[secondMinorAxis] + step * secondMinorSlope + 0.5D);
                _level.Writer.SetBlockWithoutNotifyingNeighbors(currentPos[0], currentPos[1], currentPos[2], blockId, 0, false);
            }
        }
    }

    private void placeFoliage()
    {
        int branchIndex = 0;

        for (int branchCount = branches.Length; branchIndex < branchCount; ++branchIndex)
        {
            int branchX = branches[branchIndex][0];
            int branchY = branches[branchIndex][1];
            int branchZ = branches[branchIndex][2];
            placeFoliageCluster(branchX, branchY, branchZ);
        }
    }

    private bool shouldPlaceBranch(int branchHeight) => branchHeight >= height * 0.2D;

    private void placeTrunk()
    {
        int baseX = origin[0];
        int baseY = origin[1];
        int topY = origin[1] + trunkHeight;
        int baseZ = origin[2];
        int[] trunkBase = [baseX, baseY, baseZ];
        int[] trunkTop = [baseX, topY, baseZ];
        placeBranch(trunkBase, trunkTop, 17);
        if (trunkWidth == 2)
        {
            ++trunkBase[0];
            ++trunkTop[0];
            placeBranch(trunkBase, trunkTop, 17);
            ++trunkBase[2];
            ++trunkTop[2];
            placeBranch(trunkBase, trunkTop, 17);
            trunkBase[0] += -1;
            trunkTop[0] += -1;
            placeBranch(trunkBase, trunkTop, 17);
        }
    }

    private void placeBranches()
    {
        int branchIndex = 0;
        int branchCount = branches.Length;

        for (int[] trunkBase = [origin[0], origin[1], origin[2]]; branchIndex < branchCount; ++branchIndex)
        {
            int[] branchData = branches[branchIndex];
            int[] branchBase = [branchData[0], branchData[1], branchData[2]];
            trunkBase[1] = branchData[3];
            int branchHeight = trunkBase[1] - origin[1];
            if (shouldPlaceBranch(branchHeight))
            {
                placeBranch(trunkBase, branchBase, 17);
            }
        }
    }

    private int tryBranch(int[] fromPos, int[] toPos)
    {
        int[] delta = [0, 0, 0];
        sbyte axis = 0;

        sbyte dominantAxis;
        for (dominantAxis = 0; axis < 3; ++axis)
        {
            delta[axis] = toPos[axis] - fromPos[axis];
            if (Math.Abs(delta[axis]) > Math.Abs(delta[dominantAxis]))
            {
                dominantAxis = axis;
            }
        }

        if (delta[dominantAxis] == 0)
        {
            return -1;
        }

        sbyte firstMinorAxis = MINOR_AXES[dominantAxis];
        sbyte secondMinorAxis = MINOR_AXES[dominantAxis + 3];
        sbyte stepDirection;
        if (delta[dominantAxis] > 0)
        {
            stepDirection = 1;
        }
        else
        {
            stepDirection = -1;
        }

        double firstMinorSlope = delta[firstMinorAxis] / (double)delta[dominantAxis];
        double secondMinorSlope = delta[secondMinorAxis] / (double)delta[dominantAxis];
        int[] currentPos = [0, 0, 0];
        int step = 0;

        int endStep;
        for (endStep = delta[dominantAxis] + stepDirection; step != endStep; step += stepDirection)
        {
            currentPos[dominantAxis] = fromPos[dominantAxis] + step;
            currentPos[firstMinorAxis] = MathHelper.Floor(fromPos[firstMinorAxis] + step * firstMinorSlope);
            currentPos[secondMinorAxis] = MathHelper.Floor(fromPos[secondMinorAxis] + step * secondMinorSlope);
            int blockId = _level.Reader.GetBlockId(currentPos[0], currentPos[1], currentPos[2]);
            if (blockId != 0 && blockId != 18)
            {
                break;
            }
        }

        return step == endStep ? -1 : Math.Abs(step);
    }

    private bool canPlace()
    {
        int[] basePos = [origin[0], origin[1], origin[2]];
        int[] topPos = [origin[0], origin[1] + height - 1, origin[2]];
        int soilBlockId = _level.Reader.GetBlockId(origin[0], origin[1] - 1, origin[2]);
        if (soilBlockId != 2 && soilBlockId != 3)
        {
            return false;
        }

        int clearHeight = tryBranch(basePos, topPos);
        if (clearHeight == -1)
        {
            return true;
        }

        if (clearHeight < 6)
        {
            return false;
        }

        height = clearHeight;
        return true;
    }

    public override void prepare(double heightScale, double branchScale, double foliageScale)
    {
        maxTrunkHeight = (int)(heightScale * 12.0D);
        if (heightScale > 0.5D)
        {
            foliageClusterHeight = 5;
        }

        branchLengthScale = branchScale;
        foliageDensity = foliageScale;
    }

    public override bool Generate(IWorldContext level, JavaRandom rand, int x, int y, int z)
    {
        _level = level;
        long seed = rand.NextLong();
        rand.SetSeed(seed);
        origin[0] = x;
        origin[1] = y;
        origin[2] = z;
        if (height == 0)
        {
            height = 5 + rand.NextInt(maxTrunkHeight);
        }

        if (y + height >= ChuckFormat.WorldHeight) return false;

        if (!canPlace())
        {
            return false;
        }

        makeBranches();
        placeFoliage();
        placeTrunk();
        placeBranches();
        return true;
    }
}
