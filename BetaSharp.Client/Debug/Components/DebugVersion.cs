using System.ComponentModel;

namespace BetaSharp.Client.Debug.Components;

[DisplayName("Version")]
[Description("Shows the current BetaSharp version.")]
public class DebugVersion : DebugComponent
{
    public DebugVersion() { }

    public override IEnumerable<DebugRowData> GetRows(DebugContext ctx)
    {
        yield return new DebugRowData("BetaSharp " + BetaSharp.Version);
    }

    public override DebugComponent Duplicate()
    {
        return new DebugVersion()
        {
            Right = Right
        };
    }
}
