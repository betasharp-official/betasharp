using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class DeadBushPatchFeature : Feature
{
    private readonly int _deadBushBlockId;

    public DeadBushPatchFeature(int deadBushBlockId) => _deadBushBlockId = deadBushBlockId;

    public override bool Generate(IWorldContext level, JavaRandom rand, int x, int y, int z)
    {
        while (true)
        {
            int blockId = level.Reader.GetBlockId(x, y, z);
            if ((blockId != 0 && blockId != Block.Leaves.id) || y <= 0)
            {
                for (int i = 0; i < 4; ++i)
                {
                    int genX = x + rand.NextInt(8) - rand.NextInt(8);
                    int genY = y + rand.NextInt(4) - rand.NextInt(4);
                    int genZ = z + rand.NextInt(8) - rand.NextInt(8);
                    if (level.Reader.IsAir(genX, genY, genZ) &&
                        ((BlockPlant)Block.Blocks[_deadBushBlockId]).canGrow(new OnTickEvent(level, genX, genY, genZ, level.Reader.GetBlockMeta(genX, genY, genZ), level.Reader.GetBlockId(genX, genY, genZ))))
                    {
                        level.Writer.SetBlockWithoutNotifyingNeighbors(genX, genY, genZ, _deadBushBlockId, 0, false);
                    }
                }

                return true;
            }

            --y;
        }
    }
}
