using System.ComponentModel;
using System.Runtime.InteropServices;

namespace BetaSharp.Client.Debug.Components;

[DisplayName("Framework")]
[Description("Shows .NET version.")]
public class DebugFramework : DebugComponent
{
    public DebugFramework() { }

    public override IEnumerable<DebugRowData> GetRows(DebugContext ctx)
    {
        yield return new DebugRowData(RuntimeInformation.FrameworkDescription);
    }

    public override DebugComponent Duplicate()
    {
        return new DebugFramework()
        {
            Right = Right
        };
    }
}
