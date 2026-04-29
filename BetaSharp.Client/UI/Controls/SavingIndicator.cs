using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Util.Maths;
using static System.Net.Mime.MediaTypeNames;

namespace BetaSharp.Client.UI.Controls;

public class SavingIndicator(Func<bool> isSavingComplete) : UIElement
{
    private float _tickCounter = 0;

    public override bool DoTextMeasuring => true;

    public override void Update(float partialTicks)
    {
        base.Update(partialTicks);
        _tickCounter += 1.0f;
    }

    public override void Render(UIRenderer renderer)
    {
        bool isSavingActive = !isSavingComplete();

        if (isSavingActive || _tickCounter < 20)
        {
            float pulse = (_tickCounter % 10) / 10.0F;
            pulse = MathHelper.Sin(pulse * (float)Math.PI * 2.0F) * 0.2F + 0.8F;
            int colorVal = (int)(255.0F * pulse);
            Color color = Color.FromRgb((uint)(colorVal << 16 | colorVal << 8 | colorVal));

            renderer.DrawText("Saving level...", 0, 0, color);
        }
    }

    public override void Measure(MeasureContext context)
    {
        ComputedWidth = context.MeasureString("Saving level...");
        ComputedHeight = 8;
    }
}
