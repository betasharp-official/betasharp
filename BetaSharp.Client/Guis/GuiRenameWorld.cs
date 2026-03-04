using BetaSharp.Client.Input;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Client.Guis;

public class GuiRenameWorld : Screen
{
    private readonly Screen parentScreen;
    private TextField nameInputField;
    private readonly string worldFolderName;

    public GuiRenameWorld(Screen parentScreen, string worldFolderName)
    {
        this.parentScreen = parentScreen;
        this.worldFolderName = worldFolderName;

        TranslationStorage translations = TranslationStorage.Instance;
        Keyboard.enableRepeatEvents(true);

        Button renameButton = new(Width / 2 - 100, Height / 4 + 96 + 12, translations.TranslateKey("selectWorld.renameButton"));
        Button cancelButton = new(Width / 2 - 100, Height / 4 + 120 + 12, translations.TranslateKey("gui.cancel"));
        renameButton.Clicked += (_, _) =>
        {
            IWorldStorageSource worldStorage = MC.getSaveLoader();
            worldStorage.Rename(worldFolderName, nameInputField.Text.Trim());
            MC.OpenScreen(parentScreen);
        };
        cancelButton.Clicked += (_, _) => MC.OpenScreen(parentScreen);

        IWorldStorageSource worldStorage = MC.getSaveLoader();
        WorldProperties? worldProperties = worldStorage.GetProperties(worldFolderName);
        string currentWorldName = worldProperties?.LevelName ?? string.Empty;

        nameInputField = new(Width / 2 - 100, 60, FontRenderer, currentWorldName)
        {
            Focused = true,
            MaxLength = 32,
        };

        AddChildren(renameButton, cancelButton, nameInputField);
    }

    public override void OnGuiClosed()
    {
        Keyboard.enableRepeatEvents(false);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        TranslationStorage translations = TranslationStorage.Instance;
        DrawDefaultBackground();
        Gui.DrawCenteredString(FontRenderer, translations.TranslateKey("selectWorld.renameTitle"), Width / 2, Height / 4 - 60 + 20, 0xFFFFFF);
        Gui.DrawString(FontRenderer, translations.TranslateKey("selectWorld.enterName"), Width / 2 - 100, 47, 0xA0A0A0);
    }
}
