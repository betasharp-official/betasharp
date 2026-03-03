using BetaSharp.Client.Input;
using BetaSharp.Client.Options;
using BetaSharp.NBT;

namespace BetaSharp.Client.Guis;

//TODO: Update multiplayer menu to use proper translations

public class GuiMultiplayer : GuiScreen
{
    private GuiSlotServer _serverListSelector = null!;
    private readonly List<ServerData> _serverList = [];
    private int _selectedServerIndex = -1;
    private GuiButton _btnEdit = null!;
    private GuiButton _btnSelect = null!;
    private GuiButton _btnDelete = null!;
    private bool _deletingServer = false;
    private bool _addingServer = false;
    private bool _editingServer = false;
    private bool _directConnect = false;
    private ServerData _tempServer = null!;

    private readonly GuiScreen _parentScreen;
    private readonly GameOptions _options;

    public GuiMultiplayer(GuiScreen parentScreen, GameOptions options)
    {
        _parentScreen = parentScreen;
        _options = options;
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

    public override void InitGui()
    {
        LoadServerList();
        Keyboard.enableRepeatEvents(true);
        _controlList.Clear();
        _serverListSelector = new GuiSlotServer(this);

        _controlList.Add(_btnEdit = new GuiButton(7, Width / 2 - 154, Height - 28, 70, 20, "Edit"));
        _controlList.Add(_btnDelete = new GuiButton(2, Width / 2 - 74, Height - 28, 70, 20, "Delete"));
        _controlList.Add(_btnSelect = new GuiButton(1, Width / 2 - 154, Height - 52, 100, 20, "Join Server"));
        _controlList.Add(new GuiButton(4, Width / 2 - 50, Height - 52, 100, 20, "Direct Connect"));
        _controlList.Add(new GuiButton(3, Width / 2 + 4 + 50, Height - 52, 100, 20, "Add server"));
        _controlList.Add(new GuiButton(8, Width / 2 + 4, Height - 28, 70, 20, "Refresh"));
        _controlList.Add(new GuiButton(0, Width / 2 + 4 + 76, Height - 28, 75, 20, "Cancel"));

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        bool hasSelection = _selectedServerIndex >= 0 && _selectedServerIndex < _serverList.Count;
        _btnSelect.Enabled = hasSelection;
        _btnEdit.Enabled = hasSelection;
        _btnDelete.Enabled = hasSelection;
    }

    private void LoadServerList()
    {
        try
        {
            string path = System.IO.Path.Combine(BetaSharp.getBetaSharpDir().getAbsolutePath(), "servers.dat");
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

            string path = System.IO.Path.Combine(BetaSharp.getBetaSharpDir().getAbsolutePath(), "servers.dat");
            using FileStream stream = File.OpenWrite(path);
            NbtIo.WriteCompressed(tag, stream);
        }
        catch { }
    }

    public override void OnGuiClosed()
    {
        Keyboard.enableRepeatEvents(false);
    }

    public override void UpdateScreen()
    {
        base.UpdateScreen();
        UpdateButtons();
    }

    protected override void ActionPerformed(GuiButton button)
    {
        if (!button.Enabled) return;

        if (button.Id == 2) // Delete
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
                Game.displayGuiScreen(yesNo);
            }
        }
        else if (button.Id == 1) // Select/Connect
        {
            ConnectToServer(_selectedServerIndex);
        }
        else if (button.Id == 4) // Direct Connect
        {
            _directConnect = true;
            _tempServer = new ServerData("BetaSharp Server", "");
            Game.displayGuiScreen(new GuiDirectConnect(this, _tempServer, _options));
        }
        else if (button.Id == 3) // Add
        {
            _addingServer = true;
            _tempServer = new ServerData("BetaSharp Server", "");
            Game.displayGuiScreen(new GuiScreenAddServer(this, _tempServer));
        }
        else if (button.Id == 7) // Edit
        {
            _editingServer = true;
            ServerData original = _serverList[_selectedServerIndex];
            _tempServer = new ServerData(original.Name, original.Ip);
            Game.displayGuiScreen(new GuiScreenAddServer(this, _tempServer));
        }
        else if (button.Id == 0) // Cancel
        {
            Game.displayGuiScreen(_parentScreen);
        }
        else if (button.Id == 8) // Refresh
        {
            LoadServerList();
        }
        else
        {
            _serverListSelector.ActionPerformed(button);
        }
    }

    public override void DeleteWorld(bool result, int id)
    {
        if (_deletingServer)
        {
            _deletingServer = false;
            if (result)
            {
                _serverList.RemoveAt(id);
                SaveServerList();
                _selectedServerIndex = -1;
            }
            Game.displayGuiScreen(this);
        }
    }

    public void ConfirmClicked(bool result, int id)
    {
        if (_directConnect)
        {
            _directConnect = false;

            if (result)
            {
                JoinServer(_tempServer);
            }
            else
            {
                Game.displayGuiScreen(this);
            }
        }
        else if (_addingServer)
        {
            _addingServer = false;
            if (result)
            {
                _serverList.Add(_tempServer);
                SaveServerList();
            }
            Game.displayGuiScreen(this);
        }
        else if (_editingServer)
        {
            _editingServer = false;
            if (result)
            {
                ServerData server = _serverList[_selectedServerIndex];
                server.Name = _tempServer.Name;
                server.Ip = _tempServer.Ip;
                SaveServerList();
            }
            Game.displayGuiScreen(this);
        }
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        if (eventKey == 28) // Enter
        {
            if (_btnSelect != null && _btnSelect.Enabled) ActionPerformed(_btnSelect);
        }
    }

    protected override void MouseClicked(int x, int y, int button)
    {
        base.MouseClicked(x, y, button);
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawDefaultBackground();
        _serverListSelector.DrawScreen(mouseX, mouseY, partialTicks);
        DrawCenteredString(FontRenderer, "Play Multiplayer", Width / 2, 20, Color.White);
        base.Render(mouseX, mouseY, partialTicks);
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

        Game.displayGuiScreen(new GuiConnecting(Game, host, portNum));
    }
}
