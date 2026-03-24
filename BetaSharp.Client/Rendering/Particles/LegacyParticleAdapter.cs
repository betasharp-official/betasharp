using BetaSharp.Client.Entities.FX;
using BetaSharp.Client.Rendering.Core;

namespace BetaSharp.Client.Rendering.Particles;

public class LegacyParticleAdapter : ISpecialParticle
{
    private readonly EntityFX _fx;

    public LegacyParticleAdapter(EntityFX fx)
    {
        _fx = fx;
    }

    public bool IsDead => _fx.dead;

    public void Tick() => _fx.tick();

    public void Render(Tessellator t, float partialTick, double interpX, double interpY, double interpZ)
    {
        EntityFX.interpPosX = interpX;
        EntityFX.interpPosY = interpY;
        EntityFX.interpPosZ = interpZ;
        _fx.renderParticle(t, partialTick, 0, 0, 0, 0, 0);
    }
}
