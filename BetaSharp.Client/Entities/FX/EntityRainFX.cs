using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Entities.FX;

public class EntityRainFX : EntityFX
{

    public EntityRainFX(IWorldContext world, double x, double y, double z) : base(world, x, y, z, 0.0D, 0.0D, 0.0D)
    {
        velocityX *= (double)0.3F;
        velocityY = (double)((float)Random.Shared.NextDouble() * 0.2F + 0.1F);
        velocityZ *= (double)0.3F;
        particleRed = 1.0F;
        particleGreen = 1.0F;
        particleBlue = 1.0F;
        particleTextureIndex = 19 + random.NextInt(4);
        setBoundingBoxSpacing(0.01F, 0.01F);
        particleGravity = 0.06F;
        particleMaxAge = (int)(8.0D / (Random.Shared.NextDouble() * 0.8D + 0.2D));
    }

    public override void renderParticle(Tessellator t, float partialTick, float rotX, float rotY, float rotZ, float upX, float upZ)
    {
        base.renderParticle(t, partialTick, rotX, rotY, rotZ, upX, upZ);
    }

    public override void tick()
    {
        prevX = x;
        prevY = y;
        prevZ = z;
        velocityY -= (double)particleGravity;
        move(velocityX, velocityY, velocityZ);
        velocityX *= (double)0.98F;
        velocityY *= (double)0.98F;
        velocityZ *= (double)0.98F;
        if (particleMaxAge-- <= 0)
        {
            markDead();
        }

        if (onGround)
        {
            if (Random.Shared.NextDouble() < 0.5D)
            {
                markDead();
            }

            velocityX *= (double)0.7F;
            velocityZ *= (double)0.7F;
        }

        Material material = world.Reader.GetMaterial(MathHelper.Floor(x), MathHelper.Floor(y), MathHelper.Floor(z));
        if (material.IsFluid || material.IsSolid)
        {
            double height = (double)((float)(MathHelper.Floor(y) + 1) - BlockFluid.getFluidHeightFromMeta(world.Reader.GetBlockMeta(MathHelper.Floor(x), MathHelper.Floor(y), MathHelper.Floor(z))));
            if (y < height)
            {
                markDead();
            }
        }

    }
}
