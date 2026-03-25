using BetaSharp.Client.Guis.Controls;
using BetaSharp.Stats;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Guis;

public class GuiIngameMenu : Screen
{
    private int _saveStepTimer = 0;
    private int _menuTickCounter = 0;

    public GuiIngameMenu()
    {
        _saveStepTimer = 0;
        Children.Clear();

        int verticalOffset = -16;
        int centerX = EffectiveWidth / 2;
        int centerY = EffectiveHeight / 4;
        int buttonLeft = centerX - 100;

        string quitText = (MC.isMultiplayerWorld() && MC.internalServer == null) ? "Disconnect" : "Save and quit to title";

        Control container = new(buttonLeft, centerY + verticalOffset + 24, 200, 116);
        Button backToGameButton = new(0, 0, "Back to game");
        Button achievementsButton = new(0, 24, StatCollector.TranslateToLocal("gui.achievements"))
        {
            EffectiveSize = new(98, 20),
        };
        Button statsButton = new(102, 24, StatCollector.TranslateToLocal("gui.stats"))
        {
            EffectiveSize = new(98, 20),
        };
        Button optionsButton = new(0, 72, "Options...");
        Button quitButton = new(0, 96, quitText);

        backToGameButton    .Clicked += (_, _) => MC.OpenScreen(null);
        achievementsButton  .Clicked += (_, _) => MC.OpenScreen(new GuiAchievements(MC.statFileWriter));
        statsButton         .Clicked += (_, _) => MC.OpenScreen(new GuiStats(this, MC.statFileWriter));
        optionsButton       .Clicked += (_, _) => MC.OpenScreen(new GuiOptions(this, MC.options));
        quitButton          .Clicked += QuitClicked;

        container.AddChildren(quitButton, backToGameButton, optionsButton, achievementsButton, statsButton);
        AddChild(container);
    }

    private void QuitClicked(object? o, MouseEventArgs e)
    {
        MC.statFileWriter.WriteStat(Stats.Stats.LeaveGameStat, 1);
        if (MC.isMultiplayerWorld())
        {
            MC.world.Disconnect();
        }

        MC.stopInternalServer();
        MC.changeWorld(null);
        MC.OpenScreen(new GuiMainMenu());
    }

    protected override void OnTick()
    {
        ++_menuTickCounter;
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();

        bool isSavingActive = !MC.world.attemptSaving(_saveStepTimer++);

        if (isSavingActive || _menuTickCounter < 20)
        {
            float pulse = (_menuTickCounter % 10 + e.TickDelta) / 10.0F;
            pulse = MathHelper.Sin(pulse * (float)Math.PI * 2.0F) * 0.2F + 0.8F;
            int color = (int)(255.0F * pulse);
            Gui.DrawString(FontRenderer, "Saving level..", 8, EffectiveHeight - 16, (uint)(color << 16 | color << 8 | color));
        }

        Gui.DrawCenteredString(FontRenderer, "Game menu", EffectiveWidth / 2, 40, 0xFFFFFF);
    }
}
