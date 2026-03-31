using System.ComponentModel;

namespace BetaSharp.Client.Debug.Components;

[DisplayName("World Info")]
[Description("Shows world debug info.")]
public class DebugWorld : DebugComponent
{
    public DebugWorld() { }

    public override IEnumerable<DebugRowData> GetRows(DebugContext ctx)
    {
        yield return new DebugRowData(ctx.Game.WorldDebugInfo);
    }

    public override DebugComponent Duplicate()
    {
        return new DebugWorld()
        {
            Right = Right
        };
    }
}
