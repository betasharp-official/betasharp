using BetaSharp.Client.Options;

namespace BetaSharp.Client.Guis.Controls;

public class ToggleButton : Button
{
    private readonly BoolOption _option;
    private long _lastClickTime;
    private const long DoubleClickThresholdMs = 400;
    public GameOption Option => _option;

    public ToggleButton(int x, int y, BoolOption option)
        : base(x, y, 150, option.GetDisplayString(TranslationStorage.Instance))
    {
        _option = option;
    }

    protected override void OnClick(MouseEventArgs e)
    {
        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (currentTime - _lastClickTime < DoubleClickThresholdMs)
        {
            _option.ResetToDefault();
            _lastClickTime = 0;
        }
        else
        {
            _option.Toggle();
            _lastClickTime = currentTime;
        }
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        Text = _option.GetDisplayString(TranslationStorage.Instance);
    }
}
