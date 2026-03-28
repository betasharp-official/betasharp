using System.ComponentModel;
using BetaSharp.Client.Debug;

namespace BetaSharp.Client.Debug.Components;

[DisplayName("Separator")]
[Description("Visual separator between components.")]
public class DebugSeparator : DebugComponent
{
    public DebugSeparator() { }

    public override void Draw(DebugContext ctx)
    {
        ctx.Seperator();
    }

    public override DebugComponent Duplicate()
    {
        return new DebugSeparator()
        {
            Right = Right
        };
    }
}
