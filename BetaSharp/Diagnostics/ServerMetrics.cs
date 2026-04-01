namespace BetaSharp.Diagnostics;

/// <summary>
/// Server-side diagnostic metrics. Bootstrapped by <see cref="Registries.DefaultRegistries.Initialize"/>.
/// Pushed on the server thread; read on the render thread for ImGui display.
/// When connected to a remote server these metrics are never updated and become stale.
/// </summary>
public static class ServerMetrics
{
    public static readonly MetricHandle<float> Tps = MetricRegistry.Register<float>("server:tps");
    public static readonly MetricHandle<float> Mspt = MetricRegistry.Register<float>("server:mspt");
    public static readonly MetricHandle<int> EntityCount = MetricRegistry.Register<int>("server:entity_count");
    public static readonly MetricHandle<int> PlayerCount = MetricRegistry.Register<int>("server:player_count");

    static ServerMetrics() { }
}
