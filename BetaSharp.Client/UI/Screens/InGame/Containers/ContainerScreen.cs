using BetaSharp.Client.Entities;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Input;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Items;
using BetaSharp.Screens;
using BetaSharp.Screens.Slots;
using Silk.NET.GLFW;

namespace BetaSharp.Client.UI.Screens.InGame.Containers;

public abstract class ContainerScreen(
    UIContext context,
    ClientPlayerEntity player,
    PlayerController playerController,
    ScreenHandler inventorySlots) : UIScreen(context)
{
    public ScreenHandler InventorySlots { get; } = inventorySlots;
    protected int _xSize = 176;
    protected int _ySize = 166;
    protected Panel _containerPanel = null!;

    public override bool PausesGame => false;

    protected override void Init()
    {
        player.currentScreenHandler = InventorySlots;

        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.Center;

        Root.AddChild(new Background(BackgroundType.World));

        _containerPanel = new Panel();
        _containerPanel.Style.Width = _xSize;
        _containerPanel.Style.Height = _ySize;
        _containerPanel.Style.Position = PositionType.Relative;
        Root.AddChild(_containerPanel);
    }

    protected void AddSlots()
    {
        foreach (Slot slot in InventorySlots.Slots)
        {
            var uiSlot = new UISlot(slot);
            uiSlot.Style.Position = PositionType.Absolute;
            uiSlot.Style.Left = slot.xDisplayPosition;
            uiSlot.Style.Top = slot.yDisplayPosition;
            uiSlot.OnMouseDown += (e) => OnSlotClick(uiSlot, e.Button);
            _containerPanel.AddChild(uiSlot);
        }
    }

    private void OnSlotClick(UISlot uiSlot, MouseButton button)
    {
        int slotId = uiSlot.Slot.id;
        bool isShiftClick = Keyboard.isKeyDown(Keyboard.KEY_LSHIFT) || Keyboard.isKeyDown(Keyboard.KEY_RSHIFT);
        int mouseBtn = (button == MouseButton.Right) ? 1 : 0;

        playerController.func_27174_a(InventorySlots.SyncId, slotId, mouseBtn, isShiftClick, player);
    }

    public override void Update(float partialTicks)
    {
        base.Update(partialTicks);
        if (!player.isAlive() || player.dead)
        {
            player.closeHandledScreen();
        }
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        base.Render(mouseX, mouseY, partialTicks);

        // Render held item on top of everything
        ItemStack cursorStack = player.inventory.GetCursorStack();
        if (cursorStack != null)
        {
            Renderer.Begin();
            Renderer.ClearDepth();
            Renderer.DrawItem(cursorStack, mouseX - 8, mouseY - 8);
            Renderer.DrawItemOverlay(cursorStack, mouseX - 8, mouseY - 8);
            Renderer.End();
        }

        // Tooltip rendering
        if (Root.HitTest(MouseX, MouseY) is UISlot hoveredSlot && cursorStack == null)
        {
            ItemStack stack = hoveredSlot.Slot.getStack();
            if (stack != null)
            {
                string itemName = ("" + TranslationStorage.Instance.TranslateNamedKey(stack.getItemName())).Trim();
                if (itemName.Length > 0)
                {
                    int textWidth = Context.TextRenderer.GetStringWidth(itemName);
                    float tx = MouseX + 12;
                    float ty = MouseY - 12;

                    Renderer.Begin();
                    Renderer.DrawGradientRect(tx - 3, ty - 3, textWidth + 6, 14, Color.BlackAlphaC0, Color.BlackAlphaC0);
                    Renderer.DrawText(itemName, tx, ty, Color.White);
                    Renderer.End();
                }
            }
        }
    }

    public override void GetTooltips(List<ActionTip> tips)
    {
        ItemStack cursorStack = player.inventory.GetCursorStack();

        if (Root.HitTest(MouseX, MouseY) is UISlot hoveredSlot)
        {
            ItemStack slotStack = hoveredSlot.Slot.getStack();

            if (cursorStack == null && slotStack != null)
            {
                tips.Add(new ActionTip(ControlIcon.A, "Move"));
                tips.Add(new ActionTip(ControlIcon.Y, "Quick Move"));
                if (slotStack.count > 1)
                    tips.Add(new ActionTip(ControlIcon.X, "Take Half"));
            }
            else if (cursorStack != null)
            {
                tips.Add(new ActionTip(ControlIcon.A, "Place"));
                tips.Add(new ActionTip(ControlIcon.X, "Place One"));
            }
        }
    }

    public override void HandleControllerInput()
    {
        var button = (GamepadButton)Controller.GetEventButton();
        bool isDown = Controller.GetEventButtonState();

        if (isDown && (button == GamepadButton.X || button == GamepadButton.Y))
        {
            if (GetElementUnderVirtualCursor() is UISlot uiSlot)
            {
                int slotId = uiSlot.Slot.id;
                if (button == GamepadButton.Y)
                    playerController.func_27174_a(InventorySlots.SyncId, slotId, 0, true, player);
                else
                    playerController.func_27174_a(InventorySlots.SyncId, slotId, 1, false, player);
                return;
            }
        }

        base.HandleControllerInput();
    }

    public override void KeyTyped(int key, char character)
    {
        if (key == Keyboard.KEY_ESCAPE || key == Context.Options.KeyBindInventory.keyCode)
        {
            player.closeHandledScreen();
        }
        else
        {
            base.KeyTyped(key, character);
        }
    }

    public override void Uninit()
    {
        base.Uninit();
        if (player != null)
        {
            playerController.OnGuiClosed(InventorySlots.SyncId, player);
        }
    }
}
