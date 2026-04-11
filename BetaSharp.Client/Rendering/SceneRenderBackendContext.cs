using BetaSharp.Client.Rendering.Backends;

namespace BetaSharp.Client.Rendering;

public static class SceneRenderBackendContext
{
    private static ISceneRenderBackend _current = new NoOpSceneRenderBackend();

    public static ISceneRenderBackend Current
    {
        get => _current;
        set => _current = value ?? new NoOpSceneRenderBackend();
    }
}
