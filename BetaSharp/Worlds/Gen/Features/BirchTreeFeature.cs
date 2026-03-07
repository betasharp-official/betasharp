using System.Runtime.CompilerServices;
using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Gen.Features;

internal class BirchTreeFeature : Feature
{

    public override bool Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        int treeHeight = rand.NextInt(3) + 5;
        bool canPlace = true;
        if (!(y >= 1 && y + treeHeight + 1 <= 128)) return false;


        for (int cy = y; cy <= y + 1 + treeHeight; ++cy)
        {
            byte checkRadius = 1;
            if (cy == y) checkRadius = 0;
            if (cy >= y + 1 + treeHeight - 2) checkRadius = 2;


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

        int soilId = world.getBlockId(x, y - 1, z);
        if ((soilId == Block.GrassBlock.id || soilId == Block.Dirt.id) && y < 128 - treeHeight - 1)
        {
            world.SetBlockWithoutNotifyingNeighbors(x, y - 1, z, Block.Dirt.id);


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
                                        rand.NextInt(2) != 0 && relativeY != 0) && !Block.BlocksOpaque[world.getBlockId(leafX, leafY, leafZ)];
                        if (isCorner)
                        {
                            world.SetBlockWithoutNotifyingNeighbors(leafX, leafY, leafZ, Block.Leaves.id, 2);
                        }
                    }
                }
            }

            for (int trunkY = 0; trunkY < treeHeight; ++trunkY)
            {
                var blockAtTrunk = world.getBlockId(x, y + trunkY, z);
                if (blockAtTrunk == 0 || blockAtTrunk == Block.Leaves.id)
                {
                    world.SetBlockWithoutNotifyingNeighbors(x, y + trunkY, z, Block.Log.id, 2);
                }
            }

            return true;
        }

        return false;
    }
}
