using System;
using System.Collections.Generic;
using System.Text;

namespace BetaSharp.Client.Guis.Debug;

/// <summary>
/// Renderer for a List of <see cref="DebugComponent"></see>s
/// </summary>
public class DebugOverlay
{
    private readonly BetaSharp _game;
    public readonly DebugContext Context;

    public DebugOverlay(BetaSharp game)
    {
        _game = game;
        Context = new DebugContext(game);
    }

    /// <summary>
    /// List of components to render.
    /// </summary>
    public List<DebugComponent> Components = new List<DebugComponent>();

    public void Draw()
    {
        Context.Initialize();

        foreach (var component in Components)
        {
            Context.DrawComponent(component);
        }
    }
}
