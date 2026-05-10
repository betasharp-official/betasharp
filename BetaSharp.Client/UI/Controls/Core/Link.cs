using System.Diagnostics;
using System.Runtime.InteropServices;
using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.Core;

public class Link : Label
{
    public Color HoverColor { get; set; } = Color.HoverYellow;
    public string URL { get; set; } = "";
    private bool _isHovered;

    public Link()
    {
        OnMouseEnter += (e) => _isHovered = true;
        OnMouseLeave += (e) => _isHovered = false;
        OnClick += (e) =>
        {
            if (!string.IsNullOrEmpty(URL))
            {
                OpenBrowser(URL);
            }
        };
    }

    public override void Render(UIRenderer renderer)
    {
        Color color = _isHovered ? HoverColor : TextColor;

        if (Centered)
        {
            renderer.DrawCenteredText(Text, ComputedWidth / 2, ComputedHeight / 2 - 4 * Scale, color, 0, Scale, HasShadow);
        }
        else
        {
            renderer.DrawText(Text, 0, 0, color, Scale, HasShadow);
        }

        foreach (UIElement child in Children)
        {
            renderer.PushTranslate(child.ComputedX, child.ComputedY);
            child.Render(renderer);
            renderer.PopTranslate();
        }
    }

    private static void OpenBrowser(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows requires UseShellExecute to be true for URLs
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux uses xdg-open
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS uses the open command
            Process.Start("open", url);
        }
    }
}
