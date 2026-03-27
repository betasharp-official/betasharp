using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls;

public class DebugMenu(BetaSharp game) : UIElement
{
    private readonly BetaSharp _game = game;

    public override void Render(UIRenderer renderer)
    {
        if (!_game.options.ShowDebugInfo) return;
        if (_game.player == null || _game.world == null) return;

        // The debug system handles its own rendering
        _game.componentsStorage.Overlay.Context.GCMonitor.AllowUpdating = true;
        _game.componentsStorage.Overlay.Draw();

        base.Render(renderer);
    }
}
