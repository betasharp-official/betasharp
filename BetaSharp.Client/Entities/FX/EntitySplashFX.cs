using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Entities.FX;

public class EntitySplashFX : EntityRainFX
{

    public EntitySplashFX(IWorldContext world, double x, double y, double z, double velocityX, double velocityY, double velocityZ) : base(world, x, y, z)
    {
        particleGravity = 0.04F;
        ++particleTextureIndex;
        if (velocityY == 0.0D && (velocityX != 0.0D || velocityZ != 0.0D))
        {
            base.velocityX = velocityX;
            base.velocityY = velocityY + 0.1D;
            base.velocityZ = velocityZ;
        }

    }
}
