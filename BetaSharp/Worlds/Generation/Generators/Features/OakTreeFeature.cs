using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class OakTreeFeature : Feature
{
    public override bool Generate(IWorldContext level, JavaRandom rand, int x, int y, int z)
    {
        int treeHeight = rand.NextInt(3) + 4;
        bool canPlace = true;
        if (!(y >= 1 && y + treeHeight + 1 <= ChuckFormat.WorldHeight))
        {
            return false;
        }


        for (int cy = y; cy <= y + 1 + treeHeight; ++cy)
        {
            byte checkRadius = 1;
            if (cy == y)
            {
                checkRadius = 0;
            }

            if (cy >= y + 1 + treeHeight - 2)
            {
                checkRadius = 2;
            }

            for (int cx = x - checkRadius; cx <= x + checkRadius && canPlace; ++cx)
            {
                for (int cz = z - checkRadius; cz <= z + checkRadius && canPlace; ++cz)
                {
                    if (cy >= 0 && cy < ChuckFormat.WorldHeight)
                    {
                        int blockId = level.Reader.GetBlockId(cx, cy, cz);
                        if (blockId != 0 && blockId != Block.Leaves.ID)
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
        if ((groundId == Block.GrassBlock.ID || groundId == Block.Dirt.ID) && y < ChuckFormat.WorldHeight - treeHeight - 1)
        {
            level.Writer.SetBlockWithoutNotifyingNeighbors(x, y - 1, z, Block.Dirt.ID, 0, false);

            for (int leafY = y - 3 + treeHeight; leafY <= y + treeHeight; ++leafY)
            {
                int relativeY = leafY - (y + treeHeight);
                int leafRadius = 1 - relativeY / 2;

                for (int leafX = x - leafRadius; leafX <= x + leafRadius; ++leafX)
                {
                    int offsetX = leafX - x;

                    for (int leafZ = z - leafRadius; leafZ <= z + leafRadius; ++leafZ)
                    {
                        int offsetZ = leafZ - z;
                        if ((Math.Abs(offsetX) != leafRadius || Math.Abs(offsetZ) != leafRadius || (rand.NextInt(2) != 0 && relativeY != 0)) && !Block.BlocksOpaque[level.Reader.GetBlockId(leafX, leafY, leafZ)])
                        {
                            level.Writer.SetBlockWithoutNotifyingNeighbors(leafX, leafY, leafZ, Block.Leaves.ID, 0, false);
                        }
                    }
                }
            }

            for (int trunkY = 0; trunkY < treeHeight; ++trunkY)
            {
                int blockAtTrunk = level.Reader.GetBlockId(x, y + trunkY, z);
                if (blockAtTrunk == 0 || blockAtTrunk == Block.Leaves.ID)
                {
                    level.Writer.SetBlockWithoutNotifyingNeighbors(x, y + trunkY, z, Block.Log.ID, 0, false);
                }
            }

            return true;
        }

        return false;
    }
}
