using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Gen.Features;

internal class PineTreeFeature : Feature
{
    public override bool Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        int treeHeight = rand.NextInt(5) + 7;
        int trunkWithNoLeaves = treeHeight - rand.NextInt(2) - 3;
        int canopyHeight = treeHeight - trunkWithNoLeaves;
        int maxLeafRadius = 1 + rand.NextInt(canopyHeight + 1);

        bool canPlace = true;

        if (!(y >= 1 && y + treeHeight + 1 <= 128)) return false;


        for (int cy = y; cy <= y + 1 + treeHeight && canPlace; ++cy)
        {
            int checkRadius;
            if (cy - y < trunkWithNoLeaves) checkRadius = 0;
            else checkRadius = maxLeafRadius;

            for (int cx = x - checkRadius; cx <= x + checkRadius && canPlace; ++cx)
            {
                for (int cz = z - checkRadius; cz <= z + checkRadius && canPlace; ++cz)
                {
                    if (cy >= 0 && cy < 128)
                    {
                        int blockId = world.getBlockId(cx, cy, cz);
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

        if (!canPlace) return false;

        int groundId = world.getBlockId(x, y - 1, z);
        if ((groundId == Block.GrassBlock.id || groundId == Block.Dirt.id) && y < 128 - treeHeight - 1)
        {
            world.SetBlockWithoutNotifyingNeighbors(x, y - 1, z, Block.Dirt.id);
            int currentLeafRadius = 0;

            for (int cy = y + treeHeight; cy >= y + trunkWithNoLeaves; --cy)
            {
                for (int cx = x - currentLeafRadius; cx <= x + currentLeafRadius; ++cx)
                {
                    int offsetX = cx - x;

                    for (int cz = z - currentLeafRadius; cz <= z + currentLeafRadius; ++cz)
                    {
                        int offsetZ = cz - z;
                        if ((Math.Abs(offsetX) != currentLeafRadius || Math.Abs(offsetZ) != currentLeafRadius || currentLeafRadius <= 0) && !Block.BlocksOpaque[world.getBlockId(cx, cy, cz)])
                        {
                            world.SetBlockWithoutNotifyingNeighbors(cx, cy, cz, Block.Leaves.id, 1);
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
            };

            for (int trunkY = 0; trunkY < treeHeight - 1; ++trunkY)
            {
                int blockAtTrunk = world.getBlockId(x, y + trunkY, z);
                if (blockAtTrunk == 0 || blockAtTrunk == Block.Leaves.id)
                {
                    world.SetBlockWithoutNotifyingNeighbors(x, y + trunkY, z, Block.Log.id, 1);
                }
            }

            return true;
        }
        return false;
    }
}
