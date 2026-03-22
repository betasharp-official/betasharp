using System;
using System.Collections.Generic;
using System.Text;

namespace BetaSharp.Client.Guis.Debug;

public abstract class DebugComponent
{
    public bool Right { get; set; }
    public abstract void Draw(DebugContext context);
    public abstract DebugComponent Duplicate();
}
