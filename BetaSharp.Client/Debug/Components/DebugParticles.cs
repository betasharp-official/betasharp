using System.ComponentModel;

namespace BetaSharp.Client.Debug.Components;

[DisplayName("Particles")]
[Description("Shows particle stats.")]
public class DebugParticles : DebugComponent
{
    public DebugParticles() { }

    public override IEnumerable<DebugRowData> GetRows(DebugContext ctx)
    {
        yield return new DebugRowData(ctx.Game.ParticleDebugInfo);
    }

    public override DebugComponent Duplicate()
    {
        return new DebugParticles()
        {
            Right = Right
        };
    }
}
