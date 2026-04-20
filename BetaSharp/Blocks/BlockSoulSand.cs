using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockSoulSand(int id, int textureId) : Block(id, textureId, Material.Sand)
{
    private const float Height = 2.0F / 16.0F;

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => new Box(x, y, z, x + 1, y + 1 - Height, z + 1);

    public override void OnEntityCollision(OnEntityCollisionEvent @event)
    {
        @event.Entity.VelocityX *= 0.4;
        @event.Entity.VelocityZ *= 0.4;
    }
}
