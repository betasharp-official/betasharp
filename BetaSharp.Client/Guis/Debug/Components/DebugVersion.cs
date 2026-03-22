using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace BetaSharp.Client.Guis.Debug.Components;

[DisplayName("Version")]
[Description("Shows the current BetaSharp version.")]
public class DebugVersion : DebugComponent
{

    public DebugVersion() { }

    public override void Draw(DebugContext ctx)
    {
        ctx.String("BetaSharp 1.7.3");
    }

    public override DebugComponent Duplicate()
    {
        return new DebugVersion()
        {
            Right = Right
        };
    }
}
