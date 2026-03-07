using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Gen.Features;

internal class LakeFeature : Feature
{

    private readonly int _waterBlockId;

    public LakeFeature(int waterBlockId)
    {
        _waterBlockId = waterBlockId;
    }

    public override bool Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        x -= 8;

        while (y > 0 && world.isAir(x, y, z))
        {
            y--;
        }

        y -= 4;
        bool[] lakeMask = new bool[2048];
        int blobCount = rand.NextInt(4) + 4;


        for (int i = 0; i < blobCount; ++i)
        {
            double radiusH = rand.NextDouble() * 6.0D + 3.0D;
            double radiusV = rand.NextDouble() * 4.0D + 2.0D;
            double radiusH2 = rand.NextDouble() * 6.0D + 3.0D;

            double centerX = rand.NextDouble() * (16.0D - radiusH - 2.0D) + 1.0D + radiusH / 2.0D;
            double centerY = rand.NextDouble() * (8.0D - radiusV - 4.0D) + 2.0D + radiusV / 2.0D;
            double centerZ = rand.NextDouble() * (16.0D - radiusH2 - 2.0D) + 1.0D + radiusH2 / 2.0D;

            for (int dx = 1; dx < 15; ++dx)
            {
                for (int dy = 1; dy < 15; ++dy)
                {
                    for (int dz = 1; dz < 7; ++dz)
                    {
                        double normX = (dx - centerX) / (radiusH / 2.0D);
                        double normY = (dz - centerY) / (radiusV / 2.0D);
                        double normZ = (dy - centerZ) / (radiusH2 / 2.0D);

                        double distSq = normX * normX + normY * normY + normZ * normZ;
                        if (distSq < 1.0D)
                        {
                            lakeMask[(dx * 16 + dy) * 8 + dz] = true;
                        }
                    }
                }
            }
        }



        for (int dx = 0; dx < 16; ++dx)
        {
            for (int dz = 0; dz < 16; ++dz)
            {
                for (int dy = 0; dy < 8; ++dy)
                {
                    bool isEdge = !lakeMask[(dx * 16 + dz) * 8 + dy] && (dx < 15 && lakeMask[((dx + 1) * 16 + dz) * 8 + dy] || dx > 0 && lakeMask[((dx - 1) * 16 + dz) * 8 + dy] || dz < 15 && lakeMask[(dx * 16 + dz + 1) * 8 + dy] || dz > 0 && lakeMask[(dx * 16 + (dz - 1)) * 8 + dy] || dy < 7 && lakeMask[(dx * 16 + dz) * 8 + dy + 1] || dy > 0 && lakeMask[(dx * 16 + dz) * 8 + (dy - 1)]);
                    if (isEdge)
                    {
                        Material mat = world.getMaterial(x + dx, y + dy, z + dz);
                        if (dy >= 4 && mat.IsFluid)
                        {
                            return false;
                        }

                        if (dy < 4 && !mat.IsSolid && world.getBlockId(x + dx, y + dy, z + dz) != _waterBlockId)
                        {
                            return false;
                        }
                    }
                }
            }
        }

        for (int dx = 0; dx < 16; ++dx)
        {
            for (int dy = 0; dy < 16; ++dy)
            {
                for (int dz = 0; dz < 8; ++dz)
                {
                    if (lakeMask[(dx * 16 + dy) * 8 + dz])
                    {
                        var blockId = dz >= 4 ? 0 : _waterBlockId;
                        world.SetBlockWithoutNotifyingNeighbors(x + dx, y + dz, z + dy, blockId);
                    }
                }
            }
        }

        for (int dx = 0; dx < 16; ++dx)
        {
            for (int dy = 0; dy < 16; ++dy)
            {
                for (int dz = 4; dz < 8; ++dz)
                {
                    if (lakeMask[(dx * 16 + dy) * 8 + dz] &&
                        world.getBlockId(x + dx, y + dz - 1, z + dy) == Block.Dirt.id &&
                        world.getBrightness(LightType.Sky, x + dx, y + dz, z + dy) > 0)
                    {
                        world.SetBlockWithoutNotifyingNeighbors(x + dx, y + dz - 1, z + dy, Block.GrassBlock.id);
                    }
                }
            }
        }

        if (Block.Blocks[_waterBlockId].material == Material.Lava)
        {
            for (int dx = 0; dx < 16; ++dx)
            {
                for (int dy = 0; dy < 16; ++dy)
                {
                    for (int dz = 0; dz < 8; ++dz)
                    {
                        bool isEdge = !lakeMask[(dx * 16 + dy) * 8 + dz] &&
                            (
                                dx < 15 && lakeMask[((dx + 1) * 16 + dy) * 8 + dz] ||
                                dx > 0 && lakeMask[((dx - 1) * 16 + dy) * 8 + dz] ||
                                dy < 15 && lakeMask[(dx * 16 + dy + 1) * 8 + dz] ||
                                dy > 0 && lakeMask[(dx * 16 + (dy - 1)) * 8 + dz] ||
                                dz < 7 && lakeMask[(dx * 16 + dy) * 8 + dz + 1] ||
                                dz > 0 && lakeMask[(dx * 16 + dy) * 8 + (dz - 1)]
                            );
                        if (isEdge && (dz < 4 || rand.NextInt(2) != 0) && world.getMaterial(x + dx, y + dz, z + dy).IsSolid)
                        {
                            world.SetBlockWithoutNotifyingNeighbors(x + dx, y + dz, z + dy, Block.Stone.id);
                        }
                    }
                }
            }
        }

        return true;
    }
}
