using System.ComponentModel;

namespace BetaSharp.Client.Debug.Components;

[DisplayName("Separator")]
[Description("Visual separator between components.")]
public class DebugSeparator : DebugComponent
{
    public DebugSeparator() { }

    public override IEnumerable<DebugRowData> GetRows(DebugContext ctx)
    {
        yield return DebugRowData.Spacer();
    }

    public override DebugComponent Duplicate()
    {
        return new DebugSeparator()
        {
            Right = Right
        };
    }
}
