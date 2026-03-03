namespace BetaSharp.Client.Guis;

public class GuiConnectFailed : GuiScreen
{

    private readonly string _errorMessage;
    private readonly string _errorDetail;
    private const int _buttonToMenu = 0;

    public GuiConnectFailed(string messageKey, string detailKey, params object[]? formatArgs)
    {
        TranslationStorage translations = TranslationStorage.Instance;
        _errorMessage = translations.TranslateKey(messageKey);
        if (formatArgs != null)
        {
            _errorDetail = translations.TranslateKeyFormat(detailKey, formatArgs);
        }
        else
        {
            _errorDetail = translations.TranslateKey(detailKey);
        }

    }

    public override void UpdateScreen()
    {
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
    }

    public override void InitGui()
    {
        Game.stopInternalServer();
        TranslationStorage translations = TranslationStorage.Instance;
        _controlList.Clear();
        _controlList.Add(new GuiButton(_buttonToMenu, Width / 2 - 100, Height / 4 + 120 + 12, translations.TranslateKey("gui.toMenu")));
    }

    protected override void ActionPerformed(GuiButton btt)
    {
        switch (btt.Id)
        {
            case _buttonToMenu:
                Game.displayGuiScreen(new GuiMainMenu());
                break;
        }

    }

    public override void Render(int mouseX, int mouseY, float parcialTick)
    {
        DrawDefaultBackground();
        DrawCenteredString(FontRenderer, _errorMessage, Width / 2, Height / 2 - 50, Color.White);
        DrawCenteredString(FontRenderer, _errorDetail, Width / 2, Height / 2 - 10, Color.White);
        base.Render(mouseX, mouseY, parcialTick);
    }
}
