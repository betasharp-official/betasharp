using BetaSharp.Client.Options;

namespace BetaSharp.Client.Guis.Controls;

public class OptionsSlider : Slider
{
    private readonly FloatOption _option;
    public GameOption Option => _option;

    public OptionsSlider(int x, int y, FloatOption option)
        : base(x, y, option.Value, option.Min, option.Max, option.Step, option.DefaultValue)
    {
        _option = option;
        UpdateText();
    }

    protected override void OnValueChanged(float newValue)
    {
        _option.Set(newValue);
        // Sync back in case Set() applied clamping/stepping
        Value = _option.Value;
    }

    protected override void UpdateText()
    {
        Text = _option.GetDisplayString(TranslationStorage.Instance);
    }

    public void RefreshDisplay()
    {
        Value = _option.Value;
        UpdateText();
    }
}
