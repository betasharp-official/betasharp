using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Blocks.Entities;

public class BlockEntityMobSpawnerRenderer : BlockEntitySpecialRenderer
{

    private readonly Dictionary<string, Entity> _entityDict = [];

    public void RenderTileEntityMobSpawner(BlockEntityMobSpawner spawner, double x, double y, double z, float tickDelta)
    {
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate((float)x + 0.5F, (float)y, (float)z + 0.5F);
        _entityDict.TryGetValue(spawner.SpawnedEntityId, out Entity? ent);
        if (ent == null)
        {
            ent = EntityRegistry.Create(spawner.SpawnedEntityId, null);
            _entityDict.Add(spawner.SpawnedEntityId, ent);
        }

        if (ent != null)
        {
            ent.SetWorld(spawner.World);
            float scale = 7.0F / 16.0F;
            GLManager.GL.Translate(0.0F, 0.4F, 0.0F);
            GLManager.GL.Rotate((float)(spawner.LastRotation + (spawner.Rotation - spawner.LastRotation) * (double)tickDelta) * 10.0F, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(-30.0F, 1.0F, 0.0F, 0.0F);
            GLManager.GL.Translate(0.0F, -0.4F, 0.0F);
            GLManager.GL.Scale(scale, scale, scale);
            ent.SetPositionAndAnglesKeepPrevAngles(x, y, z, 0.0F, 0.0F);
            EntityRenderDispatcher.Instance.RenderEntityWithPosYaw(ent, 0.0D, 0.0D, 0.0D, 0.0F, tickDelta);
        }

        GLManager.GL.PopMatrix();
    }

    public override void renderTileEntityAt(BlockEntity blockEntity, double x, double y, double z, float tickDelta)
    {
        RenderTileEntityMobSpawner((BlockEntityMobSpawner)blockEntity, x, y, z, tickDelta);
    }
}
