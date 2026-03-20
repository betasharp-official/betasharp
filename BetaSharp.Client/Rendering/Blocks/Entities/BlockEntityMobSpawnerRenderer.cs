using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Blocks.Entities;

public class BlockEntityMobSpawnerRenderer : BlockEntitySpecialRenderer
{

    private readonly Dictionary<string, Entity> _entityDict = [];

    public void renderTileEntityMobSpawner(BlockEntityMobSpawner spawner, double x, double y, double z, float partialTicks)
    {
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate((float)x + 0.5F, (float)y, (float)z + 0.5F);
        _entityDict.TryGetValue(spawner.GetSpawnedEntityId(), out Entity? entity);
        if (entity == null)
        {
            entity = EntityRegistry.Create(spawner.GetSpawnedEntityId(), null);
            _entityDict.Add(spawner.GetSpawnedEntityId(), entity);
        }

        if (entity != null)
        {
            entity.setWorld(spawner.World);
            float scale = 7.0F / 16.0F;
            GLManager.GL.Translate(0.0F, 0.4F, 0.0F);
            GLManager.GL.Rotate((float)(spawner.LastRotation + (spawner.Rotation - spawner.LastRotation) * (double)partialTicks) * 10.0F, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(-30.0F, 1.0F, 0.0F, 0.0F);
            GLManager.GL.Translate(0.0F, -0.4F, 0.0F);
            GLManager.GL.Scale(scale, scale, scale);
            entity.setPositionAndAnglesKeepPrevAngles(x, y, z, 0.0F, 0.0F);
            EntityRenderDispatcher.instance.renderEntityWithPosYaw(entity, 0.0D, 0.0D, 0.0D, 0.0F, partialTicks);
        }

        GLManager.GL.PopMatrix();
    }

    public override void renderTileEntityAt(BlockEntity blockEntity, double x, double y, double z, float tickDelta)
    {
        renderTileEntityMobSpawner((BlockEntityMobSpawner)blockEntity, x, y, z, tickDelta);
    }
}
