using System.ComponentModel;

namespace BetaSharp.Client.Guis.Debug.Components;

[DisplayName("Server")]
[Description("Shows server info.")]
public class DebugServer : DebugComponent
{
    public DebugServer() { }

    public override void Draw(DebugContext ctx)
    {
        if (ctx.Game.internalServer != null)
        {
            ctx.String($"Integrated server @ {ctx.Game.internalServer.Tps:F1}/20 TPS");
        }
    }

    public override DebugComponent Duplicate()
    {
        return new DebugServer()
        {
            Right = Right
        };
    }
}
