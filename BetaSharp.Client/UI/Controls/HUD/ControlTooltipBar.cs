using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.HUD;

public class ControlTooltipBar : UIElement
{
    private readonly BetaSharp _game;
    private readonly UIScreen? _screen; // null = in-game tips, otherwise GUI tips for that screen
    private readonly List<ActionTip> _tips = [];

    private const int IconSize = 16;
    private const int TextVerticalOffset = 4;
    private const int Spacing = 10;

    public ControlTooltipBar(BetaSharp game, UIScreen? screen = null)
    {
        _game = game;
        _screen = screen;
        IsHitTestVisible = false;
        Style.Height = IconSize;
    }

    public override void Render(UIRenderer renderer)
    {
        _tips.Clear();

        if (!_game.isControllerMode || _game.options.HideGUI)
        {
            base.Render(renderer);
            return;
        }

        if (_screen == null)
        {
            if (_game.currentScreen != null)
            {
                base.Render(renderer);
                return;
            }
            ControlTooltip.PopulateInGameTips(_game, _tips);
        }
        else
        {
            ControlTooltip.PopulateGuiTips(_screen, _tips);
        }

        if (_tips.Count == 0)
        {
            base.Render(renderer);
            return;
        }

        float x = 0;
        foreach (ActionTip tip in _tips)
        {
            string? assetPath = ControlTooltip.GetAssetPath(tip.Icon);
            if (assetPath != null)
            {
                TextureHandle texture = renderer.TextureManager.GetTextureId(assetPath);
                renderer.DrawTexture(texture, x, 0, IconSize, IconSize);
                x += IconSize + 4;
            }

            renderer.DrawText(tip.Action, x, TextVerticalOffset, Color.White);
            x += _game.fontRenderer.GetStringWidth(tip.Action) + Spacing;
        }

        base.Render(renderer);
    }
}
