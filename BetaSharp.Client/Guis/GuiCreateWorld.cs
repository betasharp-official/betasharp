using BetaSharp.Client.Input;
using BetaSharp.Stats;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Storage;
using java.lang;

namespace BetaSharp.Client.Guis;

public class GuiCreateWorld : GuiScreen
{
    private const int ButtonCreate = 0;
    private const int ButtonCancel = 1;

    private readonly GuiScreen _parentScreen;
    private GuiTextField _textboxWorldName;
    private GuiTextField _textboxSeed;
    private string _folderName;
    private bool _createClicked;

    public GuiCreateWorld(GuiScreen parentScreen)
    {
        this._parentScreen = parentScreen;
    }

    public override void UpdateScreen()
    {
        _textboxWorldName.updateCursorCounter();
        _textboxSeed.updateCursorCounter();
    }

    public override void InitGui()
    {
        TranslationStorage translations = TranslationStorage.Instance;
        Keyboard.enableRepeatEvents(true);

        int centerX = Width / 2;
        int centerY = Height / 4;

        _textboxWorldName = new GuiTextField(this, FontRenderer, centerX - 100, centerY, 200, 20, translations.TranslateKey("selectWorld.newWorld"))
        {
            IsFocused = true
        };
        _textboxWorldName.SetMaxStringLength(32);
        _textboxSeed = new GuiTextField(this, FontRenderer, centerX - 100, centerY + 56, 200, 20, "");

        _controlList.Clear();
        _controlList.Add(new GuiButton(ButtonCreate, centerX - 100, centerY + 96 + 12, translations.TranslateKey("selectWorld.create")));
        _controlList.Add(new GuiButton(ButtonCancel, centerX - 100, centerY + 120 + 12, translations.TranslateKey("gui.cancel")));

        UpdateFolderName();
    }

    private void UpdateFolderName()
    {
        _folderName = _textboxWorldName.GetText().Trim();
        char[] invalidCharacters = ChatAllowedCharacters.allowedCharactersArray;
        int charCount = invalidCharacters.Length;

        for (int i = 0; i < charCount; ++i)
        {
            char invalidChar = invalidCharacters[i];
            _folderName = _folderName.Replace(invalidChar, '_');
        }

        if (string.IsNullOrEmpty(_folderName))
        {
            _folderName = "World";
        }

        _folderName = GenerateUnusedFolderName(mc.getSaveLoader(), _folderName);
    }

    public static string GenerateUnusedFolderName(IWorldStorageSource worldStorage, string baseFolderName)
    {
        while (worldStorage.GetProperties(baseFolderName) != null)
        {
            baseFolderName = baseFolderName + "-";
        }

        return baseFolderName;
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
                    mc.displayGuiScreen(_parentScreen);
                    break;
                case ButtonCreate:
                    {
                        if (_createClicked)
                        {
                            return;
                        }

                        _createClicked = true;
                        long worldSeed = new JavaRandom().NextLong();
                        string seedInput = _textboxSeed.GetText();
                        if (!string.IsNullOrEmpty(seedInput))
                        {
                            try
                            {
                                long parsedSeed = Long.parseLong(seedInput);
                                if (parsedSeed != 0L)
                                {
                                    worldSeed = parsedSeed;
                                }
                            }
                            catch (NumberFormatException)
                            {
                                // Java based string hashing
                                int hash = 0;
                                foreach (char c in seedInput)
                                {
                                    hash = 31 * hash + c;
                                }
                                worldSeed = hash;
                            }
                        }

                        mc.statFileWriter.ReadStat(Stats.Stats.CreateWorldStat, 1);
                        mc.playerController = new PlayerControllerSP(mc);
                        mc.startWorld(_folderName, _textboxWorldName.GetText(), worldSeed);
                        break;
                    }
            }
        }
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        if (_textboxWorldName.IsFocused)
        {
            _textboxWorldName.textboxKeyTyped(eventChar, eventKey);
        }
        else
        {
            _textboxSeed.textboxKeyTyped(eventChar, eventKey);
        }

        if (eventChar == 13)
        {
            ActionPerformed(_controlList[0]);
        }

        _controlList[0].Enabled = _textboxWorldName.GetText().Length > 0;
        UpdateFolderName();
    }

    protected override void MouseClicked(int x, int y, int button)
    {
        base.MouseClicked(x, y, button);
        _textboxWorldName.MouseClicked(x, y, button);
        _textboxSeed.MouseClicked(x, y, button);
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        TranslationStorage translations = TranslationStorage.Instance;

        int centerX = Width / 2;
        int centerY = Height / 4;

        DrawDefaultBackground();
        DrawCenteredString(FontRenderer, translations.TranslateKey("selectWorld.create"), centerX, centerY - 60 + 20, Color.White);
        DrawString(FontRenderer, translations.TranslateKey("selectWorld.enterName"), centerX - 100, centerY - 10, Color.GrayA0);
        DrawString(FontRenderer, $"{translations.TranslateKey("selectWorld.resultFolder")} {_folderName}", centerX - 100, centerY + 24, Color.GrayA0);
        DrawString(FontRenderer, translations.TranslateKey("selectWorld.enterSeed"), centerX - 100, centerY + 56 - 12, Color.GrayA0);
        DrawString(FontRenderer, translations.TranslateKey("selectWorld.seedInfo"), centerX - 100, centerY + 56 + 24, Color.GrayA0);
        _textboxWorldName.DrawTextBox();
        _textboxSeed.DrawTextBox();
        base.Render(mouseX, mouseY, partialTicks);
    }

    public override void SelectNextField()
    {
        if (_textboxWorldName.IsFocused)
        {
            _textboxWorldName.SetFocused(false);
            _textboxSeed.SetFocused(true);
        }
        else
        {
            _textboxWorldName.SetFocused(true);
            _textboxSeed.SetFocused(false);
        }

    }
}
