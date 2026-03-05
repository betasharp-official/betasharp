using BetaSharp.Client.Rendering.Core;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Client.Guis;

public class GuiWorldSlot : GuiSlot
{
    readonly GuiSelectWorld _parentWorldGui;


    public GuiWorldSlot(GuiSelectWorld parent) : base(parent.Game, parent.Width, parent.Height, 32, parent.Height - 64, 36)
    {
        _parentWorldGui = parent;
    }

    public override int GetSize()
    {
        return GuiSelectWorld.GetSize(_parentWorldGui).Count;
    }

    protected override void ElementClicked(int slotIndex, bool doubleClick)
    {
        GuiSelectWorld.onElementSelected(_parentWorldGui, slotIndex);
        WorldSaveInfo worldInfo = GuiSelectWorld.GetSize(_parentWorldGui)[slotIndex];
        bool canSelect = GuiSelectWorld.getSelectedWorld(_parentWorldGui) >= 0 && GuiSelectWorld.getSelectedWorld(_parentWorldGui) < GetSize() && !worldInfo.IsUnsupported;
        GuiSelectWorld.getSelectButton(_parentWorldGui).Enabled = canSelect;
        GuiSelectWorld.getRenameButton(_parentWorldGui).Enabled = canSelect;
        GuiSelectWorld.getDeleteButton(_parentWorldGui).Enabled = canSelect;
        if (doubleClick && canSelect)
        {
            _parentWorldGui.selectWorld(slotIndex);
        }

    }

    protected override bool IsSelected(int slotIndex)
    {
        return slotIndex == GuiSelectWorld.getSelectedWorld(_parentWorldGui);
    }

    protected override int GetContentHeight()
    {
        return GuiSelectWorld.GetSize(_parentWorldGui).Count * 36;
    }

    protected override void DrawBackground()
    {
        _parentWorldGui.DrawDefaultBackground();
    }

    protected override void DrawSlot(int slotIndex, int x, int y, int slotHeight, Tessellator tessellator)
    {
        WorldSaveInfo worldInfo = GuiSelectWorld.GetSize(_parentWorldGui)[slotIndex];
        string displayName = worldInfo.DisplayName;
        
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = GuiSelectWorld.getWorldNameHeader(_parentWorldGui) + " " + (slotIndex + 1);
        }

        string fileInfo = worldInfo.FileName;
        string dateFormatPattern = GuiSelectWorld.getDateFormatter(_parentWorldGui);
        DateTime lastPlayed = DateTimeOffset.FromUnixTimeMilliseconds(worldInfo.LastPlayed).ToLocalTime().DateTime;
        
        fileInfo += " (" + lastPlayed.ToString(dateFormatPattern);
        
        long size = worldInfo.Size;
        fileInfo += ", " + size / 1024L * 100L / 1024L / 100.0F + " MB)";
        
        string extraStatus = "";
        if (worldInfo.IsUnsupported)
        {
            extraStatus = GuiSelectWorld.getUnsupportedFormatMessage(_parentWorldGui) + " " + extraStatus;
        }

        Gui.DrawString(_parentWorldGui.FontRenderer, displayName, x + 2, y + 1, Color.White);
        Gui.DrawString(_parentWorldGui.FontRenderer, fileInfo, x + 2, y + 12, Color.Gray80);
        Gui.DrawString(_parentWorldGui.FontRenderer, extraStatus, x + 2, y + 12 + 10, Color.Gray80);
    }
}
