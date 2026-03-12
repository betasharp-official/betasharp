using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class DeadBushPatchFeature : Feature
{
    private readonly int _deadBushBlockId;

    public DeadBushPatchFeature(int deadBushBlockId) => _deadBushBlockId = deadBushBlockId;

    public override bool Generate(IWorldContext level, JavaRandom rand, int x, int y, int z)
    {
        while (true)
        {
            int blockId = level.BlocksReader.GetBlockId(x, y, z);
            if ((blockId != 0 && blockId != Block.Leaves.id) || y <= 0)
            {
                for (int i = 0; i < 4; ++i)
                {
                    int genX = x + rand.NextInt(8) - rand.NextInt(8);
                    int genY = y + rand.NextInt(4) - rand.NextInt(4);
                    int genZ = z + rand.NextInt(8) - rand.NextInt(8);
                    if (level.BlocksReader.IsAir(genX, genY, genZ) &&
                        ((BlockPlant)Block.Blocks[_deadBushBlockId]).canGrow(new OnTickEvt(level, genX, genY, genZ, level.BlocksReader.GetMeta(genX, genY, genZ), level.BlocksReader.GetBlockId(genX, genY, genZ))))
                    {
                        level.BlockWriter.SetBlockWithoutNotifyingNeighbors(genX, genY, genZ, _deadBushBlockId, 0, notifyBlockPlaced: false);
                    }
                }

                return true;
            }

            --y;
        }
    }
}
