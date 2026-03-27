using BetaSharp.Stats;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Guis;

public class GuiIngameMenu : GuiScreen
{

    private int _saveStepTimer = 0;
    private int _menuTickCounter = 0;

    public override void InitGui()
    {
        _saveStepTimer = 0;
        _controlList.Clear();

        int verticalOffset = -16;
        int centerX = Width / 2;
        int centerY = Height / 4;

        string quitText = (Game.isMultiplayerWorld() && Game.internalServer == null) ? "Disconnect" : "Save and quit to title";

        _controlList.Add(new GuiButton(1, centerX - 100, centerY + 120 + verticalOffset, quitText));
        _controlList.Add(new GuiButton(4, centerX - 100, centerY + 24 + verticalOffset, "Back to Game"));
        _controlList.Add(new GuiButton(0, centerX - 100, centerY + 96 + verticalOffset, "Options..."));
        _controlList.Add(new GuiButton(5, centerX - 100, centerY + 48 + verticalOffset, 98, 20, StatCollector.TranslateToLocal("gui.achievements")));
        _controlList.Add(new GuiButton(6, centerX + 2, centerY + 48 + verticalOffset, 98, 20, StatCollector.TranslateToLocal("gui.stats")));
    }

    protected override void ActionPerformed(GuiButton btt)
    {
        if (btt.Id == 0)
        {
            Game.displayGuiScreen(new GuiOptions(this, Game.options));
        }

        if (btt.Id == 1)
        {
            Game.statFileWriter.ReadStat(Stats.Stats.LeaveGameStat, 1);
            if (Game.isMultiplayerWorld())
            {
                Game.world.Disconnect();
            }

            Game.stopInternalServer();
            Game.changeWorld(null);
            Game.options.ShowDebugInfo = false;
            Game.displayGuiScreen(new GuiMainMenu());
        }

        if (btt.Id == 4)
        {
            Game.displayGuiScreen(null);
            Game.setIngameFocus();
        }

        if (btt.Id == 5)
        {
            Game.displayGuiScreen(new GuiAchievements(Game.statFileWriter));
        }

        if (btt.Id == 6)
        {
            Game.displayGuiScreen(new GuiStats(this, Game.statFileWriter));
        }
    }

    public override void UpdateScreen()
    {
        base.UpdateScreen();
        ++_menuTickCounter;
    }

    public override void Render(int mouseX, int mouseY, float partialTick)
    {
        DrawDefaultBackground();

        bool isSavingActive = !Game.world.AttemptSaving(_saveStepTimer++);

        if (isSavingActive || _menuTickCounter < 20)
        {
            float pulse = (_menuTickCounter % 10 + partialTick) / 10.0F;
            pulse = MathHelper.Sin(pulse * (float)Math.PI * 2.0F) * 0.2F + 0.8F;
            int color = (int)(255.0F * pulse);
            DrawString(FontRenderer, "Saving level..", 8, Height - 16, Color.FromRgb((uint)(color << 16 | color << 8 | color)));
        }

        DrawCenteredString(FontRenderer, "Game menu", Width / 2, 40, Color.White);
        base.Render(mouseX, mouseY, partialTick);
    }
}
