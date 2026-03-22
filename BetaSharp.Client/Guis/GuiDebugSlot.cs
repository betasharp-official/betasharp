using BetaSharp.Client.Guis.Debug;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Client.Guis;

public class GuiDebugSlot : GuiSlot
{
    readonly GuiDebugEditor _parentDebugGui;


    public GuiDebugSlot(GuiDebugEditor parent) : base(parent.Game, parent.Width, parent.Height, 32, parent.Height - 64, 36)
    {
        _parentDebugGui = parent;
    }

    public override int GetSize()
    {
        return _parentDebugGui.components.Count;
    }

    protected override void ElementClicked(int slotIndex, bool doubleClick)
    {
        bool canSelect = slotIndex >= 0 && slotIndex < GetSize();
        if (!canSelect) return;

        _parentDebugGui.selectedComponent = _parentDebugGui.components[slotIndex];

        if (doubleClick)
        {
            _parentDebugGui.selectedComponent.Right = !_parentDebugGui.selectedComponent.Right;
        }

        _parentDebugGui.buttonChange.Enabled = true;
        _parentDebugGui.buttonDelete.Enabled = true;
    }

    protected override bool IsSelected(int slotIndex)
    {
        bool canSelect = slotIndex >= 0 && slotIndex < GetSize();
        if (!canSelect) return false;

        return _parentDebugGui.selectedComponent == _parentDebugGui.components[slotIndex];
    }

    protected override int GetContentHeight()
    {
        return GetSize() * 36;
    }

    protected override void DrawBackground()
    {
        _parentDebugGui.DrawDefaultBackground();
    }

    protected override void DrawSlot(int slotIndex, int x, int y, int slotHeight, Tessellator tessellator)
    {
        if (!(slotIndex >= 0 && slotIndex < GetSize())) {
            Gui.DrawString(_parentDebugGui.FontRenderer, "Invalid slot", x + 2, y + 1, Color.White);
        }

        DebugComponent comp = _parentDebugGui.components[slotIndex];
        Gui.DrawString(_parentDebugGui.FontRenderer, DebugComponents.GetName(comp.GetType()), x + 2, y + 1, Color.White);
        Gui.DrawString(_parentDebugGui.FontRenderer, DebugComponents.GetDescription(comp.GetType()), x + 2, y + 12, Color.Gray80);
        Gui.DrawString(_parentDebugGui.FontRenderer, comp.Right ? "Right" : "Left", x + 2, y + 22, Color.Gray80);
    }
}
