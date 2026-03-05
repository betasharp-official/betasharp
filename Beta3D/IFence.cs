namespace Beta3D;

public interface IFence : IDisposable
{
    bool AwaitCompletion(long timeoutMs);
}
