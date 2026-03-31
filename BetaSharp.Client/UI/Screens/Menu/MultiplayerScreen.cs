using BetaSharp.Client.Guis;
using BetaSharp.Client.Network;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Controls.ListItems;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Client.UI.Screens.Menu.Net;
using BetaSharp.NBT;

namespace BetaSharp.Client.UI.Screens.Menu;

public class MultiplayerScreen(UIContext context, ClientNetworkContext networkContext) : UIScreen(context)
{
    private readonly List<ServerData> _serverList = [];
    private ScrollView _scrollView = null!;
    private int _selectedServerIndex = -1;
    private readonly List<ServerListItem> _listItems = [];

    private Button _btnJoin = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;

    protected override void Init()
    {
        Root.AddChild(new Background());
        LoadServerList();

        Root.Style.AlignItems = Align.Center;
        Root.Style.SetPadding(20);

        Label title = new() { Text = "Play Multiplayer", TextColor = Color.White };
        title.Style.MarginBottom = 8;
        Root.AddChild(title);
        AddTitleSpacer();

        _scrollView = new ScrollView();
        _scrollView.Style.Width = 300;
        _scrollView.Style.FlexGrow = 1;
        _scrollView.Style.MaxHeight = 200;
        _scrollView.Style.MarginBottom = 10;
        _scrollView.Style.BackgroundColor = Color.BackgroundBlackAlpha;
        Root.AddChild(_scrollView);

        PopulateServerList();

        Panel buttonContainer = new();
        buttonContainer.Style.FlexDirection = FlexDirection.Column;
        buttonContainer.Style.AlignItems = Align.Center;
        buttonContainer.Style.Width = 320;

        Panel row1 = new();
        row1.Style.FlexDirection = FlexDirection.Row;
        row1.Style.JustifyContent = Justify.Center;
        row1.Style.MarginBottom = 2;

        _btnJoin = CreateButton();
        _btnJoin.Text = "Join Server";
        _btnJoin.Style.Width = 100;
        _btnJoin.Style.SetMargin(2);
        _btnJoin.OnClick += (e) => ConnectSelected();
        row1.AddChild(_btnJoin);

        Button btnDirect = CreateButton();
        btnDirect.Text = "Direct Connect";
        btnDirect.Style.Width = 100;
        btnDirect.Style.SetMargin(2);
        btnDirect.OnClick += (e) => Context.Navigator.Navigate(new DirectConnectScreen(Context, this, new ServerData("BetaSharp Server", ""), networkContext));
        row1.AddChild(btnDirect);

        Button btnAdd = CreateButton();
        btnAdd.Text = "Add Server";
        btnAdd.Style.Width = 100;
        btnAdd.Style.SetMargin(2);
        btnAdd.OnClick += (e) => Context.Navigator.Navigate(new EditServerScreen(Context, this, new ServerData("BetaSharp Server", ""), false));
        row1.AddChild(btnAdd);

        buttonContainer.AddChild(row1);

        Panel row2 = new();
        row2.Style.FlexDirection = FlexDirection.Row;
        row2.Style.JustifyContent = Justify.Center;

        _btnEdit = CreateButton();
        _btnEdit.Text = "Edit";
        _btnEdit.Style.Width = 75;
        _btnEdit.Style.SetMargin(2);
        _btnEdit.OnClick += (e) => EditSelected();
        row2.AddChild(_btnEdit);

        _btnDelete = CreateButton();
        _btnDelete.Text = "Delete";
        _btnDelete.Style.Width = 75;
        _btnDelete.Style.SetMargin(2);
        _btnDelete.OnClick += (e) => DeleteSelected();
        row2.AddChild(_btnDelete);

        Button btnRefresh = CreateButton();
        btnRefresh.Text = "Refresh";
        btnRefresh.Style.Width = 75;
        btnRefresh.Style.SetMargin(2);
        btnRefresh.OnClick += (e) => { LoadServerList(); PopulateServerList(); };
        row2.AddChild(btnRefresh);

        Button btnCancel = CreateButton();
        btnCancel.Text = "Cancel";
        btnCancel.Style.Width = 75;
        btnCancel.Style.SetMargin(2);
        btnCancel.OnClick += (e) => Context.Navigator.Navigate(null);
        row2.AddChild(btnCancel);

        buttonContainer.AddChild(row2);
        Root.AddChild(buttonContainer);

        UpdateButtons();
    }

