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
        InventorySlots.onClosed(Game.player);
    }

    protected override void DrawGuiContainerForegroundLayer()
    {
        FontRenderer.DrawString("Crafting", 28, 6, Color.Gray40);
        FontRenderer.DrawString("Inventory", 8, _ySize - 96 + 2, Color.Gray40);
    }

    protected override void DrawGuiContainerBackgroundLayer(float partialTicks)
    {
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        Game.textureManager.BindTexture(Game.textureManager.GetTextureId("/gui/crafting.png"));
        int guiLeft = (Width - _xSize) / 2;
        int guiTop = (Height - _ySize) / 2;
        DrawTexturedModalRect(guiLeft, guiTop, 0, 0, _xSize, _ySize);
    }
}
