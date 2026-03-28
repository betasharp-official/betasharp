using System.ComponentModel;
using BetaSharp.Client.Debug;

namespace BetaSharp.Client.Debug.Components;

[DisplayName("FPS")]
[Description("Shows the current FPS.")]
public class DebugFPS : DebugComponent
{
    public DebugFPS() { }

    public override void Draw(DebugContext ctx)
    {
        ctx.String(ctx.Game.debug);
    }

    public override DebugComponent Duplicate()
    {
        return new DebugFPS()
        {
            Right = Right
        };
    }
}
