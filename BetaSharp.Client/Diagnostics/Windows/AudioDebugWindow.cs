using BetaSharp.Client.Sound;
using Hexa.NET.ImGui;

namespace BetaSharp.Client.Diagnostics.Windows;

internal sealed class AudioDebugWindow(DebugWindowContext ctx) : DebugWindow
{
    public override string Title => "Audio";
    public override DebugDock DefaultDock => DebugDock.Right;

    protected override void OnDraw()
    {
        SoundManager sm = ctx.SoundManager;

        if (ImGui.CollapsingHeader("Channels", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawChannelsSection(sm);
        }

        if (ImGui.CollapsingHeader("Action Sounds", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawActionSoundsSection(sm);
        }

        if (ImGui.CollapsingHeader("Streaming", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawStreamingSection(sm);
        }

        if (ImGui.CollapsingHeader("Music", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawMusicSection(sm);
        }
    }

    private static void DrawChannelsSection(SoundManager sm)
    {
        ImGui.Text($"Active: {sm.ActiveChannelCount} / 32");
    }

    private static void DrawActionSoundsSection(SoundManager sm)
    {
        ImGui.Text($"Unique names: {sm.LoadedSoundNameCount}");
        ImGui.Text($"Files loaded: {sm.LoadedSoundFileCount}");
    }

    private static void DrawStreamingSection(SoundManager sm)
    {
        ImGui.Text($"Files loaded: {sm.LoadedStreamingFileCount}");

        string status = sm.IsStreamingPlaying ? "Playing" : "Idle";
        ImGui.Text($"Status:       {status}");
        ImGui.Text($"Track:        {sm.CurrentStreamingName ?? "none"}");
    }

    private static void DrawMusicSection(SoundManager sm)
    {
        string activeCategory = sm.ActiveCategory != null ? sm.ActiveCategory.ToString() : "none";
        string musicStatus = sm.IsMusicPlaying ? "Playing" : "Idle";

        ImGui.Text($"Status:   {musicStatus}");
        ImGui.Text($"Track:    {sm.CurrentMusicName ?? "none"}");
        ImGui.Text($"Category: {activeCategory}");

        ImGui.Spacing();

        foreach ((ResourceLocation name, MusicCategory cat) in sm.MusicCategories)
        {
            ImGui.Separator();
            ImGui.Text($"[{name}]");
            ImGui.Text($"  Tracks:      {cat.Pool.LoadedSoundCount}");
            ImGui.Text($"  Delay range: {cat.MinDelayTicks} – {cat.MaxDelayTicks} ticks");
            ImGui.Text($"  Next in:     {cat.TicksBeforeNext} ticks");
        }
    }
}
