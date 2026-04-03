using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Controls.ListItems;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Worlds;

namespace BetaSharp.Client.UI.Screens.Menu.World;

public class SelectWorldTypeScreen(
    UIContext context,
    CreateWorldScreen parent,
    WorldType currentType) : UIScreen(context)
{
    private ScrollView _scrollView = null!;
    private readonly List<WorldType> _types = [.. WorldType.WorldTypes.Where(t => t != null && t.CanBeCreated)];
    private int _selectedIndex = -1;
    private readonly List<SelectWorldTypeListItem> _listItems = [];

    protected override void Init()
    {
        Root.AddChild(new Background());
        Root.Style.AlignItems = Align.Center;
        Root.Style.SetPadding(20);

        Label title = new() { Text = "Select World Type", TextColor = Color.White };
        title.Style.MarginBottom = 10;
        Root.AddChild(title);

        _scrollView = new ScrollView();
        _scrollView.Style.Width = 300;
        _scrollView.Style.FlexGrow = 1;
        _scrollView.Style.MarginBottom = 10;
        _scrollView.Style.BackgroundColor = Color.BackgroundBlackAlpha;
        Root.AddChild(_scrollView);

        PopulateTypeList();

        Panel buttonPanel = new();
        buttonPanel.Style.FlexDirection = FlexDirection.Row;

        Button btnDone = CreateButton();
        btnDone.Text = "Done";
        btnDone.Style.Width = 100;
        btnDone.Style.SetMargin(2);
        btnDone.OnClick += (e) =>
        {
            if (_selectedIndex >= 0)
            {
                parent.SetWorldType(_types[_selectedIndex]);
                Context.Navigator.Navigate(parent);
            }
        };
        buttonPanel.AddChild(btnDone);

        Button btnCancel = CreateButton();
        btnCancel.Text = "Cancel";
        btnCancel.Style.Width = 100;
        btnCancel.Style.SetMargin(2);
        btnCancel.OnClick += (e) => Context.Navigator.Navigate(parent);
        buttonPanel.AddChild(btnCancel);

        Root.AddChild(buttonPanel);

        _selectedIndex = _types.IndexOf(currentType);
        if (_selectedIndex >= 0) SelectItem(_selectedIndex);
    }

    private void PopulateTypeList()
    {
        _listItems.Clear();
        foreach (WorldType type in _types)
        {
            int index = _listItems.Count;
            var item = new SelectWorldTypeListItem(type);
            item.OnClick += (e) => SelectItem(index);
            _scrollView.AddContent(item);
            _listItems.Add(item);
        }
    }

    private void SelectItem(int index)
    {
        _selectedIndex = index;
        foreach (SelectWorldTypeListItem item in _listItems) item.IsSelected = false;
        if (index >= 0 && index < _listItems.Count) _listItems[index].IsSelected = true;
    }
}
