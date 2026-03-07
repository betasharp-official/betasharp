using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Gen.Features;

internal class SpruceTreeFeature : Feature
{

    public override bool Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        int totalHeight = rand.NextInt(4) + 6;
        int topTrunkNoLeaves = 1 + rand.NextInt(2);
        int leafStartOffset = totalHeight - topTrunkNoLeaves;
        int maxLeafRadius = 2 + rand.NextInt(2);

        bool canPlace = true;

        if (!(y >= 1 && y + totalHeight + 1 <= 128)) return false;

        for (int cy = y; cy <= y + 1 + totalHeight && canPlace; ++cy)
        {
            int checkRadius;
            if (cy - y < topTrunkNoLeaves) checkRadius = 0;
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
        if (!((groundId == Block.GrassBlock.id || groundId == Block.Dirt.id) && y < 128 - totalHeight - 1)) return false;

        world.SetBlockWithoutNotifyingNeighbors(x, y - 1, z, Block.Dirt.id);
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

                    if ((Math.Abs(offsetX) != currentRadius || Math.Abs(offsetZ) != currentRadius || currentRadius <= 0) && !Block.BlocksOpaque[world.getBlockId(cx, leafY, cz)])
                    {
                        world.SetBlockWithoutNotifyingNeighbors(cx, leafY, cz, Block.Leaves.id, 1);
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
            int blockAtTrunk = world.getBlockId(x, y + trunkY, z);
            if (blockAtTrunk == 0 || blockAtTrunk == Block.Leaves.id)
            {
                world.SetBlockWithoutNotifyingNeighbors(x, y + trunkY, z, Block.Log.id, 1);
            }
        }

        return true;
    }

}
