using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Stats;

namespace BetaSharp.Client.UI.Controls.Achievement;

public class AchievementCard : UIElement
{
    private readonly global::BetaSharp.Achievement _achievement;
    private readonly StatFileWriter _stats;

    public AchievementCard(global::BetaSharp.Achievement ach, StatFileWriter stats)
    {
        _achievement = ach;
        _stats = stats;

        Style.Height = 44;
        Style.Width = null; // Fill parent
        Style.MarginBottom = 6;
        Style.PaddingLeft = 44;
        Style.PaddingTop = 6;
    }

    public override void Render(UIRenderer renderer)
    {
        bool unlocked = _stats.HasAchievementUnlocked(_achievement);
        bool canUnlock = _stats.CanUnlockAchievement(_achievement);

        // Base background
        renderer.DrawRect(0, 0, ComputedWidth, ComputedHeight, new Color(0, 0, 0, 100));

        // Hover / Unlocked Tint
        if (unlocked)
        {
            renderer.DrawRect(0, 0, ComputedWidth, ComputedHeight, new Color(50, 150, 50, 40));
        }
        else if (IsHovered)
        {
            renderer.DrawRect(0, 0, ComputedWidth, ComputedHeight, Color.WhiteAlpha20);
        }

        DrawAchievementIcon(renderer, unlocked, canUnlock);

        // --- Titles and Description ---
        Color titleColor = unlocked ? (_achievement.isChallenge() ? Color.AchievementChallengeYellow : Color.White)
                                   : (canUnlock ? Color.GrayE0 : Color.Gray80);

        string name = _achievement.StatName;
        renderer.DrawText(name, 46, 8, titleColor);

        string? desc = _achievement.getTranslatedDescription();
        if (desc != null && canUnlock)
        {
            renderer.DrawTextWrapped(desc, 46, 22, ComputedWidth - 120, Color.GrayA0);
        }
        else if (!canUnlock)
        {
            string reqName = _achievement.parent?.StatName ?? "Unknown";
            renderer.DrawTextWrapped($"Requires: {reqName}", 46, 22, ComputedWidth - 120, Color.AchievementRequiresRed);
        }

        // --- Status Markers ---
        if (unlocked)
        {
            renderer.DrawText("UNLOCKED", ComputedWidth - 65, 16, Color.AchievementTakenBlue);
        }
        else if (_achievement.isChallenge())
        {
            renderer.DrawText("CHALLENGE", ComputedWidth - 75, 16, Color.AchievementChallengeYellow);
        }

        base.Render(renderer);
    }

    private void DrawAchievementIcon(UIRenderer renderer, bool unlocked, bool canUnlock)
    {
        TextureHandle bgTexture = renderer.TextureManager.GetTextureId("/achievement/bg.png");
        renderer.TextureManager.BindTexture(bgTexture);

        int iconX = 10;
        int iconY = (int)ComputedHeight / 2 - 13;

        if (_achievement.isChallenge()) renderer.DrawTexturedModalRect(bgTexture, iconX - 2, iconY - 2, 26, 202, 26, 26);
        else renderer.DrawTexturedModalRect(bgTexture, iconX - 2, iconY - 2, 0, 202, 26, 26);

        renderer.DrawItem(_achievement.icon, iconX + 3, iconY + 3);
    }
}
