using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Screens;
using BetaSharp.Screens.Slots;
using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Guis;

public abstract class GuiContainer : Screen
{
    private static readonly ItemRenderer _itemRenderer = new();
    protected int _xSize = 176;
    protected int _ySize = 166;
    public ScreenHandler InventorySlots;

    public override bool PausesGame => false;

    public GuiContainer(ScreenHandler inventorySlots)
    {
        InventorySlots = inventorySlots;
        MC.player.currentScreenHandler = InventorySlots;
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();

        int guiLeft = (Width - _xSize) / 2;
        int guiTop = (Height - _ySize) / 2;

        DrawGuiContainerBackgroundLayer(e.TickDelta);

        GLManager.GL.PushMatrix();
        GLManager.GL.Rotate(120.0F, 1.0F, 0.0F, 0.0F);
        Lighting.turnOn();
        GLManager.GL.PopMatrix();

        GLManager.GL.PushMatrix();
        GLManager.GL.Translate(guiLeft, guiTop, 0.0F);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        GLManager.GL.Enable(GLEnum.RescaleNormal);

        Slot? hoveredSlot = null;


        for (int i = 0; i < InventorySlots.slots.size(); ++i)
        {
            Slot slot = (Slot)InventorySlots.slots.get(i);
            DrawSlotInventory(slot);
            if (GetIsMouseOverSlot(slot, e.MouseX, e.MouseY))
            {
                hoveredSlot = slot;

                GLManager.GL.Disable(GLEnum.Lighting);
                GLManager.GL.Disable(GLEnum.DepthTest);
                int sx = slot.xDisplayPosition;
                int sy = slot.yDisplayPosition;
                Gui.DrawGradientRect(sx, sy, sx + 16, sy + 16, 0x80FFFFFF, 0x80FFFFFF);
                GLManager.GL.Enable(GLEnum.Lighting);
                GLManager.GL.Enable(GLEnum.DepthTest);
            }
        }

        InventoryPlayer playerInv = MC.player.inventory;

        GLManager.GL.Disable(GLEnum.RescaleNormal);
        Lighting.turnOff();
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Disable(GLEnum.DepthTest);
        DrawGuiContainerForegroundLayer();

        if (playerInv.getCursorStack() == null && hoveredSlot != null && hoveredSlot.hasStack())
        {
            string itemName = ("" + TranslationStorage.Instance.TranslateNamedKey(hoveredSlot.getStack().getItemName())).Trim();
            if (itemName.Length > 0)
            {
                int tipX = e.MouseX - guiLeft + 12;
                int tipY = e.MouseY - guiTop - 12;
                int textWidth = FontRenderer.GetStringWidth(itemName);

                Gui.DrawGradientRect(tipX - 3, tipY - 3, tipX + textWidth + 3, tipY + 8 + 3, 0xC0000000, 0xC0000000);
                FontRenderer.DrawStringWithShadow(itemName, tipX, tipY, 0xFFFFFFFF);
            }
        }

        if (playerInv.getCursorStack() != null)
        {
            GLManager.GL.Enable(GLEnum.RescaleNormal);
            GLManager.GL.PushMatrix();
            GLManager.GL.Rotate(120.0F, 1.0F, 0.0F, 0.0F);
            GLManager.GL.Rotate(-90.0F, 0.0F, 1.0F, 0.0F);
            Lighting.turnOn();
            GLManager.GL.PopMatrix();
            GLManager.GL.Enable(GLEnum.Lighting);
            GLManager.GL.Enable(GLEnum.DepthTest);

            GLManager.GL.Translate(0.0F, 0.0F, 32.0F);
            _itemRenderer.renderItemIntoGUI(FontRenderer, MC.textureManager, playerInv.getCursorStack(), e.MouseX - guiLeft - 8, e.MouseY - guiTop - 8);
            _itemRenderer.renderItemOverlayIntoGUI(FontRenderer, MC.textureManager, playerInv.getCursorStack(), e.MouseX - guiLeft - 8, e.MouseY - guiTop - 8);

            Lighting.turnOff();
            GLManager.GL.Disable(GLEnum.Lighting);
            GLManager.GL.Disable(GLEnum.DepthTest);
            GLManager.GL.Disable(GLEnum.RescaleNormal);
        }

        GLManager.GL.PopMatrix();
        GLManager.GL.Enable(GLEnum.Lighting);
        GLManager.GL.Enable(GLEnum.DepthTest);
    }

