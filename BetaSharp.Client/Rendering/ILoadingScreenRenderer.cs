using BetaSharp;

namespace BetaSharp.Client.Rendering;

/// <summary>
/// Backend-owned loading/progress display contract used during world and resource loading.
/// </summary>
public interface ILoadingScreenRenderer : LoadingDisplay
{
    void BeginLoading(string message);
}
