namespace BetaSharp.Client.Rendering;

/// <summary>
/// Conventional resolver naming for backend selection/fallback.
/// </summary>
public static class RenderBackendResolver
{
    public static RenderBackendResolution Resolve(RendererBackendKind requestedBackend)
    {
        RendererBackendSelection selection = RendererBackendFactory.Resolve(requestedBackend);
        return new RenderBackendResolution(selection.Requested, selection.Effective, selection.FallbackReason);
    }

    public static bool TryParse(string value, out RendererBackendKind backend)
        => RendererBackendFactory.TryParse(value, out backend);
}
