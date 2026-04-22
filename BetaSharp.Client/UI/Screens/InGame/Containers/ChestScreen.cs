using BetaSharp.Client.Entities;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Input;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Inventorys;
using BetaSharp.Screens;

namespace BetaSharp.Client.UI.Screens.InGame.Containers;

public class ChestScreen : ContainerScreen
{
    private readonly IInventory _upperInventory;
    private readonly IInventory _lowerInventory;
    private readonly int _inventoryRows;

    public ChestScreen(
        UIContext context,
        ClientPlayerEntity player,
        PlayerController playerController,
        IInventory upperInventory,
        IInventory lowerInventory)
        : base(context, player, playerController, new GenericContainerScreenHandler(upperInventory, lowerInventory))
    {
        _upperInventory = upperInventory;
        _lowerInventory = lowerInventory;
        _inventoryRows = lowerInventory.Size / 9;
        _ySize = 114 + _inventoryRows * 18;
    }

    protected override void Init()
    {
        base.Init();

        // Background Image split into two parts to handle single/double chests
        int topHeight = _inventoryRows * 18 + 17;
        var topBg = new Image
        {
            Texture = Renderer.TextureManager.GetTextureId("/gui/container.png"),
            U = 0,
            V = 0,
            UWidth = 176,
            VHeight = topHeight
        };
        topBg.Style.Width = _xSize;
        topBg.Style.Height = topHeight;
        topBg.Style.Position = PositionType.Absolute;
        _containerPanel.AddChild(topBg);

        var bottomBg = new Image
        {
            Texture = Renderer.TextureManager.GetTextureId("/gui/container.png"),
            U = 0,
            V = 126,
            UWidth = 176,
            VHeight = 96
        };
        bottomBg.Style.Width = _xSize;
        bottomBg.Style.Height = 96;
        bottomBg.Style.Position = PositionType.Absolute;
        bottomBg.Style.Top = topHeight;
        _containerPanel.AddChild(bottomBg);

        // Labels
        var lblUpper = new Label
        {
            Text = _lowerInventory.Name,
            HasShadow = false,
            TextColor = Color.Gray40
        };
        lblUpper.Style.Position = PositionType.Absolute;
        lblUpper.Style.Left = 8;
        lblUpper.Style.Top = 6;
        _containerPanel.AddChild(lblUpper);

        var lblLower = new Label
        {
            Text = _upperInventory.Name,
            HasShadow = false,
            TextColor = Color.Gray40
        };
        lblLower.Style.Position = PositionType.Absolute;
        lblLower.Style.Left = 8;
        lblLower.Style.Top = _ySize - 96 + 2;
        _containerPanel.AddChild(lblLower);

        AddSlots();
    }
}
