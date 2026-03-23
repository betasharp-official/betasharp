using System.ComponentModel;

namespace BetaSharp.Client.Guis.Debug.Components;


[DisplayName("World Info")]
[Description("Shows world debug info.")]
public class DebugWorld : DebugComponent
{
    public DebugWorld() { }

    public override void Draw(DebugContext ctx)
    {
        ctx.String(ctx.Game.getWorldDebugInfo());
    }

    public override DebugComponent Duplicate()
    {
        return new DebugWorld()
        {
            Right = Right
        };
    }
}
