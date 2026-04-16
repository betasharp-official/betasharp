using BetaSharp.Client.Rendering.Core;

namespace BetaSharp.Client.Rendering.Particles;

public interface ISpecialParticle
{
    bool IsDead { get; }
    void Tick();
    void Render(Tessellator t, float partialTick, double interpX, double interpY, double interpZ);
}
