using BetaSharp.Client.Network;
using BetaSharp.Client.Threading;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Guis;

public class GuiConnecting : GuiScreen
{
    private readonly ILogger<GuiConnecting> _logger = Log.Instance.For<GuiConnecting>();

    private ClientNetworkHandler _clientHandler;
    private bool _cancelled = false;
    private const int _buttonCancel = 0;

    public override bool PausesGame=> false;

    public GuiConnecting(Minecraft mc, string host, int port)
    {
        _logger.LogInformation($"Connecting to {host}, {port}");
        mc.changeWorld(null);
        new ThreadConnectToServer(this, mc, host, port).Start();
    }

    public GuiConnecting(Minecraft mc, ClientNetworkHandler clientHandler)
    {
        _clientHandler = clientHandler;
        mc.changeWorld(null);
    }

    public override void UpdateScreen()
    {
        if (_clientHandler != null)
        {
            _clientHandler.tick();
        }

    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
    }

    public override void InitGui()
    {
        TranslationStorage translations = TranslationStorage.Instance;
        _controlList.Clear();
        _controlList.Add(new GuiButton(_buttonCancel, Width / 2 - 100, Height / 4 + 120 + 12, translations.TranslateKey("gui.cancel")));
    }

    protected override void ActionPerformed(GuiButton button)
    {
        switch (button.Id)
        {
            case _buttonCancel:
                _cancelled = true;
                _clientHandler?.Disconnect();

                mc.displayGuiScreen(new GuiMainMenu());
                break;
        }

    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawDefaultBackground();
        TranslationStorage translations = TranslationStorage.Instance;
        if (_clientHandler == null)
        {
            DrawCenteredString(FontRenderer, translations.TranslateKey("connect.connecting"), Width / 2, Height / 2 - 50, Color.White);
            DrawCenteredString(FontRenderer, "", Width / 2, Height / 2 - 10, Color.White);
        }
        else
        {
            DrawCenteredString(FontRenderer, translations.TranslateKey("connect.authorizing"), Width / 2, Height / 2 - 50, Color.White);
            DrawCenteredString(FontRenderer, _clientHandler.field_1209_a, Width / 2, Height / 2 - 10, Color.White);
        }

        base.Render(mouseX, mouseY, partialTicks);
    }

    public static ClientNetworkHandler setNetClientHandler(GuiConnecting guiConnecting, ClientNetworkHandler handler)
    {
        return guiConnecting._clientHandler = handler;
    }

    public static bool isCancelled(GuiConnecting guiConnecting)
    {
        return guiConnecting._cancelled;
    }

    public static ClientNetworkHandler getNetClientHandler(GuiConnecting guiConnecting)
    {
        return guiConnecting._clientHandler;
    }
}
