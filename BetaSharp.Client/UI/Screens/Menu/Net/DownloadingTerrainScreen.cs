using BetaSharp.Client.Guis;
using BetaSharp.Client.Network;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Network.Packets.Play;

namespace BetaSharp.Client.UI.Screens.Menu.Net;

public class DownloadingTerrainScreen(ClientNetworkHandler networkHandler) : UIScreen(BetaSharp.Instance)
{
    private readonly ClientNetworkHandler _networkHandler = networkHandler;
    private int _tickCounter = 0;

    public override bool PausesGame => false;

    protected override void Init()
    {
        Root.AddChild(new Background(BackgroundType.Dirt));
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.Center;

        Label label = new()
        {
            Text = TranslationStorage.Instance.TranslateKey("multiplayer.downloadingTerrain"),
            TextColor = Color.White,
            Centered = true
        };
        Root.AddChild(label);
    }

    public override void Update(float partialTicks)
    {
        base.Update(partialTicks);

        ++_tickCounter;
        if (_tickCounter % 20 == 0)
        {
            _networkHandler.addToSendQueue(KeepAlivePacket.Get());
        }

        _networkHandler?.tick();
    }

    public override void KeyTyped(int key, char character)
    {
        // Do nothing to prevent escaping
    }
}
