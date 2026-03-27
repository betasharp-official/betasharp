using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Inventorys;
using BetaSharp.Screens;

namespace BetaSharp.Client.UI.Screens.InGame.Containers;

public class DispenserScreen : ContainerScreen
{
    public DispenserScreen(InventoryPlayer inventory, BlockEntityDispenser dispenser)
        : base(new DispenserScreenHandler(inventory, dispenser))
    {
    }

    protected override void Init()
    {
        base.Init();

        // Background Image
        var background = new Image
        {
            Texture = Renderer.TextureManager.GetTextureId("/gui/trap.png"),
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
        var lblDispenser = new Label
        {
            Text = "Dispenser",
            HasShadow = false,
            TextColor = Color.Gray40
        };
        lblDispenser.Style.Position = PositionType.Absolute;
        lblDispenser.Style.Left = 60;
        lblDispenser.Style.Top = 6;
        _containerPanel.AddChild(lblDispenser);

        var lblInventory = new Label
        {
            Text = "Inventory",
            HasShadow = false,
            TextColor = Color.Gray40
        };
        lblInventory.Style.Position = PositionType.Absolute;
        lblInventory.Style.Left = 8;
        lblInventory.Style.Top = _ySize - 96 + 2;
        _containerPanel.AddChild(lblInventory);

        AddSlots();
    }
}
