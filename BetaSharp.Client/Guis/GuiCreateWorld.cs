using BetaSharp.Client.Input;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Storage;
using java.lang;

namespace BetaSharp.Client.Guis;

public class GuiCreateWorld : Screen
{
    private const int ButtonCreate = 0;
    private const int ButtonCancel = 1;

    private readonly Screen _parentScreen;
    private TextField _textboxWorldName;
    private TextField _textboxSeed;
    private string _folderName;
    private bool _createClicked;

    public GuiCreateWorld(Screen parentScreen)
    {
        _parentScreen = parentScreen;

        TranslationStorage translations = TranslationStorage.Instance;
        Keyboard.enableRepeatEvents(true);

        int centerX = Width / 2;
        int centerY = Height / 4;

        _textboxWorldName = new(centerX - 100, centerY, FontRenderer, translations.TranslateKey("selectWorld.newWorld"))
        {
            Focused = true,
            MaxLength = 32,
        };
        _textboxSeed = new(centerX - 100, centerY + 56, FontRenderer, "");
        Button createButton = new(centerX - 100, centerY + 96 + 12, translations.TranslateKey("selectWorld.create")) { Enabled = false };
        Button cancelButton = new(centerX - 100, centerY + 120 + 12, translations.TranslateKey("gui.cancel"));

        _textboxWorldName.KeyInput += (_, e) =>
        {
            bool enabled = createButton.Enabled = _textboxWorldName.Text.Length > 0;

            if (e.KeyChar == Keyboard.KEY_EQUALS && enabled)
            {
                CreateWorld();
            }
        };
        createButton.Clicked += (_, _) => CreateWorld();
        cancelButton.Clicked += (_, _) => MC.OpenScreen(_parentScreen);

        AddChildren(_textboxWorldName, _textboxSeed, createButton, cancelButton);

        UpdateFolderName();
    }

    private void CreateWorld()
    {
        if (_createClicked)
        {
            return;
        }

        _createClicked = true;
        long worldSeed = new JavaRandom().NextLong();
        string seedInput = _textboxSeed.Text;
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

        MC.playerController = new PlayerControllerSP(MC);
        MC.startWorld(_folderName, _textboxWorldName.Text, worldSeed);
    }

    private void UpdateFolderName()
    {
        _folderName = _textboxWorldName.Text.Trim();
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

        _folderName = GenerateUnusedFolderName(MC.getSaveLoader(), _folderName);
    }

    public static string GenerateUnusedFolderName(IWorldStorageSource worldStorage, string baseFolderName)
    {
        while (worldStorage.GetProperties(baseFolderName) != null)
        {
            baseFolderName += "-";
        }

        return baseFolderName;
    }

    public override void OnGuiClosed()
    {
        Keyboard.enableRepeatEvents(false);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        TranslationStorage translations = TranslationStorage.Instance;

        int centerX = Width / 2;
        int centerY = Height / 4;

        DrawDefaultBackground();
        Gui.DrawCenteredString(FontRenderer, translations.TranslateKey("selectWorld.create"), centerX, centerY - 60 + 20, 0xFFFFFF);
        Gui.DrawString(FontRenderer, translations.TranslateKey("selectWorld.enterName"), centerX - 100, centerY - 10, 0xA0A0A0);
        Gui.DrawString(FontRenderer, $"{translations.TranslateKey("selectWorld.resultFolder")} {_folderName}", centerX - 100, centerY + 24, 0xA0A0A0);
        Gui.DrawString(FontRenderer, translations.TranslateKey("selectWorld.enterSeed"), centerX - 100, centerY + 56 - 12, 0xA0A0A0);
        Gui.DrawString(FontRenderer, translations.TranslateKey("selectWorld.seedInfo"), centerX - 100, centerY + 56 + 24, 0xA0A0A0);
    }
}
