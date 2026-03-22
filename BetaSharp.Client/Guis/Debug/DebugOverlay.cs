using System;
using System.Collections.Generic;
using System.Text;

namespace BetaSharp.Client.Guis.Debug;

public class DebugOverlay
{
    private readonly BetaSharp _game;
    public readonly DebugContext Context;

    public DebugOverlay(BetaSharp game)
    {
        _game = game;
        Context = new DebugContext(game);
    }

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
