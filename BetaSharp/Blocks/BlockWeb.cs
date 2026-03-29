using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockWeb(int id, int texturePosition) : Block(id, texturePosition, Material.Cobweb)
{
    public override bool IsOpaque => false;
    public override bool IsFullCube => false;
    public override void OnEntityCollision(OnEntityCollisionEvent ctx) => ctx.Entity.slowed = true;
    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;
    public override BlockRendererType GetRenderType() => BlockRendererType.Reed;
    public override int GetDroppedItemId(int blockMeta) => Item.String.id;
}
