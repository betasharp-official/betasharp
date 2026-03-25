using BetaSharp.Client.Guis.Controls;

namespace BetaSharp.Client.Guis;

public class GuiConnectFailed : Screen
{
    private readonly string _errorMessage;
    private readonly string _errorDetail;

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

        MC.stopInternalServer();
        Button titleButton = new(EffectiveWidth / 2 - 100, EffectiveHeight / 4 + 120 + 12, translations.TranslateKey("gui.toMenu"));
        titleButton.Clicked += (_, _) => MC.OpenScreen(new GuiMainMenu());
        AddChild(titleButton);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();
        Gui.DrawCenteredString(FontRenderer, _errorMessage, EffectiveWidth / 2, EffectiveHeight / 2 - 50, 0xFFFFFF);
        Gui.DrawCenteredString(FontRenderer, _errorDetail, EffectiveWidth / 2, EffectiveHeight / 2 - 10, 0xFFFFFF);
    }
}
