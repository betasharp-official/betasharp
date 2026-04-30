using BetaSharp.Client.Guis;
using BetaSharp.Client.Modding;
using BetaSharp.Client.Network;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Controls.ListItems;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Client.UI.Screens.Menu.Net;
using BetaSharp.NBT;

namespace BetaSharp.Client.UI.Screens.Menu;

public class ModsScreen(UIContext context, ModManager modMan) : UIScreen(context)
{
    private Mod? _currentMod = null;
    private ModListItem? _currentItem = null;

    private Label _title;
    private Label _author;
    private Label _id;
    private Label _description;

    protected override void Init()
    {
        Root.AddChild(new Background());

        Root.Style.AlignItems = Align.Center;
        Root.Style.SetPadding(20);

        Label title = new() { Text = "Mods", TextColor = Color.White };
        title.Style.MarginBottom = 8;
        Root.AddChild(title);

        Panel middle = new Panel();
        middle.Style.FlexDirection = FlexDirection.Row;
        middle.Style.FlexGrow = 1;
        middle.Style.SetPadding(20);

        ScrollView mods = new ScrollView();
        mods.Style.Width = 150;
        mods.Style.BackgroundColor = Color.BackgroundBlackAlpha;
        mods.Style.MarginRight = 4;

        foreach (Mod mod in modMan.Mods) {
            var item = new ModListItem(mod);
            item.Style.Width = 140;
            item.OnClick += (e) =>
            {
                item.IsSelected = true;
                _currentItem?.IsSelected = false;

                _currentItem = item;
                _currentMod = mod;

                _title.Visible = true;
                _author.Visible = true;
                _id.Visible = true;
                _description.Visible = true;

                _title.Text = mod.Name;
                _author.Text = $"Made by {mod.Author}";
                _id.Text = $"ID: {mod.ID}";
                _description.Text = mod.Description;
            };

            mods.AddContent(item);
        }

        middle.AddChild(mods);

        Panel info = new Panel();
        info.Style.FlexGrow = 1;
        info.Style.BackgroundColor = Color.BlackAlphaC0;
        info.Style.SetPadding(8);

        _title = new Label { Scale = 2.0F, Visible = false };
        _title.Style.MarginBottom = 2;

        _author = new Label { Visible = false, TextColor = Color.Gray90 };
        _id = new Label { Visible = false, TextColor = Color.Gray90 };
        _id.Style.MarginBottom = 8;

        _description = new Label { Visible = false };

        info.AddChild(_title);
        info.AddChild(_author);
        info.AddChild(_id);
        info.AddChild(_description);

        middle.AddChild(info);

        Root.AddChild(middle);


        Button btnDone = CreateButton();
        btnDone.Text = "Done";
        btnDone.Style.Width = 150;
        btnDone.Style.MarginTop = 8;
        btnDone.OnClick += (e) => Context.Navigator.Navigate(null);
        Root.AddChild(btnDone);
    }
}
