using BetaSharp.Client.Options;

namespace BetaSharp.Client.Guis;

public class CycleButton : Button
{
    private readonly CycleOption _option;
    private long _lastClickTime;
    private const long DoubleClickThresholdMs = 400;
    public GameOption Option => _option;

    public CycleButton(int x, int y, CycleOption option)
        : base(x, y, option.GetDisplayString(TranslationStorage.Instance))
    {
        _option = option;
        Size = new(150, 20);
    }

    protected override void OnClick(MouseEventArgs e)
    {
        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (currentTime - _lastClickTime < DoubleClickThresholdMs)
        {
            // Double-click: reset to default
            _option.ResetToDefault();
            _lastClickTime = 0;
        }
        else
        {
            _option.Cycle();
            _lastClickTime = currentTime;
        }
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        Text = _option.GetDisplayString(TranslationStorage.Instance);
    }
}
