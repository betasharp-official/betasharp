using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Rendering.Entities;

public class FallingBlockEntityRenderer : EntityRenderer
{
    public FallingBlockEntityRenderer()
    {
        ShadowRadius = 0.5F;
    }

    public void doRenderFallingSand(EntityFallingSand fallingBlockEntity, double x, double y, double z, float yaw, float tickDelta)
    {
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate((float)x, (float)y, (float)z);
        loadTexture("/terrain.png");
        Block block = Block.Blocks[fallingBlockEntity.BlockId];
        IWorldContext world = fallingBlockEntity.World;
        GLManager.GL.Disable(GLEnum.Lighting);
        BlockRenderer.RenderBlockFallingSand(block, world, MathHelper.Floor(fallingBlockEntity.X), MathHelper.Floor(fallingBlockEntity.Y), MathHelper.Floor(fallingBlockEntity.Z), Tessellator.instance);
        GLManager.GL.Enable(GLEnum.Lighting);
        GLManager.GL.PopMatrix();
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        doRenderFallingSand((EntityFallingSand)target, x, y, z, yaw, tickDelta);
    }
}