    protected virtual void DrawGuiContainerForegroundLayer() { }

    protected abstract void DrawGuiContainerBackgroundLayer(float partialTicks);

    private void DrawSlotInventory(Slot slot)
    {
        int x = slot.xDisplayPosition;
        int y = slot.yDisplayPosition;
        ItemStack item = slot.getStack();
        if (item == null)
        {
            int iconIdx = slot.getBackgroundTextureId();
            if (iconIdx >= 0)
            {
                GLManager.GL.Disable(GLEnum.Lighting);
                MC.textureManager.BindTexture(MC.textureManager.GetTextureId("/gui/items.png"));
                DrawTextureRegion(x, y, iconIdx % 16 * 16, iconIdx / 16 * 16, 16, 16);
                GLManager.GL.Enable(GLEnum.Lighting);
                return;
            }
        }

        _itemRenderer.renderItemIntoGUI(FontRenderer, MC.textureManager, item, x, y);
        _itemRenderer.renderItemOverlayIntoGUI(FontRenderer, MC.textureManager, item, x, y);
    }

    private Slot? GetSlotAtPosition(int mouseX, int mouseY)
    {
        for (int i = 0; i < InventorySlots.slots.size(); ++i)
        {
            Slot slot = (Slot)InventorySlots.slots.get(i);
            if (GetIsMouseOverSlot(slot, mouseX, mouseY))
            {
                return slot;
            }
        }

        return null;
    }

    private bool GetIsMouseOverSlot(Slot slot, int mouseX, int mouseY)
    {
        int guiLeft = (Width - _xSize) / 2;
        int guiTop = (Height - _ySize) / 2;
        mouseX -= guiLeft;
        mouseY -= guiTop;

        return mouseX >= slot.xDisplayPosition - 1 &&
               mouseX < slot.xDisplayPosition + 16 + 1 &&
               mouseY >= slot.yDisplayPosition - 1 &&
               mouseY < slot.yDisplayPosition + 16 + 1;
    }

    protected override void OnClick(MouseEventArgs e)
    {
        if (e.Button is 0 or 1)
        {
            Slot slot = GetSlotAtPosition(e.X, e.Y);
            int guiLeft = (Width - _xSize) / 2;
            int guiTop = (Height - _ySize) / 2;

            bool isOutside = e.X < guiLeft || e.Y < guiTop || e.X >= guiLeft + _xSize || e.Y >= guiTop + _ySize;

            int slotId = -1;
            if (slot != null) slotId = slot.id;
            if (isOutside) slotId = -999;
            if (slotId != -1)
            {
                bool isShiftClick = slotId != -999 && (Keyboard.isKeyDown(Keyboard.KEY_LSHIFT) || Keyboard.isKeyDown(Keyboard.KEY_RSHIFT));
                MC.playerController.func_27174_a(InventorySlots.syncId, slotId, e.Button, isShiftClick, MC.player);
            }
        }
    }

    protected override void OnKeyInput(KeyboardEventArgs e)
    {
        if (e.IsKeyDown && (e.Key == Keyboard.KEY_ESCAPE || e.Key == MC.options.KeyBindInventory.keyCode))
        {
            MC.player.closeHandledScreen();
        }
    }

    public override void OnGuiClosed()
    {
        if (MC.player != null)
        {
            MC.playerController.func_20086_a(InventorySlots.syncId, MC.player);
        }
    }


    protected override void OnTick()
    {
        if (!MC.player.isAlive() || MC.player.dead)
        {
            MC.player.closeHandledScreen();
        }
    }
}
