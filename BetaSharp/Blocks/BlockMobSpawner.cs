using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockMobSpawner(int id, int textureId) : BlockWithEntity(id, textureId, Material.Stone)
{
    public override BlockEntity GetBlockEntity() => new BlockEntityMobSpawner();

    public override int GetDroppedItemId(int blockMeta) => 0;

    public override int GetDroppedItemCount() => 0;

    public override bool IsOpaque() => false;
}
