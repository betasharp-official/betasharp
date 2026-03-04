using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core;

namespace BetaSharp.Client.Guis;

public class Slider : Control
{
    public override bool Focusable => true;
    public float Value;
    protected float Min;
    protected float Max;
    protected float Step;
    protected float DefaultValue;
    private bool _dragging;
    private long _lastClickTime;
    private const long DoubleClickThresholdMs = 400;

    public Slider(int x, int y, float value, float min, float max, float step = 0, float defaultValue = 0)
        : base(x, y, 150, 20)
    {
        Value = value;
        Min = min;
        Max = max;
        Step = step;
        DefaultValue = defaultValue == 0 ? min : defaultValue;
    }

    protected float NormalizedValue => Math.Clamp((Value - Min) / (Max - Min), 0f, 1f);

    protected virtual void OnValueChanged(float newValue) { }

    protected virtual void UpdateText() { }

    private void SetValueFromMouseX(int mouseX)
    {
        if (!Enabled) return;

        Point abs = AbsolutePosition;
        float percentage = (mouseX - (abs.X + 4)) / (float)(Width - 8);
        percentage = Math.Clamp(percentage, 0, 1);

        float newValue = Min + percentage * (Max - Min);
        if (Step > 0f)
        {
            newValue = MathF.Round(newValue / Step) * Step;
        }
        newValue = Math.Clamp(newValue, Min, Max);

        Value = newValue;
        OnValueChanged(newValue);
        UpdateText();
    }

    protected override void OnMousePress(MouseEventArgs e)
    {
        if (!Enabled) return;
        Minecraft.INSTANCE.sndManager.PlaySoundFX("random.click", 1.0F, 1.0F);

        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (currentTime - _lastClickTime < DoubleClickThresholdMs)
        {
            Value = DefaultValue;
            OnValueChanged(DefaultValue);
            UpdateText();
            _lastClickTime = 0;
        }
        else
        {
            SetValueFromMouseX(e.X);
            _lastClickTime = currentTime;
        }
    }

    protected override void OnMouseDrag(MouseEventArgs e)
    {
        SetValueFromMouseX(e.X);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        var mc = Minecraft.INSTANCE;
        TextRenderer font = mc.fontRenderer;

        mc.textureManager.BindTexture(mc.textureManager.GetTextureId("/gui/gui.png"));
        GLManager.GL.Color4(1, 1, 1, 1);

        // Left half of background
        DrawTextureRegion(X, Y, 0, 46, Width / 2, Height);
        // Right half of background
        DrawTextureRegion(X + Width / 2, Y, 200 - Width / 2, 46, Width / 2, Height);

        // Grabber position based on normalized value
        int grabberX = X + (int)(NormalizedValue * (Width - 8));
        // Left half of grabber
        DrawTextureRegion(grabberX, Y, 0, 66, 4, 20);
        // Right half of grabber
        DrawTextureRegion(grabberX + 4, Y, 196, 66, 4, 20);

        bool hovered = PointInBounds(e.MouseX, e.MouseY);
        uint color = !Enabled ? 0xA0A0A0u : (hovered ? 0xFFFFA0u : 0xE0E0E0u);
        Gui.DrawCenteredString(font, Text, X + Width / 2, Y + (Height - 8) / 2, color);
    }
}
