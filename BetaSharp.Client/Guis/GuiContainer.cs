using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Screens;
using BetaSharp.Screens.Slots;
using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Guis;

public abstract class GuiContainer : GuiScreen
{

    private static readonly ItemRenderer _itemRenderer = new();
    protected int _xSize = 176;
    protected int _ySize = 166;
    public ScreenHandler InventorySlots;

    public override bool PausesGame => false;

    public GuiContainer(ScreenHandler inventorySlots)
    {
        InventorySlots = inventorySlots;
    }

    public override void InitGui()
    {
        base.InitGui();
        mc.player.currentScreenHandler = InventorySlots;
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawDefaultBackground();

        int guiLeft = (Width - _xSize) / 2;
        int guiTop = (Height - _ySize) / 2;

        DrawGuiContainerBackgroundLayer(partialTicks);

        GLManager.GL.PushMatrix();
        GLManager.GL.Rotate(120.0F, 1.0F, 0.0F, 0.0F);
        Lighting.turnOn();
        GLManager.GL.PopMatrix();

        GLManager.GL.PushMatrix();
        GLManager.GL.Translate(guiLeft, guiTop, 0.0F);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        GLManager.GL.Enable(GLEnum.RescaleNormal);

        Slot hoveredSlot = null;


        for (int i = 0; i < InventorySlots.Slots.Count; ++i)
        {
            Slot slot = InventorySlots.Slots[i];
            DrawSlotInventory(slot);
            if (GetIsMouseOverSlot(slot, mouseX, mouseY))
            {
                hoveredSlot = slot;

                GLManager.GL.Disable(GLEnum.Lighting);
                GLManager.GL.Disable(GLEnum.DepthTest);
                int sx = slot.xDisplayPosition;
                int sy = slot.yDisplayPosition;
                DrawGradientRect(sx, sy, sx + 16, sy + 16, Color.BackgroundWhiteAlpha, Color.BackgroundWhiteAlpha);
                GLManager.GL.Enable(GLEnum.Lighting);
                GLManager.GL.Enable(GLEnum.DepthTest);
            }
        }

        InventoryPlayer playerInv = mc.player.inventory;

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
                int tipX = mouseX - guiLeft + 12;
                int tipY = mouseY - guiTop - 12;
                int textWidth = FontRenderer.GetStringWidth(itemName);

                DrawGradientRect(tipX - 3, tipY - 3, tipX + textWidth + 3, tipY + 8 + 3, Color.BlackAlphaC0, Color.BlackAlphaC0);
                FontRenderer.DrawStringWithShadow(itemName, tipX, tipY, Color.White);
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
            _itemRenderer.renderItemIntoGUI(FontRenderer, mc.textureManager, playerInv.getCursorStack(), mouseX - guiLeft - 8, mouseY - guiTop - 8);
            _itemRenderer.renderItemOverlayIntoGUI(FontRenderer, mc.textureManager, playerInv.getCursorStack(), mouseX - guiLeft - 8, mouseY - guiTop - 8);

            Lighting.turnOff();
            GLManager.GL.Disable(GLEnum.Lighting);
            GLManager.GL.Disable(GLEnum.DepthTest);
            GLManager.GL.Disable(GLEnum.RescaleNormal);
        }

        GLManager.GL.PopMatrix();
        base.Render(mouseX, mouseY, partialTicks);

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
                mc.textureManager.BindTexture(mc.textureManager.GetTextureId("/gui/items.png"));
                DrawTexturedModalRect(x, y, iconIdx % 16 * 16, iconIdx / 16 * 16, 16, 16);
                GLManager.GL.Enable(GLEnum.Lighting);
                return;
            }
        }

        _itemRenderer.renderItemIntoGUI(FontRenderer, mc.textureManager, item, x, y);
        _itemRenderer.renderItemOverlayIntoGUI(FontRenderer, mc.textureManager, item, x, y);
    }

    private Slot GetSlotAtPosition(int mouseX, int mouseY)
    {
        for (int i = 0; i < InventorySlots.Slots.Count; ++i)
        {
            Slot slot = (Slot)InventorySlots.Slots[i];
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

    protected override void MouseClicked(int x, int y, int button)
    {
        base.MouseClicked(x, y, button);
        if (button == 0 || button == 1)
        {
            Slot slot = GetSlotAtPosition(x, y);
            int guiLeft = (Width - _xSize) / 2;
            int guiTop = (Height - _ySize) / 2;

            bool isOutside = x < guiLeft || y < guiTop || x >= guiLeft + _xSize || y >= guiTop + _ySize;

            int slotId = -1;
            if (slot != null) slotId = slot.id;
            if (isOutside) slotId = -999;
            if (slotId != -1)
            {
                bool isShiftClick = slotId != -999 && (Keyboard.isKeyDown(Keyboard.KEY_LSHIFT) || Keyboard.isKeyDown(Keyboard.KEY_RSHIFT));
                mc.playerController.func_27174_a(InventorySlots.SyncId, slotId, button, isShiftClick, mc.player);
            }
        }

    }

    protected override void MouseMovedOrUp(int x, int y, int button) { }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        if (eventKey == Keyboard.KEY_ESCAPE || eventKey == mc.options.KeyBindInventory.keyCode)
        {
            mc.player.closeHandledScreen();
        }

    }

    public override void OnGuiClosed()
    {
        if (mc.player != null)
        {
            mc.playerController.func_20086_a(InventorySlots.SyncId, mc.player);
        }
    }


    public override void UpdateScreen()
    {
        base.UpdateScreen();
        if (!mc.player.isAlive() || mc.player.dead)
        {
            mc.player.closeHandledScreen();
        }

    }
}
