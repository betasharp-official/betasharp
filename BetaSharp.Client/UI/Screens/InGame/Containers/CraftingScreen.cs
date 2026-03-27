using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Inventorys;
using BetaSharp.Screens;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.UI.Screens.InGame.Containers;

public class CraftingScreen : ContainerScreen
{
    public CraftingScreen(InventoryPlayer player, IWorldContext world, int posX, int posY, int posZ)
        : base(new CraftingScreenHandler(player, world, posX, posY, posZ))
    {
    }

    protected override void Init()
    {
        base.Init();

        // Background Image
        var background = new Image
        {
            Texture = Renderer.TextureManager.GetTextureId("/gui/crafting.png"),
            U = 0,
            V = 0,
            UWidth = 176,
            VHeight = 166
        };
        background.Style.Width = _xSize;
        background.Style.Height = _ySize;
        background.Style.Position = PositionType.Absolute;
        _containerPanel.AddChild(background);

        // Labels
        var lblCrafting = new Label { Text = "Crafting", HasShadow = false };
        lblCrafting.TextColor = Color.Gray40;
        lblCrafting.Style.Position = PositionType.Absolute;
        lblCrafting.Style.Left = 28;
        lblCrafting.Style.Top = 6;
        _containerPanel.AddChild(lblCrafting);

        var lblInventory = new Label { Text = "Inventory", HasShadow = false };
        lblInventory.TextColor = Color.Gray40;
        lblInventory.Style.Position = PositionType.Absolute;
        lblInventory.Style.Left = 8;
        lblInventory.Style.Top = _ySize - 96 + 2;
        _containerPanel.AddChild(lblInventory);

        AddSlots();
    }
}
