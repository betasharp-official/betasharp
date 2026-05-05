using System.Numerics;
using BetaSharp.Client.Entities;
using Hexa.NET.ImGui;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Diagnostics.Windows;

internal sealed class ConsoleWindow(DebugWindowContext ctx) : DebugWindow
{
    private string _input = string.Empty;
    private bool _autoScroll = true;
    private bool _scrollToBottom;
    private int _prevEntryCount;
    private bool _refocusInput;

    private static readonly Dictionary<LogLevel, Vector4> s_levelColors = new()
    {
        [LogLevel.Trace] = new Vector4(0.5f, 0.5f, 0.5f, 1f),
        [LogLevel.Debug] = new Vector4(0.7f, 0.7f, 0.7f, 1f),
        [LogLevel.Information] = new Vector4(1f, 1f, 1f, 1f),
        [LogLevel.Warning] = new Vector4(1f, 0.8f, 0f, 1f),
        [LogLevel.Error] = new Vector4(1f, 0.35f, 0.35f, 1f),
        [LogLevel.Critical] = new Vector4(1f, 0f, 0.5f, 1f),
    };

    private static readonly Dictionary<LogLevel, string> s_levelTags = new()
    {
        [LogLevel.Trace] = "TRC",
        [LogLevel.Debug] = "DBG",
        [LogLevel.Information] = "INF",
        [LogLevel.Warning] = "WRN",
        [LogLevel.Error] = "ERR",
        [LogLevel.Critical] = "CRT",
    };

    public override string Title => "Console";
    public override DebugDock DefaultDock => DebugDock.Bottom;

    protected override void OnDraw()
    {
        ImGui.Checkbox("Auto-scroll", ref _autoScroll);
        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Log.Instance.ClearLog();

        ImGui.Separator();

        float inputHeight = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y;
        ImGui.BeginChild("##log_scroll", new Vector2(0f, -inputHeight), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

        LogEntry[] entries = Log.Instance.GetRecentEntries();

        if (entries.Length != _prevEntryCount)
        {
            _prevEntryCount = entries.Length;
            _scrollToBottom = true;
        }

        foreach (LogEntry entry in entries)
        {
            Vector4 color = s_levelColors.TryGetValue(entry.Level, out Vector4 c) ? c : Vector4.One;
            string tag = s_levelTags.TryGetValue(entry.Level, out string? t) ? t : "???";

            ImGui.TextColored(color, $"[{entry.Timestamp:HH:mm:ss}] [{tag}] {entry.Category}: {entry.Message}");

            if (entry.Exception is not null)
                ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), entry.Exception.ToString());
        }

        if (_autoScroll && _scrollToBottom)
        {
            ImGui.SetScrollHereY(1f);
            _scrollToBottom = false;
        }

        ImGui.EndChild();

        ImGui.Separator();

        ClientPlayerEntity? player = ctx.Player;
        if (player is null)
            ImGui.BeginDisabled();

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("Send").X - ImGui.GetStyle().ItemSpacing.X * 2 - ImGui.GetStyle().FramePadding.X * 4);
        if (_refocusInput)
        {
            ImGui.SetKeyboardFocusHere();
            _refocusInput = false;
        }

        bool submitted = ImGui.InputText("##console_input", ref _input, 256, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGui.SameLine();
        bool sendClicked = ImGui.Button("Send");

        if (player is null)
            ImGui.EndDisabled();

        if ((submitted || sendClicked) && player is not null && !string.IsNullOrWhiteSpace(_input))
        {
            player.SendChatMessage(_input.Trim());
            _input = string.Empty;
            _scrollToBottom = true;
            _refocusInput = true;
        }
    }
}
