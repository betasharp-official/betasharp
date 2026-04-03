using BetaSharp.Diagnostics;
using Hexa.NET.ImGui;

namespace BetaSharp.Client.Diagnostics.Windows;

internal sealed class RenderInfoWindow : DebugWindow
{
    public override string Title => "Render Info";
    public override DebugDock DefaultDock => DebugDock.Right;

    protected override void OnDraw()
    {
        if (MetricRegistry.IsStale(RenderMetrics.ChunksTotal))
        {
            ImGui.TextDisabled("No world loaded.");
            return;
        }

        if (ImGui.CollapsingHeader("Chunks", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawChunkSection();
        }

        if (ImGui.CollapsingHeader("Entities", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawEntitiesSection();
        }

        if (ImGui.CollapsingHeader("Textures", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawTextureSection();
        }
    }

    private static void DrawChunkSection()
    {
        ImGui.Text($"Total:    {MetricRegistry.Get(RenderMetrics.ChunksTotal)}");
        ImGui.Text($"Frustum:  {MetricRegistry.Get(RenderMetrics.ChunksFrustum)}");
        ImGui.Text($"Occluded: {MetricRegistry.Get(RenderMetrics.ChunksOccluded)}");
        ImGui.Text($"Rendered: {MetricRegistry.Get(RenderMetrics.ChunksRendered)}");

        ImGui.Spacing();
        ImGui.Text($"VBO Allocated:      {MetricRegistry.Get(RenderMetrics.VboAllocatedMb):F2} MB");
        ImGui.Text($"Mesh Version Alloc: {MetricRegistry.Get(RenderMetrics.MeshVersionAllocated)}");
        ImGui.Text($"Mesh Version Free:  {MetricRegistry.Get(RenderMetrics.MeshVersionReleased)}");
    }

    private static void DrawEntitiesSection()
    {
        ImGui.Text($"Rendered:  {MetricRegistry.Get(RenderMetrics.EntitiesRendered)}");
        ImGui.Text($"Hidden:    {MetricRegistry.Get(RenderMetrics.EntitiesHidden)}");
        ImGui.Text($"Total:     {MetricRegistry.Get(RenderMetrics.EntitiesTotal)}");
        ImGui.Text($"Particles: {MetricRegistry.Get(RenderMetrics.ParticlesActive)}");
    }

    private static void DrawTextureSection()
    {
        ImGui.Text($"Binds:   {MetricRegistry.Get(RenderMetrics.TextureBindsLastFrame)} (Avg: {MetricRegistry.Get(RenderMetrics.TextureAvgBinds):F1}/f)");
        ImGui.Text($"Active:  {MetricRegistry.Get(RenderMetrics.TextureActive)}");
    }
}
