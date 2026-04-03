using System.Numerics;
using BetaSharp.Client.UI;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Screens.InGame;
using Hexa.NET.ImGui;
using Silk.NET.Maths;

namespace BetaSharp.Client.Diagnostics.Windows;

internal sealed class UIInspectorWindow(DebugWindowContext ctx) : DebugWindow
{
    public override string Title => "UI Inspector";
    public override DebugDock DefaultDock => DebugDock.Right;

    private UIElement? _hoveredElement;
    private UIElement? _selectedElement;
    private UIScreen? _lastScreen;

    protected override void OnDraw()
    {
        _hoveredElement = null;

        UIScreen? screen = ctx.CurrentScreen;

        if (screen != _lastScreen)
        {
            _selectedElement = null;
            _lastScreen = screen;
        }

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

        if (_selectedElement != null)
        {
            ImGui.Spacing();
            ImGui.SeparatorText("Selected");
            if (ImGui.SmallButton("Clear selection"))
            {
                _selectedElement = null;
            }
            else
            {
                DrawProperties(_selectedElement);
            }
        }

        DrawOverlays();
    }

    private void DrawElementNode(UIElement element, int depth)
    {
        ImGuiTreeNodeFlags nodeFlags = ImGuiTreeNodeFlags.SpanAvailWidth;
        if (depth == 0)
            nodeFlags |= ImGuiTreeNodeFlags.DefaultOpen;

        bool dim = !element.Visible || !element.Enabled;
        bool isSelected = element == _selectedElement;

        int colorPushCount = 0;
        if (isSelected)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.35f, 0.75f, 1f, 1f));
            colorPushCount++;
        }
        else if (dim)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1f));
            colorPushCount++;
        }

        bool open = ImGui.TreeNodeEx("##n", nodeFlags);

        if (ImGui.IsItemHovered())
        {
            _hoveredElement = element;
        }
        if (ImGui.IsItemClicked())
        {
            _selectedElement = isSelected ? null : element;
        }

        ImGui.SameLine();
        ImGui.Text(BuildLabel(element));

        if (ImGui.IsItemHovered())
        {
            _hoveredElement = element;
        }

        if (colorPushCount > 0)
        {
            ImGui.PopStyleColor(colorPushCount);
        }

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

    private unsafe void DrawOverlays()
    {
        int scaleFactor = GetScaleFactor();
        Vector2 vpOffset = ctx.DebugViewportScreenPos;
        ImDrawList* drawList = ImGui.GetForegroundDrawList();

        if (_hoveredElement != null && _hoveredElement != _selectedElement)
        {
            DrawElementHighlight(drawList, _hoveredElement, scaleFactor, vpOffset, isSelected: false);
        }

        if (_selectedElement != null)
        {
            DrawElementHighlight(drawList, _selectedElement, scaleFactor, vpOffset, isSelected: true);
        }
    }

    private static unsafe void DrawElementHighlight(ImDrawList* drawList, UIElement el, int scaleFactor, System.Numerics.Vector2 vpOffset, bool isSelected)
    {
        float x = el.ScreenX * scaleFactor + vpOffset.X;
        float y = el.ScreenY * scaleFactor + vpOffset.Y;
        float w = el.ComputedWidth * scaleFactor;
        float h = el.ComputedHeight * scaleFactor;

        if (w <= 0 || h <= 0) return;

        var min = new Vector2(x, y);
        var max = new Vector2(x + w, y + h);

        uint fillColor = isSelected
            ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 0.60f, 1.0f, 0.30f))
            : ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 0.60f, 1.0f, 0.14f));

        uint borderColor = isSelected
            ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.20f, 0.55f, 1.0f, 1.0f))
            : ImGui.ColorConvertFloat4ToU32(new Vector4(0.20f, 0.55f, 1.0f, 0.65f));

        float borderThickness = isSelected ? 2f : 1f;

        drawList->AddRectFilled(min, max, fillColor);
        drawList->AddRect(min, max, borderColor, 0f, 0, borderThickness);

        string typeName = el.GetType().Name;
        string sizeStr = $"  {el.ComputedWidth:F0}×{el.ComputedHeight:F0}";

        Vector2 typeSize = ImGui.CalcTextSize(typeName);
        Vector2 sizeTextSize = ImGui.CalcTextSize(sizeStr);
        float padX = 5f;
        float padY = 2f;
        float chipW = typeSize.X + sizeTextSize.X + padX * 2;
        float chipH = typeSize.Y + padY * 2;

        float chipX = x;
        float chipY = y - chipH - 1f;
        if (chipY < 0) chipY = y + 1f;

        var chipMin = new Vector2(chipX, chipY);
        var chipMax = new Vector2(chipX + chipW, chipY + chipH);

        uint chipBg = ImGui.ColorConvertFloat4ToU32(new Vector4(0.08f, 0.08f, 0.08f, 0.90f));
        uint typeColor = isSelected
            ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.35f, 0.75f, 1.0f, 1.0f))
            : ImGui.ColorConvertFloat4ToU32(new Vector4(0.55f, 0.80f, 1.0f, 1.0f));
        uint dimColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.70f, 0.70f, 0.70f, 1.0f));

        drawList->AddRectFilled(chipMin, chipMax, chipBg, 3f);
        drawList->AddText(new Vector2(chipX + padX, chipY + padY), typeColor, typeName);
        drawList->AddText(new Vector2(chipX + padX + typeSize.X, chipY + padY), dimColor, sizeStr);
    }

    private int GetScaleFactor()
    {
        UIContext uiCtx = ctx.UIContext;
        Vector2D<int> inputSize = uiCtx.InputDisplaySize;
        var res = new ScaledResolution(uiCtx.Options, inputSize.X, inputSize.Y);
        return res.ScaleFactor;
    }

    private static void DrawProperties(UIElement el)
    {
        foreach (string prop in el.GetInspectorProperties())
        {
            ImGui.Text(prop);
        }
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
