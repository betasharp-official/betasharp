using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class PineTreeFeature : Feature
{
    public override bool Generate(IWorldContext level, JavaRandom rand, int x, int y, int z)
    {
        int treeHeight = rand.NextInt(5) + 7;
        int trunkWithNoLeaves = treeHeight - rand.NextInt(2) - 3;
        int canopyHeight = treeHeight - trunkWithNoLeaves;
        int maxLeafRadius = 1 + rand.NextInt(canopyHeight + 1);

        bool canPlace = true;

        if (!(y >= 1 && y + treeHeight + 1 <= 128))
        {
            return false;
        }


        for (int cy = y; cy <= y + 1 + treeHeight && canPlace; ++cy)
        {
            int checkRadius;
            if (cy - y < trunkWithNoLeaves)
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
        if ((groundId == Block.GrassBlock.id || groundId == Block.Dirt.id) && y < 128 - treeHeight - 1)
        {
            level.Writer.SetBlockWithoutNotifyingNeighbors(x, y - 1, z, Block.Dirt.id, 0, false);
            int currentLeafRadius = 0;

            for (int cy = y + treeHeight; cy >= y + trunkWithNoLeaves; --cy)
            {
                for (int cx = x - currentLeafRadius; cx <= x + currentLeafRadius; ++cx)
                {
                    int offsetX = cx - x;

                    for (int cz = z - currentLeafRadius; cz <= z + currentLeafRadius; ++cz)
                    {
                        int offsetZ = cz - z;
                        if ((Math.Abs(offsetX) != currentLeafRadius || Math.Abs(offsetZ) != currentLeafRadius || currentLeafRadius <= 0) && !Block.BlocksOpaque[level.Reader.GetBlockId(cx, cy, cz)])
                        {
                            level.Writer.SetBlockWithoutNotifyingNeighbors(cx, cy, cz, Block.Leaves.id, 1, false);
                        }
                    }
                }

                if (currentLeafRadius >= 1 && cy == y + trunkWithNoLeaves + 1)
                {
                    --currentLeafRadius;
                }
                else if (currentLeafRadius < maxLeafRadius)
                {
                    ++currentLeafRadius;
                }
            }

            ;

            for (int trunkY = 0; trunkY < treeHeight - 1; ++trunkY)
            {
                int blockAtTrunk = level.Reader.GetBlockId(x, y + trunkY, z);
                if (blockAtTrunk == 0 || blockAtTrunk == Block.Leaves.id)
                {
                    level.Writer.SetBlockWithoutNotifyingNeighbors(x, y + trunkY, z, Block.Log.id, 1, false);
                }
            }

            return true;
        }

        return false;
    }
}
