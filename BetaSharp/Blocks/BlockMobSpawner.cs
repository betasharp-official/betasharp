using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockMobSpawner(int id, int textureId) : BlockWithEntity(id, textureId, Material.Stone)
{
    public override BlockEntity getBlockEntity() => new BlockEntityMobSpawner();

    public override int getDroppedItemId(int blockMeta) => 0;

    public override int getDroppedItemCount() => 0;

    public override bool isOpaque() => false;
}