    private void LoadServerList()
    {
        try
        {
            string path = Path.Combine(BetaSharp.BetaSharpDir, "servers.dat");
            if (!File.Exists(path)) return;

            using FileStream stream = File.OpenRead(path);
            NBTTagCompound tag = NbtIo.ReadCompressed(stream);

            NBTTagList list = tag.GetTagList("servers");
            _serverList.Clear();
            for (int i = 0; i < list.TagCount(); ++i)
            {
                _serverList.Add(ServerData.FromNBT((NBTTagCompound)list.TagAt(i)));
            }
        }
        catch { }
    }

    private void SaveServerList()
    {
        try
        {
            NBTTagList list = new();
            foreach (ServerData server in _serverList)
            {
                list.SetTag(server.ToNBT());
            }
            NBTTagCompound tag = new();
            tag.SetTag("servers", list);

            string path = Path.Combine(BetaSharp.BetaSharpDir, "servers.dat");
            using FileStream stream = File.Create(path);
            NbtIo.WriteCompressed(tag, stream);
        }
        catch { }
    }

    private void PopulateServerList()
    {
        _scrollView.ContentContainer.Children.Clear();
        _listItems.Clear();
        _selectedServerIndex = -1;

        for (int i = 0; i < _serverList.Count; i++)
        {
            int index = i;
            ServerListItem item = new(_serverList[i]);
            item.OnClick += (e) => SelectServer(index);
            _scrollView.AddContent(item);
            _listItems.Add(item);
        }
    }

    private void SelectServer(int index)
    {
        _selectedServerIndex = index;
        foreach (ServerListItem item in _listItems) item.IsSelected = false;
        if (index >= 0 && index < _listItems.Count) _listItems[index].IsSelected = true;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        bool hasSelection = _selectedServerIndex >= 0;
        _btnJoin.Enabled = hasSelection;
        _btnEdit.Enabled = hasSelection;
        _btnDelete.Enabled = hasSelection;
    }

    private void ConnectSelected()
    {
        if (_selectedServerIndex < 0) return;
        ServerData data = _serverList[_selectedServerIndex];
        ConnectToServer(data.Ip);
    }

    private void EditSelected()
    {
        if (_selectedServerIndex < 0) return;
        ServerData original = _serverList[_selectedServerIndex];
        ServerData temp = new(original.Name, original.Ip);
        Context.Navigator.Navigate(new EditServerScreen(Context, this, temp, true));
    }

    public void ConfirmEdit(ServerData data, bool isEditing)
    {
        if (isEditing)
        {
            if (_selectedServerIndex >= 0)
            {
                _serverList[_selectedServerIndex].Name = data.Name;
                _serverList[_selectedServerIndex].Ip = data.Ip;
            }
        }
        else
        {
            _serverList.Add(data);
        }
        SaveServerList();
        PopulateServerList();
        UpdateButtons();
    }

    private void DeleteSelected()
    {
        if (_selectedServerIndex < 0) return;
        ServerData server = _serverList[_selectedServerIndex];
        string q = "Are you sure you want to remove this server?";
        string w = "'" + server.Name + "' " + "will be lost forever! (A long time!)";

        Context.Navigator.Navigate(new ConfirmationScreen(Context, this, q, w, "Delete", "Cancel", (result) =>
        {
            if (result)
            {
                _serverList.RemoveAt(_selectedServerIndex);
                SaveServerList();
                PopulateServerList();
                UpdateButtons();
            }
        }));
    }

    private void ConnectToServer(string ip)
    {
        string[] parts = ip.Split(':');
        string host = parts[0];
        int portNum = 25565;
        if (parts.Length > 1) int.TryParse(parts[1], out portNum);
        Context.Navigator.Navigate(new ConnectingScreen(Context, networkContext, host, portNum));
    }
}
