using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Entities.FX;

public class EntityBubbleFX : EntityFX
{

    public EntityBubbleFX(IWorldContext world, double x, double y, double z, double velocityX, double velocityY, double velocityZ) : base(world, x, y, z, velocityX, velocityY, velocityZ)
    {
        particleRed = 1.0F;
        particleGreen = 1.0F;
        particleBlue = 1.0F;
        particleTextureIndex = 32;
        setBoundingBoxSpacing(0.02F, 0.02F);
        particleScale *= random.NextFloat() * 0.6F + 0.2F;
        base.velocityX = velocityX * (double)0.2F + (double)((float)(Random.Shared.NextDouble() * 2.0D - 1.0D) * 0.02F);
        base.velocityY = velocityY * (double)0.2F + (double)((float)(Random.Shared.NextDouble() * 2.0D - 1.0D) * 0.02F);
        base.velocityZ = velocityZ * (double)0.2F + (double)((float)(Random.Shared.NextDouble() * 2.0D - 1.0D) * 0.02F);
        particleMaxAge = (int)(8.0D / (Random.Shared.NextDouble() * 0.8D + 0.2D));
    }

    public override void tick()
    {
        prevX = x;
        prevY = y;
        prevZ = z;
        velocityY += 0.002D;
        move(velocityX, velocityY, velocityZ);
        velocityX *= (double)0.85F;
        velocityY *= (double)0.85F;
        velocityZ *= (double)0.85F;
        if (world.Reader.GetMaterial(MathHelper.Floor(x), MathHelper.Floor(y), MathHelper.Floor(z)) != Material.Water)
        {
            markDead();
        }

        if (particleMaxAge-- <= 0)
        {
            markDead();
        }

    }
}
