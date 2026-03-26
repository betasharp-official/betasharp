using System.ComponentModel;
using System.Runtime.InteropServices;

namespace BetaSharp.Client.Guis.Debug.Components;


[DisplayName("Framework")]
[Description("Shows .NET version.")]
public class DebugFramework : DebugComponent
{
    public DebugFramework() { }

    public override void Draw(DebugContext ctx)
    {
        ctx.String(RuntimeInformation.FrameworkDescription);
    }

    public override DebugComponent Duplicate()
    {
        return new DebugFramework()
        {
            Right = Right
        };
    }
}
