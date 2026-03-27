using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class SpruceTreeFeature : Feature
{
    public override bool Generate(IWorldContext level, JavaRandom rand, int x, int y, int z)
    {
        int totalHeight = rand.NextInt(4) + 6;
        int topTrunkNoLeaves = 1 + rand.NextInt(2);
        int leafStartOffset = totalHeight - topTrunkNoLeaves;
        int maxLeafRadius = 2 + rand.NextInt(2);

        bool canPlace = true;

        if (!(y >= 1 && y + totalHeight + 1 <= 128))
        {
            return false;
        }

        for (int cy = y; cy <= y + 1 + totalHeight && canPlace; ++cy)
        {
            int checkRadius;
            if (cy - y < topTrunkNoLeaves)
            {
                checkRadius = 0;
            }
            else
            {
                checkRadius = maxLeafRadius;
            }

            for (int cx = x - checkRadius; cx <= x + checkRadius && canPlace; ++cx)
            {
                for (int cz = z - checkRadius; cz <= z + checkRadius && canPlace; ++cz)
                {
                    if (cy >= 0 && cy < 128)
                    {
                        int blockId = level.Reader.GetBlockId(cx, cy, cz);
                        if (blockId != 0 && blockId != Block.Leaves.id)
                        {
                            canPlace = false;
                        }
                    }
                    else
                    {
                        canPlace = false;
                    }
                }
            }
        }

        if (!canPlace)
        {
            return false;
        }

        int groundId = level.Reader.GetBlockId(x, y - 1, z);
        if (!((groundId == Block.GrassBlock.id || groundId == Block.Dirt.id) && y < 128 - totalHeight - 1))
        {
            return false;
        }

        level.Writer.SetBlockWithoutNotifyingNeighbors(x, y - 1, z, Block.Dirt.id, 0, false);
        int currentRadius = rand.NextInt(2);
        int radiusTarget = 1;
        byte radiusStep = 0;


        for (int h = 0; h <= leafStartOffset; ++h)
        {
            int leafY = y + totalHeight - h;

            for (int cx = x - currentRadius; cx <= x + currentRadius; ++cx)
            {
                int offsetX = cx - x;
                for (int cz = z - currentRadius; cz <= z + currentRadius; ++cz)
                {
                    int offsetZ = cz - z;

                    if ((Math.Abs(offsetX) != currentRadius || Math.Abs(offsetZ) != currentRadius || currentRadius <= 0) && !Block.BlocksOpaque[level.Reader.GetBlockId(cx, leafY, cz)])
                    {
                        level.Writer.SetBlockWithoutNotifyingNeighbors(cx, leafY, cz, Block.Leaves.id, 1, false);
                    }
                }
            }

            if (currentRadius >= radiusTarget)
            {
                currentRadius = radiusStep;
                radiusStep = 1;
                ++radiusTarget;
                if (radiusTarget > maxLeafRadius)
                {
                    radiusTarget = maxLeafRadius;
                }
            }
            else
            {
                ++currentRadius;
            }
        }

        int trunkVariability = rand.NextInt(3);

        for (int trunkY = 0; trunkY < totalHeight - trunkVariability; ++trunkY)
        {
            int blockAtTrunk = level.Reader.GetBlockId(x, y + trunkY, z);
            if (blockAtTrunk == 0 || blockAtTrunk == Block.Leaves.id)
            {
                level.Writer.SetBlockWithoutNotifyingNeighbors(x, y + trunkY, z, Block.Log.id, 1, false);
            }
        }

        return true;
    }
}
