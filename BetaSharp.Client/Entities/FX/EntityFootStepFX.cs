using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Entities.FX;

public class EntityFootStepFX : EntityFX
{

    private int localAge;
    private readonly int maxAge;
    private readonly TextureManager textureManager;

    public EntityFootStepFX(TextureManager textureManager, World world, double x, double y, double z) : base(world, x, y, z, 0.0D, 0.0D, 0.0D)
    {
        this.textureManager = textureManager;
        VelocityX = VelocityY = VelocityZ = 0.0D;
        maxAge = 200;
    }

    public override void renderParticle(Tessellator t, float partialTick, float rotX, float rotY, float rotZ, float upX, float upZ)
    {
        float lifeProgress = ((float)localAge + partialTick) / (float)maxAge;
        lifeProgress *= lifeProgress;
        float alpha = 2.0F - lifeProgress * 2.0F;
        if (alpha > 1.0F)
        {
            alpha = 1.0F;
        }

        alpha *= 0.2F;
        GLManager.GL.Disable(GLEnum.Lighting);
        float footprintSize = 2.0F / 16.0F;
        float renderX = (float)(X - interpPosX);
        float renderY = (float)(Y - interpPosY);
        float renderZ = (float)(Z - interpPosZ);
        float brightness = World.Lighting.GetLuminance(MathHelper.Floor(X), MathHelper.Floor(Y), MathHelper.Floor(Z));
        textureManager.BindTexture(textureManager.GetTextureId("/misc/footprint.png"));
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        t.startDrawingQuads();
        t.setColorRGBA_F(brightness, brightness, brightness, alpha);
        t.addVertexWithUV((double)(renderX - footprintSize), (double)renderY, (double)(renderZ + footprintSize), 0.0D, 1.0D);
        t.addVertexWithUV((double)(renderX + footprintSize), (double)renderY, (double)(renderZ + footprintSize), 1.0D, 1.0D);
        t.addVertexWithUV((double)(renderX + footprintSize), (double)renderY, (double)(renderZ - footprintSize), 1.0D, 0.0D);
        t.addVertexWithUV((double)(renderX - footprintSize), (double)renderY, (double)(renderZ - footprintSize), 0.0D, 0.0D);
        t.draw();
        GLManager.GL.Disable(GLEnum.Blend);
        GLManager.GL.Enable(GLEnum.Lighting);
    }

    public override void Tick()
    {
        ++localAge;
        if (localAge == maxAge)
        {
            MarkDead();
        }

    }

    public override int getFXLayer()
    {
        return 3;
    }
}
