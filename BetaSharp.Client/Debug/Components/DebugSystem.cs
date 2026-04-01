using System.ComponentModel;
using BetaSharp.Client.Diagnostics;

namespace BetaSharp.Client.Debug.Components;

[DisplayName("System")]
[Description("Shows info about your system.")]
public class DebugSystem : DebugComponent
{
    public DebugSystem() { }

    public override IEnumerable<DebugRowData> GetRows(DebugContext ctx)
    {
        DebugSystemSnapshot systemSnapshot = ctx.Game.DebugSystemSnapshot;
        yield return new DebugRowData($"CPU: {FormatCpuInfo(systemSnapshot)}");
        yield return new DebugRowData($"GPU: {systemSnapshot.GpuName} (VRAM: {systemSnapshot.GpuVram})");
        yield return new DebugRowData($"OpenGL: {systemSnapshot.OpenGlVersion}");
        yield return new DebugRowData($"GLSL: {systemSnapshot.GlslVersion}");
        yield return new DebugRowData($"Driver: {systemSnapshot.DriverVersion}");
        yield return new DebugRowData($"OS: {systemSnapshot.OsDescription}");
        yield return new DebugRowData($".NET: {systemSnapshot.DotNetRuntime}");
    }

    private static string FormatCpuInfo(DebugSystemSnapshot systemSnapshot)
    {
        string coreLabel = systemSnapshot.CpuCoreCount == 1 ? "core" : "cores";
        if (systemSnapshot.CpuName == DebugTelemetry.UnknownValue)
        {
            return $"{systemSnapshot.CpuCoreCount} {coreLabel}";
        }

        return $"{systemSnapshot.CpuName} ({systemSnapshot.CpuCoreCount} {coreLabel})";
    }

    public override DebugComponent Duplicate()
    {
        return new DebugSystem()
        {
            Right = Right
        };
    }
}
