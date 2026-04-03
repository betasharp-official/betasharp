using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Client.UI.Screens.Menu.World;

public class RenameWorldScreen(
    UIContext context,
    WorldScreen parent,
    string worldFolderName,
    IWorldStorageSource saveLoader) : UIScreen(context)
{
    private TextField _txfName = null!;
    private readonly string _worldFolderName = worldFolderName;

    protected override void Init()
    {
        Root.AddChild(new Background());
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.Center;
        Root.Style.SetPadding(20);

        TranslationStorage translations = TranslationStorage.Instance;

        Label title = new() { Text = translations.TranslateKey("selectWorld.renameTitle"), TextColor = Color.White };
        title.Style.MarginBottom = 10;
        Root.AddChild(title);

        Label lName = new() { Text = translations.TranslateKey("selectWorld.enterName"), TextColor = Color.GrayA0 };
        lName.Style.MarginBottom = 4;
        Root.AddChild(lName);

        IWorldStorageSource worldStorage = saveLoader;
        WorldProperties? worldProperties = worldStorage.GetProperties(_worldFolderName);
        string currentWorldName = worldProperties?.LevelName ?? string.Empty;

        _txfName = new TextField { Text = currentWorldName };
        _txfName.Style.MarginBottom = 20;
        Root.AddChild(_txfName);

        Panel buttonPanel = new();
        buttonPanel.Style.FlexDirection = FlexDirection.Row;

        Button btnRename = CreateButton();
        btnRename.Text = translations.TranslateKey("selectWorld.renameButton");
        btnRename.Style.Width = 100;
        btnRename.Style.SetMargin(2);
        btnRename.OnClick += (e) =>
        {
            if (_txfName.Text.Trim().Length > 0)
            {
                worldStorage.Rename(_worldFolderName, _txfName.Text.Trim());
                Context.Navigator.Navigate(parent);
            }
        };
        buttonPanel.AddChild(btnRename);

        Button btnCancel = CreateButton();
        btnCancel.Text = translations.TranslateKey("gui.cancel");
        btnCancel.Style.Width = 100;
        btnCancel.Style.SetMargin(2);
        btnCancel.OnClick += (e) => Context.Navigator.Navigate(parent);
        buttonPanel.AddChild(btnCancel);

        Root.AddChild(buttonPanel);
    }
}
