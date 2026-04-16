namespace BetaSharp.Client.Rendering.Rhi;

public readonly record struct RenderPassDesc(
    string DebugLabel,
    bool ClearColor,
    bool ClearDepth,
    int Width,
    int Height);
