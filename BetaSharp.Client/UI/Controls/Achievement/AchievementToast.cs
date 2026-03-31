using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.Achievement;

public class AchievementToast : UIElement
{
    private global::BetaSharp.Achievement? _achievement;
    private string? _title;
    private string? _description;
    private long _startTime;
    private bool _isInfo;
    private const long Duration = 3000L;

    public AchievementToast()
    {
        Style.Width = 160;
        Style.Height = 32;
    }

    public void QueueAchievement(global::BetaSharp.Achievement ach)
    {
        _achievement = ach;
        _title = "Achievement get!";
        _description = ach.StatName;
        _startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _isInfo = false;
    }

    public void QueueInfo(global::BetaSharp.Achievement ach)
    {
        _achievement = ach;
        _title = ach.StatName;
        _description = ach.getTranslatedDescription();
        _startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 2500L;
        _isInfo = true;
    }

    public override void Render(UIRenderer renderer)
    {
        if (_achievement == null || _startTime == 0) return;

        long elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _startTime;
        if (!_isInfo && elapsed > Duration)
        {
            _startTime = 0;
            return;
        }

        double progress = elapsed / (double)Duration;
        double anim = CalculateAnim(progress);

        float y = (float)(-anim * 36);

        renderer.TextureManager.BindTexture(renderer.TextureManager.GetTextureId("/achievement/bg.png"));
        renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/achievement/bg.png"), 0, y, 96, 202, 160, 32);

        if (_isInfo)
        {
            renderer.DrawTextWrapped(_description ?? "", 30, y + 7, 126, Color.White);
        }
        else
        {
            renderer.DrawText(_title ?? "", 30, y + 7, Color.Yellow);
            renderer.DrawTextWrapped(_description ?? "", 30, y + 18, 126, Color.White);
        }

        renderer.DrawItem(_achievement.icon, 8, y + 8);
    }

    private static double CalculateAnim(double progress)
    {
        double p = progress * 2.0;
        if (p > 1.0) p = 2.0 - p;
        p *= 4.0;
        p = 1.0 - p;
        if (p < 0.0) p = 0.0;
        p *= p;
        p *= p;
        return p;
    }
}
