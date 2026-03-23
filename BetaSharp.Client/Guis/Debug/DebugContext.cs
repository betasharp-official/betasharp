using BetaSharp.Util;

namespace BetaSharp.Client.Guis.Debug;

public class DebugContext
{
    private const int PADDING = 2; // padding on the outside of text

    private int _leftY;
    private int _rightY;
    private int _scaledWidth;
    private bool _right;
    public readonly BetaSharp Game;

    public readonly GCMonitor GCMonitor;

    public DebugContext(BetaSharp game)
    {
        Game = game;

        GCMonitor = new GCMonitor();
    }

    public void Initialize()
    {
        _leftY = BetaSharp.hasPaidCheckTime > 0L ? 32 + PADDING : PADDING;
        _rightY = PADDING; // right side doesnt need it

        ScaledResolution scaled = new(Game.options, Game.displayWidth, Game.displayHeight);
        _scaledWidth = scaled.ScaledWidth;
    }

    public void String(string str, Color? color = null)
    {
        color ??= Color.White;

        int width = Game.fontRenderer.GetStringWidth(str);
        Color bg = new(128, 128, 128, 96);

        void LeftString()
        {
            Gui.DrawRect(0, _leftY, width + PADDING * 2, _leftY + 10, bg);
            Game.fontRenderer.DrawStringWithShadow(str, PADDING, _leftY + 1, (Color)color);

            _leftY += 10;
        }

        void RightString()
        {

            Gui.DrawRect(_scaledWidth - PADDING * 2 - width, _rightY, _scaledWidth, _rightY + 10, bg);
            Game.fontRenderer.DrawStringWithShadow(str, _scaledWidth - PADDING, _rightY + 1, (Color)color, SixLabors.Fonts.HorizontalAlignment.Right);

            _rightY += 10;
        }

        if (_right) RightString();
        else LeftString();
    }

    public void Seperator()
    {
        if (_right) _rightY += 10;
        else _leftY += 10;
    }

    public void DrawComponent(DebugComponent comp)
    {
        _right = comp.Right;

        comp.Draw(this);
    }
}
