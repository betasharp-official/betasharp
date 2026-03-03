using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Inventorys;
using BetaSharp.Screens;

namespace BetaSharp.Client.Guis;

public class GuiFurnace : GuiContainer
{

    private readonly BlockEntityFurnace _furnaceInventory;

    public GuiFurnace(InventoryPlayer playerInventory, BlockEntityFurnace furnace) : base(new FurnaceScreenHandler(playerInventory, furnace))
    {
        _furnaceInventory = furnace;
    }

    protected override void DrawGuiContainerForegroundLayer()
    {
        FontRenderer.DrawString("Furnace", 60, 6, Color.Gray40);
        FontRenderer.DrawString("Inventory", 8, _ySize - 96 + 2, Color.Gray40);
    }

    protected override void DrawGuiContainerBackgroundLayer(float partialTicks)
    {
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        Game.textureManager.BindTexture(Game.textureManager.GetTextureId("/gui/furnace.png"));
        int guiLeft = (Width - _xSize) / 2;
        int guiTop = (Height - _ySize) / 2;
        DrawTexturedModalRect(guiLeft, guiTop, 0, 0, _xSize, _ySize);
        int progress;
        if (_furnaceInventory.isBurning())
        {
            progress = _furnaceInventory.getFuelTimeDelta(12);
            DrawTexturedModalRect(guiLeft + 56, guiTop + 36 + 12 - progress, 176, 12 - progress, 14, progress + 2);
        }

        progress = _furnaceInventory.getCookTimeDelta(24);
        DrawTexturedModalRect(guiLeft + 79, guiTop + 34, 176, 14, progress + 1, 16);
    }
}
