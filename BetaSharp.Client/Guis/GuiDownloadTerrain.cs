using BetaSharp.Client.Network;
using BetaSharp.Network.Packets.Play;

namespace BetaSharp.Client.Guis;

public class GuiDownloadTerrain : GuiScreen
{

    private readonly ClientNetworkHandler _networkHandler;
    private int _tickCounter = 0;

    public override bool PausesGame => false;

    public GuiDownloadTerrain(ClientNetworkHandler networkHandler)
    {
        this._networkHandler = networkHandler;
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
    }

    public override void InitGui()
    {
        _controlList.Clear();
    }

    public override void UpdateScreen()
    {
        ++_tickCounter;
        if (_tickCounter % 20 == 0)
        {
            _networkHandler.addToSendQueue(KeepAlivePacket.Get());
        }

        if (_networkHandler != null)
        {
            _networkHandler.tick();
        }

    }

    protected override void ActionPerformed(GuiButton button)
    {
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawBackground(0);
        TranslationStorage translations = TranslationStorage.Instance;
        DrawCenteredString(FontRenderer, translations.TranslateKey("multiplayer.downloadingTerrain"), Width / 2, Height / 2 - 50, Color.White);
        base.Render(mouseX, mouseY, partialTicks);
    }
}
