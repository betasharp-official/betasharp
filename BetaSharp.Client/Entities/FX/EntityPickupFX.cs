using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Entities.FX;

public class EntityPickupFX : EntityFX
{

    private readonly Entity target;
    private readonly Entity source;
    private int currentAge;
    private readonly int maxAge;
    private readonly float yOffset;

    public EntityPickupFX(IWorldContext world, Entity target, Entity source, float yOffset) : base(world, target.X, target.Y, target.Z, target.VelocityX, target.VelocityY, target.VelocityZ)
    {
        this.target = target;
        this.source = source;
        this.yOffset = yOffset;
        maxAge = 3;
    }

    public override void renderParticle(Tessellator t, float partialTick, float rotX, float rotY, float rotZ, float upX, float upZ)
    {
        float lifeProgress = ((float)currentAge + partialTick) / (float)maxAge;
        lifeProgress *= lifeProgress;
        double targetX = target.X;
        double targetY = target.Y;
        double targetZ = target.Z;
        double sourceX = source.LastTickX + (source.X - source.LastTickX) * (double)partialTick;
        double sourceY = source.LastTickY + (source.Y - source.LastTickY) * (double)partialTick + (double)yOffset;
        double sourceZ = source.LastTickZ + (source.Z - source.LastTickZ) * (double)partialTick;
        double renderX = targetX + (sourceX - targetX) * (double)lifeProgress;
        double renderY = targetY + (sourceY - targetY) * (double)lifeProgress;
        double renderZ = targetZ + (sourceZ - targetZ) * (double)lifeProgress;
        int itemX = MathHelper.Floor(renderX);
        int itemY = MathHelper.Floor(renderY + (double)(StandingEyeHeight / 2.0F));
        int itemZ = MathHelper.Floor(renderZ);
        float luminance = World.Lighting.GetLuminance(itemX, itemY, itemZ);
        renderX -= interpPosX;
        renderY -= interpPosY;
        renderZ -= interpPosZ;
        GLManager.GL.Color4(luminance, luminance, luminance, 1.0F);
        EntityRenderDispatcher.Instance.RenderEntityWithPosYaw(target, (double)((float)renderX), (double)((float)renderY), (double)((float)renderZ), target.Yaw, partialTick);
    }

    public override void Tick()
    {
        ++currentAge;
        if (currentAge == maxAge)
        {
            MarkDead();
        }

    }

    public override int getFXLayer()
    {
        return 3;
    }
}
