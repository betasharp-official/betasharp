using System.ComponentModel;

namespace BetaSharp.Client.Guis.Debug.Components;

[DisplayName("Version")]
[Description("Shows the current BetaSharp version.")]
public class DebugVersion : DebugComponent
{

    public DebugVersion() { }

    public override void Draw(DebugContext ctx)
    {
        ctx.String("BetaSharp " + BetaSharp.Version);
    }

    public override DebugComponent Duplicate()
    {
        return new DebugVersion()
        {
            Right = Right
        };
    }
}
