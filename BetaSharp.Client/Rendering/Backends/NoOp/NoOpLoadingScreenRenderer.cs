namespace BetaSharp.Client.Rendering.Backends;

internal sealed class NoOpLoadingScreenRenderer(BetaSharp game) : ILoadingScreenRenderer
{
    private bool _ignoreShutdownCheck;
    private long _lastUpdateMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public void BeginLoading(string message)
    {
        _ignoreShutdownCheck = false;
    }

    public void BeginLoadingPersistent(string message)
    {
        _ignoreShutdownCheck = true;
    }

    public void SetStage(string message)
    {
        if (!game.Running && !_ignoreShutdownCheck)
        {
            throw new BetaSharpShutdownException();
        }
    }

    public void SetProgress(int progress)
    {
        if (!game.Running && !_ignoreShutdownCheck)
        {
            throw new BetaSharpShutdownException();
        }

        if (!game.Running)
        {
            return;
        }

        long currentTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (currentTimeMs - _lastUpdateMs < 20L)
        {
            return;
        }

        _lastUpdateMs = currentTimeMs;
        game.UpdateWindow(true);
        Thread.Yield();
    }
}
