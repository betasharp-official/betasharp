using System.ComponentModel;

namespace BetaSharp.Client.Guis.Debug.Components;

[DisplayName("Particles")]
[Description("Shows particle stats.")]
public class DebugParticles : DebugComponent
{
    public DebugParticles() { }

    public override void Draw(DebugContext ctx)
    {
        ctx.String(ctx.Game.getParticleDebugInfo());
    }

    public override DebugComponent Duplicate()
    {
        return new DebugParticles()
        {
            Right = Right
        };
    }
}
