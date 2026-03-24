using BetaSharp.Client.Input;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Client.Guis;

public class GuiRenameWorld : GuiScreen
{
    private const int ButtonRename = 0;
    private const int ButtonCancel = 1;

    private readonly GuiScreen parentScreen;
    private GuiTextField nameInputField;
    private readonly string worldFolderName;

    public GuiRenameWorld(GuiScreen parentScreen, string worldFolderName)
    {
        this.parentScreen = parentScreen;
        this.worldFolderName = worldFolderName;
    }

    public override void UpdateScreen()
    {
        nameInputField.updateCursorCounter();
    }

    public override void InitGui()
    {
        TranslationStorage translations = TranslationStorage.Instance;
        Keyboard.enableRepeatEvents(true);
        _controlList.Clear();
        _controlList.Add(new GuiButton(ButtonRename, Width / 2 - 100, Height / 4 + 96 + 12, translations.TranslateKey("selectWorld.renameButton")));
        _controlList.Add(new GuiButton(ButtonCancel, Width / 2 - 100, Height / 4 + 120 + 12, translations.TranslateKey("gui.cancel")));
        IWorldStorageSource worldStorage = Game.getSaveLoader();
        WorldProperties? worldProperties = worldStorage.GetProperties(worldFolderName);
        string currentWorldName = worldProperties?.LevelName ?? string.Empty;
        nameInputField = new GuiTextField(this, FontRenderer, Width / 2 - 100, 60, 200, 20, currentWorldName)
        {
            IsFocused = true
        };
        nameInputField.SetMaxStringLength(32);
    }

    public override void OnGuiClosed()
    {
        Keyboard.enableRepeatEvents(false);
    }

    protected override void ActionPerformed(GuiButton button)
    {
        if (button.Enabled)
        {
            switch (button.Id)
            {
                case ButtonCancel:
                    Game.displayGuiScreen(parentScreen);
                    break;
                case ButtonRename:
                    IWorldStorageSource worldStorage = Game.getSaveLoader();
                    worldStorage.Rename(worldFolderName, nameInputField.GetText().Trim());
                    Game.displayGuiScreen(parentScreen);
                    break;
            }
        }
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        nameInputField.textboxKeyTyped(eventChar, eventKey);
        _controlList[0].Enabled = nameInputField.GetText().Trim().Length > 0;
        if (eventChar == 13)
        {
            ActionPerformed(_controlList[0]);
        }

    }

    protected override void MouseClicked(int x, int y, int button)
    {
        base.MouseClicked(x, y, button);
        nameInputField.MouseClicked(x, y, button);
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        TranslationStorage translations = TranslationStorage.Instance;
        DrawDefaultBackground();
        DrawCenteredString(FontRenderer, translations.TranslateKey("selectWorld.renameTitle"), Width / 2, Height / 4 - 60 + 20, Color.White);
        DrawString(FontRenderer, translations.TranslateKey("selectWorld.enterName"), Width / 2 - 100, 47, Color.GrayA0);
        nameInputField.DrawTextBox();
        base.Render(mouseX, mouseY, partialTicks);
    }
}
