using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Entities;

namespace BetaSharp.Client.UI.Screens.InGame.Containers;

public class InventoryScreen : ContainerScreen
{
    private EntityPreview _playerPreview = null!;

    public InventoryScreen(EntityPlayer player) : base(player.playerScreenHandler)
    {
        player.increaseStat(global::BetaSharp.Achievements.OpenInventory, 1);
    }

    protected override void Init()
    {
        base.Init();

        // Background Image
        var background = new Image
        {
            Texture = Renderer.TextureManager.GetTextureId("/gui/inventory.png"),
            U = 0,
            V = 0,
            UWidth = 176,
            VHeight = 166
        };
        background.Style.Width = _xSize;
        background.Style.Height = _ySize;
        background.Style.Position = PositionType.Absolute;
        _containerPanel.AddChild(background);

        _playerPreview = new EntityPreview
        {
            Entity = Game.player,
            Scale = 30.0f
        };
        _playerPreview.Style.Position = PositionType.Absolute;
        _playerPreview.Style.Left = 36;
        _playerPreview.Style.Top = 75 - 50; // Adjustment from legacy logic
        _playerPreview.Style.Width = 30;
        _playerPreview.Style.Height = 50;
        _containerPanel.AddChild(_playerPreview);

        // Labels
        var lblCrafting = new Label
        {
            Text = "Crafting",
            HasShadow = false,
            TextColor = Color.Gray40
        };
        lblCrafting.Style.Position = PositionType.Absolute;
        lblCrafting.Style.Left = 86;
        lblCrafting.Style.Top = 16;
        _containerPanel.AddChild(lblCrafting);

        AddSlots();
    }
}
