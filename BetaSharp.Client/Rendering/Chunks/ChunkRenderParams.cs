using BetaSharp.Util.Maths;
using Silk.NET.Maths;

namespace BetaSharp.Client.Rendering.Chunks;

public struct ChunkRenderParams
{
    public ICuller Camera;
    public Vector3D<double> ViewPos;
    public int RenderDistance;
    public long Ticks;
    public float PartialTicks;
    public float DeltaTime;
    public bool EnvironmentAnimation;
    public bool ChunkFade;
    public bool RenderOccluded;
}
