using System.Diagnostics;
using BetaSharp.Client.Rendering;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Guis;

public class GuiTexturePacks : Screen
{
    private readonly ILogger<GuiTexturePacks> _logger = Log.Instance.For<GuiTexturePacks>();

    protected Screen _parentScreen;
    private int _refreshTimer = -1;
    private string _texturePackFolder;
    private GuiTexturePackList _guiTexturePackList;

    public GuiTexturePacks(Screen parent)
    {
        _parentScreen = parent;
        TranslationStorage translations = TranslationStorage.Instance;
        Button openFolderButton = new Button(Width / 2 - 154, Height - 48, 150, 20, translations.TranslateKey("texturePack.openFolder"));
        Button doneButton = new Button(Width / 2 + 4, Height - 48, 150, 20, translations.TranslateKey("gui.done"));
        MC.texturePackList.updateAvaliableTexturePacks();
        _texturePackFolder = new java.io.File(Minecraft.getMinecraftDir(), "texturepacks").getAbsolutePath();
        _guiTexturePackList = new GuiTexturePackList(this);

        openFolderButton.Clicked += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "file://" + _texturePackFolder,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to open URL: {Message}", ex.Message);
            }
        };
        doneButton.Clicked += (_, _) =>
        {
            MC.textureManager.Reload();
            MC.OpenScreen(_parentScreen);
        };

        AddChildren(_guiTexturePackList, openFolderButton, doneButton);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        if (_refreshTimer <= 0)
        {
            MC.texturePackList.updateAvaliableTexturePacks();
            _refreshTimer += 20;
        }

        TranslationStorage translations = TranslationStorage.Instance;
        Gui.DrawCenteredString(FontRenderer, translations.TranslateKey("texturePack.title"), Width / 2, 16, 0xFFFFFF);
        Gui.DrawCenteredString(FontRenderer, translations.TranslateKey("texturePack.folderInfo"), Width / 2 - 77, Height - 26, 0x808080);
    }

    protected override void OnTick()
    {
        --_refreshTimer;
    }
}
