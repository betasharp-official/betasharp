namespace BetaSharp.Launcher.Features.Home;

internal sealed class BetaSharpRelease(string name, string date, string url)
{
    public string Name => name;

    public string Date => date;

    public string Url => url;
}
