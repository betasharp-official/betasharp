using BetaSharp.Client.Guis.Debug;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Client.Guis;

public class GuiNewDebugSlot : GuiSlot
{
    readonly GuiNewDebug _parentNewGui;


    public GuiNewDebugSlot(GuiNewDebug parent) : base(parent.Game, parent.Width, parent.Height, 32, parent.Height - 64, 36)
    {
        _parentNewGui = parent;
    }

    public override int GetSize()
    {
        return DebugComponents.Components.Count;
    }

    protected override void ElementClicked(int slotIndex, bool doubleClick)
    {
        bool canSelect = slotIndex >= 0 && slotIndex < GetSize();
        if (!canSelect) return;

        _parentNewGui.selectedType = DebugComponents.Components[slotIndex];

        if (doubleClick)
        {
            _parentNewGui.Finish();
        }

        _parentNewGui.buttonAdd.Enabled = true;
    }

    protected override bool IsSelected(int slotIndex)
    {
        bool canSelect = slotIndex >= 0 && slotIndex < GetSize();
        if (!canSelect) return false;

        return _parentNewGui.selectedType == DebugComponents.Components[slotIndex];
    }

    protected override int GetContentHeight()
    {
        return GetSize() * 36;
    }

    protected override void DrawBackground()
    {
        _parentNewGui.DrawDefaultBackground();
    }

    protected override void DrawSlot(int slotIndex, int x, int y, int slotHeight, Tessellator tessellator)
    {
        if (!(slotIndex >= 0 && slotIndex < GetSize())) {
            Gui.DrawString(_parentNewGui.FontRenderer, "Invalid slot", x + 2, y + 1, Color.White);
        }

        Type type = DebugComponents.Components[slotIndex];
        Gui.DrawString(_parentNewGui.FontRenderer, DebugComponents.GetName(type), x + 2, y + 1, Color.White);
        _parentNewGui.FontRenderer.DrawStringWrapped(DebugComponents.GetDescription(type), x + 2, y + 12, 200, Color.Gray80);
    }
}
