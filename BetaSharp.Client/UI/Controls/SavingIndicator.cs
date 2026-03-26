using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.UI.Controls;

public class SavingIndicator : UIElement
{
    private int _saveStepTimer = 0;
    private float _tickCounter = 0;

    public override void Update(float partialTicks)
    {
        base.Update(partialTicks);
        _tickCounter += 1.0f;
    }

    public override void Render(UIRenderer renderer)
    {
        BetaSharp game = BetaSharp.Instance;

        bool isSavingActive = !game.world.AttemptSaving(_saveStepTimer++);

        if (isSavingActive || _tickCounter < 20)
        {
            float pulse = (_tickCounter % 10) / 10.0F;
            pulse = MathHelper.Sin(pulse * (float)Math.PI * 2.0F) * 0.2F + 0.8F;
            int colorVal = (int)(255.0F * pulse);
            Color color = Color.FromRgb((uint)(colorVal << 16 | colorVal << 8 | colorVal));

            renderer.DrawText("Saving level..", 0, 0, color);
        }
    }
}
