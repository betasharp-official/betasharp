using BetaSharp.Client.Guis.Controls;
using BetaSharp.Client.Guis.Layout;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Util.Maths;
using com.sun.javadoc;
using java.io;
using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Guis;

public class GuiMainMenu : Screen
{
    private static readonly JavaRandom s_rand = new();
    private string _splashText = "missingno";

    public GuiMainMenu()
    {
        try
        {
            List<string> splashLines = AssetManager.Instance.getAsset("title/splashes.txt")
                .getTextContent().Split('\n').ToList();
            DateTime now = DateTime.Now;
            _splashText = now switch
            {
                { Month: 11, Day: 9 } => "Happy birthday, ez!",
                { Month: 6, Day: 1 } => "Happy birthday, Notch!",
                { Month: 12, Day: 24 } => "Merry X-mas!",
                { Month: 1, Day: 1 } => "Happy new year!",
                _ => splashLines[s_rand.NextInt(splashLines.Count)],
            };
        }
        catch (Exception)
        {
        }
        TranslationStorage translator = TranslationStorage.Instance;
        int buttonTop = EffectiveHeight / 4;
        int buttonLeft = EffectiveWidth / 2 - 100;

        Control container = new(buttonLeft, buttonTop, 200, 152);
        Button singleplayerButton = new(0, 48, translator.TranslateKey("menu.singleplayer"));
        Button multiplayerButton = new(0, 72, translator.TranslateKey("menu.multiplayer"));
        Button texturePacksButton = new(0, 96, translator.TranslateKey("menu.mods"));
        Button optionsButton = new(0, 132, 98, translator.TranslateKey("menu.options"));
        Button quitButton = new(102, 132, 98, translator.TranslateKey("menu.quit"));

        singleplayerButton.Clicked += (_, _) => MC.OpenScreen(new GuiSelectWorld(this));
        multiplayerButton.Clicked += (_, _) => MC.OpenScreen(new GuiMultiplayer(this));
        texturePacksButton.Clicked += (_, _) => MC.OpenScreen(new GuiTexturePacks(this));
        optionsButton.Clicked += (_, _) => MC.OpenScreen(new GuiOptions(this, MC.options));
        quitButton.Clicked += (_, _) => MC.shutdown();

        container.AddChildren(singleplayerButton, multiplayerButton, texturePacksButton, optionsButton, quitButton);
        AddChild(container);

        Grid test = new Grid(0, 0, 150, 100)
        {
            Anchor = Anchors.Left | Anchors.Right,
            Background = 0xFFFF0000,
        };
        Control blueBox = new(20, 0, 50, 50)
        {
            Background = 0xFF0000FF,
            Margin = new(0, 0, 0, 0),
            Text = "blue",
        };
        Control greenBox = new(0, 0, 50, 50)
        {
            Background = 0xFF00FF00,
            Dock = Dock.Fill,
            Text = "green",
        };
        Control magentaBox = new(0, 0, 50, 50)
        {
            Background = 0xFFFF00FF,
            Dock = Dock.Fill,
            Text = "magenta",
        };
        test.AddColumn(GridUnit.Pixel, 60);
        test.AddColumn(GridUnit.Auto, 1);
        test.AddColumn(GridUnit.Star, 1);
        test.AddRow(GridUnit.Star, 1);
        Grid.SetColumn(blueBox, 1);
        Grid.SetRow(blueBox, 0);
        Grid.SetColumn(greenBox, 0);
        Grid.SetRow(greenBox, 0);
        Grid.SetColumn(magentaBox, 2);
        Grid.SetRow(magentaBox, 0);
        test.AddChild(greenBox);
        test.AddChild(magentaBox);
        test.AddChild(blueBox);
        AddChild(test);

        Control marginHaver = new(0, 0, 50, 50)
        {
            Background = 0xFFFF8000,
            Anchor = Anchors.Left,
            VerticalCenteringBehavior = CenteringBehavior.Middle,
            Margin = new(10, 0, 0, 0)
        };
        AddChild(marginHaver);

        if (MC.session == null || MC.session.sessionId == "-")
        {
            multiplayerButton.Enabled = false;
        }
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();
        Tessellator tess = Tessellator.instance;
        short logoWidth = 274;
        int logoX = EffectiveWidth / 2 - logoWidth / 2;
        byte logoY = 30;
        MC.textureManager.BindTexture(MC.textureManager.GetTextureId("/title/mclogo.png"));
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        DrawTextureRegion(logoX + 0, logoY + 0, 0, 0, 155, 44);
        DrawTextureRegion(logoX + 155, logoY + 0, 0, 45, 155, 44);
        tess.setColorOpaque_I(0xFFFFFF);
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate(EffectiveWidth / 2 + 90, 70.0F, 0.0F);
        GLManager.GL.Rotate(-20.0F, 0.0F, 0.0F, 1.0F);
        float splashScale = 1.8F - MathHelper.Abs(MathHelper.Sin(java.lang.System.currentTimeMillis() % 1000L /
            1000.0F * (float)Math.PI * 2.0F) * 0.1F);
        splashScale = splashScale * 100.0F / (FontRenderer.GetStringWidth(_splashText) + 32);
        GLManager.GL.Scale(splashScale, splashScale, splashScale);
        Gui.DrawCenteredString(FontRenderer, _splashText, 0, -8, 0xFFFF00);
        GLManager.GL.PopMatrix();
        Gui.DrawString(FontRenderer, "Minecraft Beta 1.7.3", 2, 2, 0x505050);
        string copyrightText = "Copyright Mojang Studios. Not an official Minecraft product.";
        Gui.DrawString(FontRenderer, copyrightText, EffectiveWidth - FontRenderer.GetStringWidth(copyrightText) - 2, EffectiveHeight - 20, 0xFFFFFF);
        string disclaimerText = "Not approved by or associated with Mojang Studios or Microsoft.";
        Gui.DrawString(FontRenderer, disclaimerText, EffectiveWidth - FontRenderer.GetStringWidth(disclaimerText) - 2, EffectiveHeight - 10, 0xFFFFFF);
    }
}
