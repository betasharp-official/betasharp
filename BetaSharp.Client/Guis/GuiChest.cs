using BetaSharp.Client.Rendering.Core;
using BetaSharp.Inventorys;
using BetaSharp.Screens;

namespace BetaSharp.Client.Guis;

public class GuiChest : GuiContainer
{

    private readonly IInventory _upperChestInventory;
    private readonly IInventory _lowerChestInventory;
    private readonly int _inventoryRows = 0;

    public GuiChest(IInventory upper, IInventory lower) : base(new GenericContainerScreenHandler(upper, lower))
    {
        _upperChestInventory = upper;
        _lowerChestInventory = lower;
        AllowUserInput = false;
        short baseHeight = 222;
        int guiHeightMinus = baseHeight - 108;
        _inventoryRows = lower.size() / 9;
        _ySize = guiHeightMinus + _inventoryRows * 18;
    }

    protected override void DrawGuiContainerForegroundLayer()
    {
        FontRenderer.DrawString(_lowerChestInventory.getName(), 8, 6, Color.Gray40);
        FontRenderer.DrawString(_upperChestInventory.getName(), 8, _ySize - 96 + 2, Color.Gray40);
    }

    protected override void DrawGuiContainerBackgroundLayer(float partialTicks)
    {
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        Game.textureManager.BindTexture(Game.textureManager.GetTextureId("/gui/container.png"));

        int guiLeft = (Width - _xSize) / 2;
        int guiTop = (Height - _ySize) / 2;

        DrawTexturedModalRect(guiLeft, guiTop, 0, 0, _xSize, _inventoryRows * 18 + 17);

        DrawTexturedModalRect(guiLeft, guiTop + _inventoryRows * 18 + 17, 0, 126, _xSize, 96);
    }
}
