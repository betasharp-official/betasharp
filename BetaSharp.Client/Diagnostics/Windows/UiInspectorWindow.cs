using System.Numerics;
using BetaSharp.Client.UI;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Screens.InGame;
using Hexa.NET.ImGui;

namespace BetaSharp.Client.Diagnostics.Windows;

internal sealed class UIInspectorWindow(DebugWindowContext ctx) : DebugWindow
{
    public override string Title => "UI Inspector";
    public override DebugDock DefaultDock => DebugDock.Right;

    protected override void OnDraw()
    {
        UIScreen? screen = ctx.CurrentScreen;

        ImGui.SeparatorText("Current Screen");

        if (screen == null)
        {
            ImGui.TextDisabled("none");
        }
        else
        {
            ImGui.TextColored(new Vector4(0.4f, 0.8f, 1f, 1f), screen.GetType().Name);
            ImGui.Spacing();

            ImGui.PushID("screen");
            DrawElementNode(screen.Root, depth: 0);
            ImGui.PopID();
        }

        ImGui.Spacing();
        ImGui.SeparatorText("HUD");

        HUD hud = ctx.HUD;
        if (hud?.Root is { } hudRoot)
        {
            ImGui.PushID("hud");
            DrawElementNode(hudRoot, depth: 0);
            ImGui.PopID();
        }
        else
        {
            ImGui.TextDisabled("no HUD");
        }
    }

    private static void DrawElementNode(UIElement element, int depth)
    {
        ImGuiTreeNodeFlags nodeFlags = ImGuiTreeNodeFlags.SpanAvailWidth;
        if (depth == 0)
        {
            nodeFlags |= ImGuiTreeNodeFlags.DefaultOpen;
        }

        bool dim = !element.Visible || !element.Enabled;

        if (dim) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1f));

        bool open = ImGui.TreeNodeEx("##n", nodeFlags);
        ImGui.SameLine();
        ImGui.Text(BuildLabel(element));

        if (dim) ImGui.PopStyleColor();

        if (open)
        {
            bool propsOpen = ImGui.TreeNodeEx("##p", ImGuiTreeNodeFlags.SpanAvailWidth);
            ImGui.SameLine();
            ImGui.TextDisabled("Properties");

            if (propsOpen)
            {
                DrawProperties(element);
                ImGui.TreePop();
            }

            for (int i = 0; i < element.Children.Count; i++)
            {
                ImGui.PushID(i);
                DrawElementNode(element.Children[i], depth + 1);
                ImGui.PopID();
            }

            if (element is ScrollView sv)
            {
                ImGui.PushID("content");
                DrawElementNode(sv.ContentContainer, depth + 1);
                ImGui.PopID();
            }

            ImGui.TreePop();
        }
    }

    private static void DrawProperties(UIElement el)
    {
        ImGui.Text($"Type:     {el.GetType().FullName}");
        ImGui.Text($"Screen:   ({el.ScreenX:F1}, {el.ScreenY:F1})");
        ImGui.Text($"Size:     {el.ComputedWidth:F1} × {el.ComputedHeight:F1}");
        ImGui.Text($"Local:    ({el.ComputedX:F1}, {el.ComputedY:F1})");
        ImGui.Text($"Visible:  {el.Visible}   Enabled: {el.Enabled}");
        ImGui.Text($"Focused:  {el.IsFocused}   Hovered: {el.IsHovered}");
        ImGui.Text($"HitTest:  {el.IsHitTestVisible}   Clip: {el.ClipToBounds}");
        ImGui.Text($"Children: {el.Children.Count}");
    }

    private static string BuildLabel(UIElement el)
    {
        string typeName = el.GetType().Name;
        string sizeStr = el.ComputedWidth > 0 || el.ComputedHeight > 0
            ? $"  {el.ComputedWidth:F0}×{el.ComputedHeight:F0}"
            : string.Empty;

        var sb = new System.Text.StringBuilder();
        if (!el.Visible) sb.Append(" [hidden]");
        if (!el.Enabled) sb.Append(" [disabled]");
        if (el.IsHovered) sb.Append(" [hovered]");
        if (el.IsFocused) sb.Append(" [focused]");
        if (!el.IsHitTestVisible) sb.Append(" [no-hit]");

        string flags = sb.Length > 0 ? "  " + sb.ToString().TrimStart() : string.Empty;
        return $"{typeName}{sizeStr}{flags}";
    }
}
