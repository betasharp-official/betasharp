using BetaSharp.Client.Rendering.Core;
using BetaSharp.Util.Maths;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Guis;

public class GuiMainMenu : GuiScreen
{

    private readonly ILogger<GuiMainMenu> _logger = Log.Instance.For<GuiMainMenu>();
    private const int ButtonOptions = 0;
    private const int ButtonSingleplayer = 1;
    private const int ButtonMultiplayer = 2;
    private const int ButtonTexturePacksAndMods = 3;
    private const int ButtonQuit = 4;

    private static readonly JavaRandom s_rand = new();
    private string _splashText = "missingno";
    private GuiButton _multiplayerButton;

    public GuiMainMenu()
    {
        try
        {
            List<string> splashLines = [];
            string splashesText = AssetManager.Instance.getAsset("title/splashes.txt").getTextContent();
            using (StringReader reader = new(splashesText))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length > 0)
                    {
                        splashLines.Add(line);
                    }
                }
            }

            if (splashLines.Count > 0)
            {
                _splashText = splashLines[s_rand.NextInt(splashLines.Count)];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading splash text");
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
        // Special days
        DateTime now = DateTime.Now;
        if (now.Month == 11 && now.Day == 9) _splashText = "Happy birthday, ez!";
        else if (now.Month == 6 && now.Day == 1) _splashText = "Happy birthday, Notch!";
        else if (now.Month == 12 && now.Day == 24) _splashText = "Merry X-mas!";
        else if (now.Month == 1 && now.Day == 1) _splashText = "Happy new year!";

        TranslationStorage translator = TranslationStorage.Instance;
        int buttonTopY = Height / 4 + 48;

        _controlList.Add(new GuiButton(ButtonSingleplayer, Width / 2 - 100, buttonTopY, translator.TranslateKey("menu.singleplayer")));
        _controlList.Add(_multiplayerButton =
            new GuiButton(ButtonMultiplayer, Width / 2 - 100, buttonTopY + 24, translator.TranslateKey("menu.multiplayer")));
        _controlList.Add(new GuiButton(ButtonTexturePacksAndMods, Width / 2 - 100, buttonTopY + 48, translator.TranslateKey("menu.mods")));

        if (Game.hideQuitButton)
        {
            _controlList.Add(new GuiButton(ButtonOptions, Width / 2 - 100, buttonTopY + 72, translator.TranslateKey("menu.options")));
        }
        else
        {
            _controlList.Add(new GuiButton(ButtonOptions, Width / 2 - 100, buttonTopY + 72 + 12, 98, 20,
                translator.TranslateKey("menu.options")));

            _controlList.Add(new GuiButton(ButtonQuit, Width / 2 + 2, buttonTopY + 72 + 12, 98, 20,
                translator.TranslateKey("menu.quit")));
        }

        if (Game.session == null || Game.session.sessionId == "-")
        {
            _multiplayerButton.Enabled = false;
        }
    }

    protected override void ActionPerformed(GuiButton button)
    {
        switch (button.Id)
        {
            case ButtonOptions:
                Game.displayGuiScreen(new GuiOptions(this, Game.options));
                break;
            case ButtonSingleplayer:
                Game.displayGuiScreen(new GuiSelectWorld(this));
                break;
            case ButtonMultiplayer:
                Game.displayGuiScreen(new GuiMultiplayer(this, Game.options));
                break;
            case ButtonTexturePacksAndMods:
                Game.displayGuiScreen(new GuiTexturePacks(this));
                break;
            case ButtonQuit:
                Game.shutdown();
                break;
        }
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawDefaultBackground();
        Tessellator tess = Tessellator.instance;
        short logoWidth = 274;
        int logoX = Width / 2 - logoWidth / 2;
        byte logoY = 30;
        Game.textureManager.BindTexture(Game.textureManager.GetTextureId("/title/mclogo.png"));
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        DrawTexturedModalRect(logoX + 0, logoY + 0, 0, 0, 155, 44);
        DrawTexturedModalRect(logoX + 155, logoY + 0, 0, 45, 155, 44);
        tess.setColorOpaque_I(0xFFFFFF);
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate(Width / 2 + 90, 70.0F, 0.0F);
        GLManager.GL.Rotate(-20.0F, 0.0F, 0.0F, 1.0F);
        float splashScale = 1.8F - MathHelper.Abs(MathHelper.Sin(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
 % 1000L / 1000.0F * (float)Math.PI * 2.0F) * 0.1F);
        splashScale = splashScale * 100.0F / (FontRenderer.GetStringWidth(_splashText) + 32);
        GLManager.GL.Scale(splashScale, splashScale, splashScale);
        DrawCenteredString(FontRenderer, _splashText, 0, -8, Color.Yellow);
        GLManager.GL.PopMatrix();
        DrawString(FontRenderer, "BetaSharp Beta 1.7.3", 2, 2, Color.Gray50);
        string copyrightText = "Copyright Mojang Studios. Not an official Minecraft product.";
        DrawString(FontRenderer, copyrightText, Width - FontRenderer.GetStringWidth(copyrightText) - 2, Height - 20, Color.White);
        string disclaimerText = "Not approved by or associated with Mojang Studios or Microsoft.";
        DrawString(FontRenderer, disclaimerText, Width - FontRenderer.GetStringWidth(disclaimerText) - 2, Height - 10, Color.White);
        base.Render(mouseX, mouseY, partialTicks);
    }
}
