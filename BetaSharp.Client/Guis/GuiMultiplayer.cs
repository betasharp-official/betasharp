using BetaSharp.Client.Guis.Controls;
using BetaSharp.Client.Guis.Layout;
using BetaSharp.Client.Input;
using BetaSharp.NBT;

namespace BetaSharp.Client.Guis;

//TODO: Update multiplayer menu to use proper translations

public class GuiMultiplayer : Screen
{
    private GuiListServer _serverListSelector;
    private readonly List<ServerData> _serverList = [];
    private int _selectedServerIndex = -1;
    private readonly Button _btnEdit;
    private readonly Button _btnSelect;
    private readonly Button _btnDelete;
    private bool _deletingServer;
    private bool _addingServer;
    private bool _editingServer;
    private bool _directConnect;
    private ServerData _tempServer = null!;

    private readonly Screen _parentScreen;

    public GuiMultiplayer(Screen parentScreen)
    {
        _parentScreen = parentScreen;

        LoadServerList();
        Keyboard.enableRepeatEvents(true);
        Children.Clear();
        _serverListSelector = new GuiListServer(this)
        {
            Anchor = Anchors.Top | Anchors.Left | Anchors.Right | Anchors.Bottom,
        };

        Control container = new(EffectiveWidth / 2 - 154, EffectiveHeight - 52, 308, 44)
        {
            Anchor = Anchors.Bottom,
        };
        _btnSelect =                 new(0,   0,  100, "Join Server") { Enabled = false };
        Button directConnectButton = new(104, 0,  100, "Direct Connect");
        Button addServerButton =     new(208, 0,  100, "Add server");
        _btnEdit =                   new(0,   24, 74,  "Edit") { Enabled = false };
        _btnDelete =                 new(77,  24, 74,  "Delete") { Enabled = false };
        Button refreshButton =       new(156, 24, 74,  "Refresh");
        Button cancelButton =        new(234, 24, 74,  "Cancel");

        _btnEdit.Clicked += (_, _) =>
        {
            _editingServer = true;
            ServerData original = _serverList[_selectedServerIndex];
            _tempServer = new ServerData(original.Name, original.Ip);
            MC.OpenScreen(new GuiScreenAddServer(this, _tempServer));
        };
        _btnDelete.Clicked += (_, _) =>
        {
            string serverName = _serverList[_selectedServerIndex].Name;
            if (serverName != null)
            {
                _deletingServer = true;
                string q = "Are you sure you want to remove this server?";
                string w = "'" + serverName + "' " + "will be lost forever! (A long time!)";
                string b = "Delete";
                string c = "Cancel";
                GuiYesNo yesNo = new(this, q, w, b, c, _selectedServerIndex);
                MC.OpenScreen(yesNo);
            }
        };
        _btnSelect.Clicked += (_, _) => ConnectToServer(_selectedServerIndex);
        directConnectButton.Clicked += (_, _) =>
        {
            _directConnect = true;
            _tempServer = new ServerData("Minecraft Server", "");
            MC.OpenScreen(new GuiDirectConnect(this, _tempServer));
        };
        addServerButton.Clicked += (_, _) =>
        {
            _addingServer = true;
            _tempServer = new ServerData("Minecraft Server", "");
            MC.OpenScreen(new GuiScreenAddServer(this, _tempServer));
        };
        refreshButton.Clicked += (_, _) => LoadServerList();
        cancelButton.Clicked += (_, _) => MC.OpenScreen(_parentScreen);

        container.AddChildren(_btnEdit, _btnDelete, _btnSelect, directConnectButton, addServerButton, refreshButton, cancelButton);
        AddChildren(_serverListSelector, container);
    }

    public List<ServerData> GetServerList()
    {
        return _serverList;
    }

    public void ConnectToServer(int index)
    {
        JoinServer(_serverList[index]);
    }

    public void SelectServer(int index)
    {
        _selectedServerIndex = index;
    }

    public int GetSelectedServerIndex()
    {
        return _selectedServerIndex;
    }

    private void LoadServerList()
    {
        try
        {
            string path = System.IO.Path.Combine(Minecraft.getMinecraftDir().getAbsolutePath(), "servers.dat");
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

            string path = System.IO.Path.Combine(Minecraft.getMinecraftDir().getAbsolutePath(), "servers.dat");
            using FileStream stream = File.OpenWrite(path);
            NbtIo.WriteCompressed(tag, stream);
        }
        catch { }
    }

    public override void OnGuiClosed()
    {
        Keyboard.enableRepeatEvents(false);
    }

    public override void DeleteWorld(bool confirmed, int index)
    {
        if (_deletingServer)
        {
            _deletingServer = false;
            if (confirmed)
            {
                _serverList.RemoveAt(index);
                SaveServerList();
                _selectedServerIndex = -1;
            }
            MC.OpenScreen(this);
        }
    }

    public void ConfirmClicked(bool confirmed, int id)
    {
        if (_directConnect)
        {
            _directConnect = false;

            if (confirmed)
            {
                JoinServer(_tempServer);
            }
            else
            {
                MC.OpenScreen(this);
            }
        }
        else if (_addingServer)
        {
            _addingServer = false;
            if (confirmed)
            {
                _serverList.Add(_tempServer);
                SaveServerList();
            }
            MC.OpenScreen(this);
        }
        else if (_editingServer)
        {
            _editingServer = false;
            if (confirmed)
            {
                ServerData server = _serverList[_selectedServerIndex];
                server.Name = _tempServer.Name;
                server.Ip = _tempServer.Ip;
                SaveServerList();
            }
            MC.OpenScreen(this);
        }
    }

    protected override void OnKeyInput(KeyboardEventArgs e)
    {
        if (e.Key == Keyboard.KEY_RETURN)
        {
            ConnectToServer(_selectedServerIndex);
        }
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();
        Gui.DrawCenteredString(FontRenderer, "Play Multiplayer", EffectiveWidth / 2, 20, 0xFFFFFF);
    }

    private void JoinServer(ServerData server)
    {
        JoinServer(server.Ip);
    }

    private void JoinServer(string ip)
    {
        string[] parts = ip.Split(':');
        if (ip.StartsWith('['))
        {
            int end = ip.IndexOf(']');
            if (end > 0)
            {
                string ipV6 = ip.Substring(1, end);
                string port = ip.Substring(end + 1).Trim();
                if (port.StartsWith(':') && port.Length > 0)
                {
                    parts = [ipV6, port.Substring(1)];
                }
                else
                {
                    parts = [ipV6];
                }
            }
        }

        string host = parts[0];
        int portNum = 25565;
        if (parts.Length > 1)
        {
            _ = int.TryParse(parts[1], out portNum);
        }

        MC.OpenScreen(new GuiConnecting(MC, host, portNum));
    }
}
