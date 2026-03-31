using System.ComponentModel;

namespace BetaSharp.Client.Debug.Components;

[DisplayName("Server")]
[Description("Shows server info.")]
public class DebugServer : DebugComponent
{
    public DebugServer() { }

    public override IEnumerable<DebugRowData> GetRows(DebugContext ctx)
    {
        if (ctx.Game.InternalServer != null)
            yield return new DebugRowData($"Integrated server @ {ctx.Game.InternalServer.Tps:F1}/20 TPS");
    }

    public override DebugComponent Duplicate()
    {
        return new DebugServer()
        {
            Right = Right
        };
    }
}
