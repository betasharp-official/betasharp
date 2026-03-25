using BetaSharp.Client.Rendering.Core;

namespace BetaSharp.Client.Guis;

public class GuiListServer : GuiList
{
    private readonly GuiMultiplayer _parent;

    public GuiListServer(GuiMultiplayer parent)
        : base(parent.MC, parent.EffectiveWidth, parent.EffectiveHeight, 32, parent.EffectiveHeight - 64, 36)
    {
        _parent = parent;
    }

    public override int GetSize()
    {
        return _parent.GetServerList().Count;
    }

    protected override void ElementClicked(int index, bool doubleClick)
    {
        _parent.SelectServer(index);
        bool isValidIndex = index >= 0 && index < GetSize();
        if (doubleClick && isValidIndex)
        {
            _parent.ConnectToServer(index);
        }
    }

    protected override bool IsSelected(int index)
    {
        return index == _parent.GetSelectedServerIndex();
    }

    protected override int GetContentHeight()
    {
        return GetSize() * 36;
    }

    protected override void DrawBackground()
    {
        _parent.DrawDefaultBackground();
    }

    protected override void DrawSlot(int index, int x, int y, int height, Tessellator tess)
    {
        ServerData server = _parent.GetServerList()[index];
        Gui.DrawString(_parent.FontRenderer, server.Name, x + 2, y + 1, 0xFFFFFF);
        Gui.DrawString(_parent.FontRenderer, server.Ip, x + 2, y + 12, 0x808080);
        Gui.DrawString(_parent.FontRenderer, server.PopulationInfo ?? "Unknown player count", x + 2, y + 12 + 11, 0x808080);
    }
}
