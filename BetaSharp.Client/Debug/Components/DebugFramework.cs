using System.ComponentModel;
using System.Runtime.InteropServices;
using BetaSharp.Client.Debug;

namespace BetaSharp.Client.Debug.Components;


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
