using BetaSharp.Client.Guis.Controls;
using BetaSharp.Client.Guis.Layout;
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
        Text = translations.TranslateKey("selectWorld.renameTitle");
        DisplayTitle = true;
        Keyboard.enableRepeatEvents(true);

        Control container = new(EffectiveWidth / 2 - 100, EffectiveHeight / 2 - 52, 200, 104)
        {
            VerticalCenteringBehavior = CenteringBehavior.Middle,
        };

        IWorldStorageSource worldStorage = MC.getSaveLoader();
        WorldProperties? worldProperties = worldStorage.GetProperties(worldFolderName);
        string currentWorldName = worldProperties?.LevelName ?? string.Empty;

        Label worldNameLabel = new(0, 0, translations.TranslateKey("selectWorld.enterName"), 0xA0A0A0);
        nameInputField = new(0, 12, FontRenderer, currentWorldName)
        {
            Focused = true,
            MaxLength = 32,
        };
        Button renameButton = new(0, 60, translations.TranslateKey("selectWorld.renameButton"));
        Button cancelButton = new(0, 84, translations.TranslateKey("gui.cancel"));
        renameButton.Clicked += (_, _) =>
        {
            IWorldStorageSource worldStorage = MC.getSaveLoader();
            worldStorage.Rename(worldFolderName, nameInputField.Text.Trim());
            MC.OpenScreen(parentScreen);
        };
        cancelButton.Clicked += (_, _) => MC.OpenScreen(parentScreen);

        container.AddChildren(worldNameLabel, renameButton, cancelButton, nameInputField);
        AddChild(container);
    }

    public override void OnGuiClosed()
    {
        Keyboard.enableRepeatEvents(false);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();
    }
}
