using System;
using System.Collections.Generic;
using System.Text;

namespace BetaSharp.Client.Guis.Debug;

/// <summary>
/// Base class for all debug components.
/// </summary>
public abstract class DebugComponent
{
    /// <summary>
    /// Get or set if the component is on the right side of the debug overlay.
    /// </summary>
    public bool Right { get; set; }

    /// <summary>
    /// Draw this component using a <see cref="DebugComponent"/>
    /// </summary>
    /// <param name="context"></param>
    public abstract void Draw(DebugContext context);

    /// <summary>
    /// Duplicate this component.
    /// </summary>
    /// <returns></returns>
    public abstract DebugComponent Duplicate();
}
