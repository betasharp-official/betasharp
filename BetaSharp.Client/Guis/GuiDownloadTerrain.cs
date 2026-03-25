using BetaSharp.Client.Network;
using BetaSharp.Network.Packets.Play;

namespace BetaSharp.Client.Guis;

public class GuiDownloadTerrain : Screen
{
    private readonly ClientNetworkHandler _networkHandler;
    private int _tickCounter = 0;

    public override bool PausesGame => false;

    public GuiDownloadTerrain(ClientNetworkHandler networkHandler)
    {
        _networkHandler = networkHandler;
    }

    protected override void OnTick()
    {
        ++_tickCounter;
        if (_tickCounter % 20 == 0)
        {
            _networkHandler.addToSendQueue(new KeepAlivePacket());
        }

        if (_networkHandler != null)
        {
            _networkHandler.tick();
        }
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawBackground(0);
        TranslationStorage translations = TranslationStorage.Instance;
        Gui.DrawCenteredString(FontRenderer, translations.TranslateKey("multiplayer.downloadingTerrain"), EffectiveWidth / 2, EffectiveHeight / 2 - 50, 0xFFFFFF);
    }
}
