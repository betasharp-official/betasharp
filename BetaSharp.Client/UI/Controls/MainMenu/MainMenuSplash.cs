using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Util.Maths;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.UI.Controls.MainMenu;

public class MainMenuSplash : UIElement
{
    private static readonly ILogger<MainMenuSplash> s_logger = Log.Instance.For<MainMenuSplash>();
    private static readonly JavaRandom s_rand = new();

    private string _splashText = "missingno";

    public override bool DoTextMeasuring => true;

    public MainMenuSplash()
    {
        LoadSplashText();

        Style.Position = Layout.Flexbox.PositionType.Absolute;
    }

    private void LoadSplashText()
    {
        try
        {
            List<string> splashLines = [];
            string splashesText = AssetManager.Instance.getAsset("title/splashes.txt").GetTextContent();
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

            // Special days
            DateTime now = DateTime.Now;
            if (now.Month == 11 && now.Day == 9) _splashText = "Happy birthday, ez!";
            else if (now.Month == 6 && now.Day == 1) _splashText = "Happy birthday, Notch!";
            else if (now.Month == 12 && now.Day == 24) _splashText = "Merry X-mas!";
            else if (now.Month == 1 && now.Day == 1) _splashText = "Happy new year!";
        }
        catch (Exception ex)
        {
            s_logger.LogError(ex, "Error loading splash text");
        }
    }

    public override void Measure(MeasureContext context)
    {
        ComputedWidth = context.MeasureString(_splashText);
        ComputedHeight = 8;
    }

    public override void Render(UIRenderer renderer)
    {
        float splashScale = 1.8F - MathHelper.Abs(MathHelper.Sin(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1000L / 1000.0F * (float)Math.PI * 2.0F) * 0.1F);
        splashScale = splashScale * 100.0F / (ComputedWidth + 32);

        renderer.DrawCenteredText(_splashText, 0, -8, Color.Yellow, rotation: -20.0f, scale: splashScale);

        base.Render(renderer);
    }
}
