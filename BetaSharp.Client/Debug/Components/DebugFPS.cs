using System.ComponentModel;

namespace BetaSharp.Client.Debug.Components;

[DisplayName("FPS")]
[Description("Shows the current FPS.")]
public class DebugFPS : DebugComponent
{
    public DebugFPS() { }

    public override IEnumerable<DebugRowData> GetRows(DebugContext ctx)
    {
        yield return new DebugRowData(ctx.Game.debug);
    }

    public override DebugComponent Duplicate()
    {
        return new DebugFPS()
        {
            Right = Right
        };
    }
}
