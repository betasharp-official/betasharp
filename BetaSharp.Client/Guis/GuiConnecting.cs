using BetaSharp.Client.Guis.Controls;
using BetaSharp.Client.Network;
using BetaSharp.Client.Threading;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Guis;

public class GuiConnecting : Screen
{
    private readonly ILogger<GuiConnecting> _logger = Log.Instance.For<GuiConnecting>();

    public ClientNetworkHandler ClientHandler;
    public bool Cancelled { get; private set; }

    public override bool PausesGame=> false;

    public GuiConnecting(Minecraft mc, string host, int port)
    {
        _logger.LogInformation($"Connecting to {host}, {port}");
        mc.changeWorld(null);
        new ThreadConnectToServer(this, mc, host, port).start();
        AddCancelButton();
    }

    public GuiConnecting(Minecraft mc, ClientNetworkHandler clientHandler)
    {
        ClientHandler = clientHandler;
        mc.changeWorld(null);
        AddCancelButton();
    }

    private void AddCancelButton()
    {
        TranslationStorage translations = TranslationStorage.Instance;
        Control container = new(EffectiveWidth / 2 - 100, EffectiveHeight / 4 + 120 + 12, 200, 20);
        Button cancelButton = new(0, 0, translations.TranslateKey("gui.cancel"));
        cancelButton.Clicked += (_, _) =>
        {
            Cancelled = true;
            ClientHandler?.disconnect();
            MC.OpenScreen(new GuiMainMenu());
        };
        container.AddChild(cancelButton);
        AddChild(container);
    }

    protected override void OnTick()
    {
        if (ClientHandler != null)
        {
            ClientHandler.tick();
        }
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();
        TranslationStorage translations = TranslationStorage.Instance;
        if (ClientHandler == null)
        {
            Gui.DrawCenteredString(FontRenderer, translations.TranslateKey("connect.connecting"), EffectiveWidth / 2, EffectiveHeight / 2 - 50, 0xFFFFFF);
            Gui.DrawCenteredString(FontRenderer, "", EffectiveWidth / 2, EffectiveHeight / 2 - 10, 0xFFFFFF);
        }
        else
        {
            Gui.DrawCenteredString(FontRenderer, translations.TranslateKey("connect.authorizing"), EffectiveWidth / 2, EffectiveHeight / 2 - 50, 0xFFFFFF);
            Gui.DrawCenteredString(FontRenderer, ClientHandler.field_1209_a, EffectiveWidth / 2, EffectiveHeight / 2 - 10, 0xFFFFFF);
        }
    }
}
