using BetaSharp.Client.Rendering.Core;
using BetaSharp.Inventorys;
using BetaSharp.Screens;
using BetaSharp.Worlds;

namespace BetaSharp.Client.Guis;

public class GuiCrafting : GuiContainer
{
    public GuiCrafting(InventoryPlayer player, World world, int posX, int posY, int posZ) : base(new CraftingScreenHandler(player, world, posX, posY, posZ))
    {
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();
        InventorySlots.onClosed(MC.player);
    }

    protected override void DrawGuiContainerForegroundLayer()
    {
        FontRenderer.DrawString("Crafting", 28, 6, 0x404040);
        FontRenderer.DrawString("Inventory", 8, _ySize - 96 + 2, 0x404040);
    }

    protected override void DrawGuiContainerBackgroundLayer(float partialTicks)
    {
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        MC.textureManager.BindTexture(MC.textureManager.GetTextureId("/gui/crafting.png"));
        int guiLeft = (EffectiveWidth - _xSize) / 2;
        int guiTop = (EffectiveHeight - _ySize) / 2;
        DrawTextureRegion(guiLeft, guiTop, 0, 0, _xSize, _ySize);
    }
}
