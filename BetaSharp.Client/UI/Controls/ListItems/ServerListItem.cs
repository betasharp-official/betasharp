using BetaSharp.Client.Guis;
using BetaSharp.Client.Network;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.ListItems;

public class ServerListItem(ServerData data) : ListItem<ServerData>(data)
{
    public override void Render(UIRenderer renderer)
    {
        base.Render(renderer);

        renderer.DrawText(Value.Name, 5, 5, Color.White);

        string secondary = string.IsNullOrEmpty(Value.Motd) ? Value.Ip : Value.Motd;
        renderer.DrawText(secondary, 5, 17, Color.GrayA0);
    }
}
