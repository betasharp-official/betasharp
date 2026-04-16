using Silk.NET.OpenGL;

namespace BetaSharp.Client.Rendering.Core;

internal class DeletionQueue
{
    private class Frame
    {
        public nint Fence { get; set; }
        public List<Action> PendingDeletions = [];
    }

    private const int FramesInFlight = 2;

    //TODO: Integrate this into the ImGui debug display
    public int SyncStalls { get; private set; }

    private readonly Frame[] _frames = new Frame[FramesInFlight];
    private readonly GL _gl;
    private int _currentFrame;

    public DeletionQueue(GL gl)
    {
        _gl = gl;
        _currentFrame = 0;

        for (int i = 0; i < _frames.Length; i++)
        {
            _frames[i] = new();
        }
    }

    public void BeginFrame()
    {
        Frame oldest = _frames[_currentFrame % FramesInFlight];

        if (oldest.Fence != 0)
        {
            unchecked
            {
                GLEnum result = _gl.ClientWaitSync(oldest.Fence, SyncObjectMask.Bit, (ulong)GLEnum.TimeoutIgnored);

                if (result != GLEnum.AlreadySignaled)
                {
                    SyncStalls++;
                }
            }

            _gl.DeleteSync(oldest.Fence);
            oldest.Fence = 0;
        }

        // It is now safe to free anything queues on frame "oldest"
        foreach (Action deletion in oldest.PendingDeletions)
        {
            deletion();
        }

        oldest.PendingDeletions.Clear();
    }

    public void EndFrame()
    {
        Frame current = _frames[_currentFrame % FramesInFlight];
        current.Fence = _gl.FenceSync(GLEnum.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
        _currentFrame++;
    }

    public void AddDeletion(Action deletion)
    {
        _frames[_currentFrame % FramesInFlight].PendingDeletions.Add(deletion);
    }
}
