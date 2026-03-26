using BetaSharp.Client.Input;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Client.Guis;

public class GuiCreateWorld : GuiScreen
{
    private const int ButtonCreate = 0;
    private const int ButtonCancel = 1;
    private const int ButtonMoreOptions = 2;
    private const int ButtonWorldType = 3;
    private const int ButtonCustomizeFlat = 4;

    private readonly GuiScreen _parentScreen;
    private GuiTextField _textboxWorldName;
    private GuiTextField _textboxSeed;
    private string _folderName;
    private bool _createClicked;

    private bool _moreOptions = false;
    private WorldType _selectedWorldType = WorldType.Default;
    public string GeneratorOptions { get; set; } = "";
    private GuiButton? _btnWorldType;
    private GuiButton? _btnCustomize;

    public GuiCreateWorld(GuiScreen parentScreen)
    {
        _parentScreen = parentScreen;
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

        _controlList.Clear();
        GuiButton btnCreate, btnCancel;
        _controlList.Add(btnCreate = new GuiButton(ButtonCreate, Width / 2 - 155, Height - 28, 150, 20, translations.TranslateKey("selectWorld.create")));
        _controlList.Add(btnCancel = new GuiButton(ButtonCancel, Width / 2 + 5, Height - 28, 150, 20, translations.TranslateKey("gui.cancel")));

        btnCreate.Visible = !_moreOptions;
        btnCancel.Visible = !_moreOptions;

        const int moreOptionsY = 150;
        const int worldTypeY = 110;

        _controlList.Add(_btnWorldType = new GuiButton(ButtonWorldType, Width / 2 - 75, worldTypeY, 150, 20, "World Type"));
        _btnWorldType.Visible = _moreOptions;

        _controlList.Add(_btnCustomize = new GuiButton(ButtonCustomizeFlat, Width / 2 - 75, 130, 150, 20, "Customize"));
        _btnCustomize.Visible = _moreOptions && _selectedWorldType == WorldType.Flat;

        int moreOptionsBtnY = _moreOptions ? Height - 28 : moreOptionsY;
        string moreOptionsText = _moreOptions ? "Done" : "More World Options...";
        _controlList.Add(new GuiButton(ButtonMoreOptions, Width / 2 - 75, moreOptionsBtnY, 150, 20, moreOptionsText));

        string oldName = _textboxWorldName?.GetText() ?? translations.TranslateKey("selectWorld.newWorld");
        _textboxWorldName = new GuiTextField(this, FontRenderer, Width / 2 - 100, 60, 200, 20, oldName);
        _textboxWorldName.SetMaxStringLength(32);

        string oldSeed = _textboxSeed?.GetText() ?? "";
        _textboxSeed = new GuiTextField(this, FontRenderer, Width / 2 - 100, 60, 200, 20, oldSeed);

        if (_moreOptions) _textboxSeed.IsFocused = true;
        else _textboxWorldName.IsFocused = true;

        UpdateFolderName();
        UpdateButtonText();
    }

    private void UpdateButtonText()
    {
        if (_btnWorldType != null)
        {
            _btnWorldType.DisplayString = "World Type: " + _selectedWorldType.DisplayName;
        }
    }

    public void SetWorldType(WorldType type)
    {
        _selectedWorldType = type;
        UpdateButtonText();
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

        _folderName = GenerateUnusedFolderName(Game.getSaveLoader(), _folderName);
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
                    Game.displayGuiScreen(_parentScreen);
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
                                long parsedSeed = long.Parse(seedInput);
                                if (parsedSeed != 0L)
                                {
                                    worldSeed = parsedSeed;
                                }
                            }
                            catch (Exception)
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

                        Game.statFileWriter.ReadStat(Stats.Stats.CreateWorldStat, 1);
                        Game.playerController = new PlayerControllerSP(Game);

                        WorldSettings settings = new(worldSeed, _selectedWorldType, GeneratorOptions);

                        Game.startWorld(_folderName, _textboxWorldName.GetText(), settings);
                        break;
                    }
                case ButtonMoreOptions:
                    _moreOptions = !_moreOptions;
                    InitGui();
                    break;
                case ButtonWorldType:
                    Game.displayGuiScreen(new GuiSelectWorldType(this, _selectedWorldType));
                    break;
                case ButtonCustomizeFlat:
                    Game.displayGuiScreen(new GuiCreateFlatWorld(this, GeneratorOptions));
                    break;
            }
        }
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        if (eventKey == Keyboard.KEY_ESCAPE)
        {
            if (_moreOptions)
            {
                _moreOptions = false;
                InitGui();
            }
            else
            {
                Game.displayGuiScreen(_parentScreen);
            }
            return;
        }

        if (_textboxWorldName.IsFocused && !_moreOptions)
        {
            _textboxWorldName.textboxKeyTyped(eventChar, eventKey);
        }
        else if (_textboxSeed.IsFocused && _moreOptions)
        {
            _textboxSeed.textboxKeyTyped(eventChar, eventKey);
        }

        if (eventChar == 13) // Enter
        {
            ActionPerformed(_controlList[0]);
        }

        _controlList[0].Enabled = _textboxWorldName.GetText().Length > 0;
        UpdateFolderName();
    }

    protected override void MouseClicked(int x, int y, int button)
    {
        base.MouseClicked(x, y, button);
        if (!_moreOptions)
        {
            _textboxWorldName.MouseClicked(x, y, button);
        }
        else
        {
            _textboxSeed.MouseClicked(x, y, button);
        }
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        TranslationStorage translations = TranslationStorage.Instance;

        DrawDefaultBackground();
        DrawCenteredString(FontRenderer, translations.TranslateKey("selectWorld.create"), Width / 2, 20, Color.White);

        if (!_moreOptions)
        {
            DrawString(FontRenderer, translations.TranslateKey("selectWorld.enterName"), Width / 2 - 100, 47, Color.GrayA0);
            DrawString(FontRenderer, translations.TranslateKey("selectWorld.resultFolder") + " " + _folderName, Width / 2 - 100, 85, Color.GrayA0);
            _textboxWorldName.DrawTextBox();
        }
        else
        {
            DrawString(FontRenderer, translations.TranslateKey("selectWorld.enterSeed"), Width / 2 - 100, 47, Color.GrayA0);
            DrawString(FontRenderer, translations.TranslateKey("selectWorld.seedInfo"), Width / 2 - 100, 85, Color.GrayA0);
            _textboxSeed.DrawTextBox();
        }

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
