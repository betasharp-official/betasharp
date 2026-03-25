using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Inventorys;
using BetaSharp.Screens;

namespace BetaSharp.Client.Guis;

public class GuiDispenser : GuiContainer
{
    public GuiDispenser(InventoryPlayer inventory, BlockEntityDispenser dispenser) : base(new DispenserScreenHandler(inventory, dispenser))
    {
    }

    protected override void DrawGuiContainerForegroundLayer()
    {
        FontRenderer.DrawString("Dispenser", 60, 6, 0x404040);
        FontRenderer.DrawString("Inventory", 8, _ySize - 96 + 2, 0x404040);
    }

    protected override void DrawGuiContainerBackgroundLayer(float partialTicks)
    {
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        MC.textureManager.BindTexture(MC.textureManager.GetTextureId("/gui/trap.png"));
        int guiLeft = (EffectiveWidth - _xSize) / 2;
        int guiTop = (EffectiveHeight - _ySize) / 2;
        DrawTextureRegion(guiLeft, guiTop, 0, 0, _xSize, _ySize);
    }
}
