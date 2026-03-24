using BetaSharp.Client.Entities.FX;
using BetaSharp.Client.Rendering.Core;

namespace BetaSharp.Client.Rendering.Particles;

public class LegacyParticleAdapter(EntityFX fx) : ISpecialParticle
{
    public bool IsDead => fx.dead;
    public void Tick() => fx.tick();
    public void Render(Tessellator t, float partialTick, double interpX, double interpY, double interpZ)
    {
        EntityFX.interpPosX = interpX;
        EntityFX.interpPosY = interpY;
        EntityFX.interpPosZ = interpZ;
        fx.renderParticle(t, partialTick, 0, 0, 0, 0, 0);
    }
}
