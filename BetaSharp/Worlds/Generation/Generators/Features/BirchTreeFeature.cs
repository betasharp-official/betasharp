using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class BirchTreeFeature : Feature
{
    public override bool Generate(IWorldContext level, JavaRandom rand, int x, int y, int z)
    {
        int treeHeight = rand.NextInt(3) + 5;
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

        int soilId = level.Reader.GetBlockId(x, y - 1, z);
        if ((soilId == Block.GrassBlock.id || soilId == Block.Dirt.id) && y < ChuckFormat.WorldHeight - treeHeight - 1)
        {
            level.Writer.SetBlockWithoutNotifyingNeighbors(x, y - 1, z, Block.Dirt.id, 0, false);


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
                        bool isCorner = (Math.Abs(offsetX) != leafRadius ||
                                         Math.Abs(offsetZ) != leafRadius ||
                                         (rand.NextInt(2) != 0 && relativeY != 0)) && !Block.BlocksOpaque[level.Reader.GetBlockId(leafX, leafY, leafZ)];
                        if (isCorner)
                        {
                            level.Writer.SetBlockWithoutNotifyingNeighbors(leafX, leafY, leafZ, Block.Leaves.id, 2, false);
                        }
                    }
                }
            }

            for (int trunkY = 0; trunkY < treeHeight; ++trunkY)
            {
                int blockAtTrunk = level.Reader.GetBlockId(x, y + trunkY, z);
                if (blockAtTrunk == 0 || blockAtTrunk == Block.Leaves.id)
                {
                    level.Writer.SetBlockWithoutNotifyingNeighbors(x, y + trunkY, z, Block.Log.id, 2, false);
                }
            }

            return true;
        }

        return false;
    }
}
