using System.ComponentModel;
using BetaSharp.Client.Diagnostics;
using BetaSharp.NBT;

namespace BetaSharp.Client.Debug.Components;

[DisplayName("System")]
[Description("Shows info about your system.")]
public class DebugSystem : DebugComponent
{
    public DebugSystem() { }

    [DisplayName("Show CPU")]
    [Description("Whether to show CPU information.")]
    public bool ShowCPU { get; set; } = true;

    [DisplayName("Show GPU")]
    [Description("Whether to show GPU information.")]
    public bool ShowGPU { get; set;} = true;

    [DisplayName("Show OpenGL")]
    [Description("Whether to show OpenGL information.")]
    public bool ShowOpenGL { get; set;} = true;

    [DisplayName("Show GLSL")]
    [Description("Whether to show GLSL information.")]
    public bool ShowGLSL { get; set;} = true;

    [DisplayName("Show Driver")]
    [Description("Whether to show Driver information.")]
    public bool ShowDriver { get; set;} = true;

    [DisplayName("Show OS")]
    [Description("Whether to show OS information.")]
    public bool ShowOS { get; set;} = true;


    public override IEnumerable<DebugRowData> GetRows(DebugContext ctx)
    {
        DebugSystemSnapshot systemSnapshot = ctx.Game.GetDebugSystemSnapshot();
        if (ShowCPU) yield return new DebugRowData($"CPU: {FormatCpuInfo(systemSnapshot)}");
        if (ShowGPU) yield return new DebugRowData($"GPU: {systemSnapshot.GpuName} (VRAM: {systemSnapshot.GpuVram})");
        if (ShowOpenGL) yield return new DebugRowData($"OpenGL: {systemSnapshot.OpenGlVersion}");
        if (ShowGLSL) yield return new DebugRowData($"GLSL: {systemSnapshot.GlslVersion}");
        if (ShowDriver) yield return new DebugRowData($"Driver: {systemSnapshot.DriverVersion}");
        if (ShowOS) yield return new DebugRowData($"OS: {systemSnapshot.OsDescription}");
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
            Right = Right,

            ShowCPU = ShowCPU,
            ShowGPU = ShowGPU,
            ShowOpenGL = ShowOpenGL,
            ShowGLSL = ShowGLSL,
            ShowDriver = ShowDriver,
            ShowOS = ShowOS
        };
    }

    public override void writeNBT(NBTTagCompound nbt)
    {
        nbt.SetBoolean("ShowCPU", ShowCPU);
        nbt.SetBoolean("ShowGPU", ShowGPU);
        nbt.SetBoolean("ShowOpenGL", ShowOpenGL);
        nbt.SetBoolean("ShowGLSL", ShowGLSL);
        nbt.SetBoolean("ShowDriver", ShowDriver);
        nbt.SetBoolean("ShowOS", ShowOS);
    }

    public override void readNBT(NBTTagCompound nbt)
    {
        ShowCPU = nbt.GetBoolean("ShowCPU");
        ShowGPU = nbt.GetBoolean("ShowGPU");
        ShowOpenGL = nbt.GetBoolean("ShowOpenGL");
        ShowGLSL = nbt.GetBoolean("ShowGLSL");
        ShowDriver = nbt.GetBoolean("ShowDriver");
        ShowOS = nbt.GetBoolean("ShowOS");
    }
}
