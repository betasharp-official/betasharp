using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Screens;
using BetaSharp.Screens.Slots;

namespace BetaSharp.Client.Guis;

public abstract class GuiContainer : GuiScreen
{
    private static readonly ItemRenderer s_itemRenderer = new();
    protected int _xSize = 176;
    protected int _ySize = 166;
    public ScreenHandler InventorySlots;
    protected Slot? _hoveredSlot;

    public override bool PausesGame => false;

    public GuiContainer(ScreenHandler inventorySlots)
    {
        InventorySlots = inventorySlots;
    }

    public override void InitGui()
    {
        base.InitGui();
        Game.player.currentScreenHandler = InventorySlots;
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

        _hoveredSlot = null;


        for (int i = 0; i < InventorySlots.Slots.Count; ++i)
        {
            Slot slot = InventorySlots.Slots[i];
            DrawSlotInventory(slot);
            if (GetIsMouseOverSlot(slot, mouseX, mouseY))
            {
                _hoveredSlot = slot;

                GLManager.GL.Disable(GLEnum.Lighting);
                GLManager.GL.Disable(GLEnum.DepthTest);
                int sx = slot.xDisplayPosition;
                int sy = slot.yDisplayPosition;
                DrawGradientRect(sx, sy, sx + 16, sy + 16, Color.BackgroundWhiteAlpha, Color.BackgroundWhiteAlpha);
                GLManager.GL.Enable(GLEnum.Lighting);
                GLManager.GL.Enable(GLEnum.DepthTest);
            }
        }

        InventoryPlayer playerInv = Game.player.inventory;

        GLManager.GL.Disable(GLEnum.RescaleNormal);
        Lighting.turnOff();
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Disable(GLEnum.DepthTest);
        DrawGuiContainerForegroundLayer();

        if (playerInv?.getCursorStack() == null && _hoveredSlot != null && _hoveredSlot.hasStack())
        {
            string itemName = TranslationStorage.Instance.TranslateNamedKey(_hoveredSlot.getStack().getItemName()).Trim();
            if (itemName.Length > 0)
            {
                int tipX = mouseX - guiLeft + 12;
                int tipY = mouseY - guiTop - 12;

                List<string> tooltipLines = [itemName];
                if (Game.options.AdvancedItemTooltips)
                {
                    tooltipLines.Add(_hoveredSlot.getStack().getDamage() != 0 ? $"{_hoveredSlot.getStack().itemId}:{_hoveredSlot.getStack().getDamage()}" : $"{_hoveredSlot.getStack().itemId}");
                }

                int maxTextWidth = tooltipLines.Select(line => FontRenderer.GetStringWidth(line)).Prepend(0).Max();

                int boxHeight = 8;
                if (tooltipLines.Count > 1)
                {
                    boxHeight += 2 + (tooltipLines.Count - 1) * 10;
                }

                DrawGradientRect(tipX - 3, tipY - 3, tipX + maxTextWidth + 3, tipY + boxHeight + 3, Color.BlackAlphaC0, Color.BlackAlphaC0);

                for (int i = 0; i < tooltipLines.Count; i++)
                {
                    FontRenderer.DrawStringWithShadow(tooltipLines[i], tipX, tipY, i == 0 ? Color.White : new Color((byte)170, (byte)170, (byte)170));
                    if (i == 0) tipY += 2;
                    tipY += 10;
                }
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
            s_itemRenderer.renderItemIntoGUI(FontRenderer, Game.textureManager, playerInv.getCursorStack(), mouseX - guiLeft - 8, mouseY - guiTop - 8);
            s_itemRenderer.renderItemOverlayIntoGUI(FontRenderer, Game.textureManager, playerInv.getCursorStack(), mouseX - guiLeft - 8, mouseY - guiTop - 8);

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

    protected virtual void DrawGuiContainerForegroundLayer()
    {
    }

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
                Game.textureManager.BindTexture(Game.textureManager.GetTextureId("/gui/items.png"));
                DrawTexturedModalRect(x, y, iconIdx % 16 * 16, iconIdx / 16 * 16, 16, 16);
                GLManager.GL.Enable(GLEnum.Lighting);
                return;
            }
        }

        s_itemRenderer.renderItemIntoGUI(FontRenderer, Game.textureManager, item, x, y);
        s_itemRenderer.renderItemOverlayIntoGUI(FontRenderer, Game.textureManager, item, x, y);
    }

    private Slot? GetSlotAtPosition(int mouseX, int mouseY)
    {
        for (int i = 0; i < InventorySlots.Slots.Count; ++i)
        {
            Slot slot = InventorySlots.Slots[i];
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
            Slot? slot = GetSlotAtPosition(x, y);
            int guiLeft = (Width - _xSize) / 2;
            int guiTop = (Height - _ySize) / 2;

            bool isOutside = x < guiLeft || y < guiTop || x >= guiLeft + _xSize || y >= guiTop + _ySize;

            int slotId = -1;
            if (slot != null) slotId = slot.id;
            if (isOutside) slotId = -999;
            if (slotId != -1)
            {
                bool isShiftClick = slotId != -999 && (Keyboard.isKeyDown(Keyboard.KEY_LSHIFT) || Keyboard.isKeyDown(Keyboard.KEY_RSHIFT));
                Game.playerController.func_27174_a(InventorySlots.SyncId, slotId, button, isShiftClick, Game.player);
            }
        }
    }

    protected override void HandleQuickMove(int x, int y)
    {
        Slot? slot = GetSlotAtPosition(x, y);
        if (slot != null)
        {
            Game.playerController.func_27174_a(InventorySlots.SyncId, slot.id, 0, true, Game.player);
        }
    }

    protected override void MouseMovedOrUp(int x, int y, int button)
    {
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        if (eventKey == Keyboard.KEY_ESCAPE || eventKey == Game.options.KeyBindInventory.keyCode)
        {
            Game.player.closeHandledScreen();
        }
    }

    public override void OnGuiClosed()
    {
        if (Game.player != null)
        {
            Game.playerController.func_20086_a(InventorySlots.SyncId, Game.player);
        }
    }


    public override void UpdateScreen()
    {
        base.UpdateScreen();
        if (!Game.player.isAlive() || Game.player.dead)
        {
            Game.player.closeHandledScreen();
        }
    }

    public override bool HandleDPadNavigation(int dpadX, int dpadY, ref float cursorX, ref float cursorY)
    {
        int guiLeft = (Width - _xSize) / 2;
        int guiTop = (Height - _ySize) / 2;

        ScaledResolution sr = new(Game.options, Game.displayWidth, Game.displayHeight);

        int scaledMouseX = (int)(cursorX * sr.ScaledWidth / Game.displayWidth);
        int scaledMouseY = (int)(cursorY * sr.ScaledHeight / Game.displayHeight);

        Slot? currentSlot = GetSlotAtPosition(scaledMouseX, scaledMouseY);

        float refX, refY;
        if (currentSlot != null)
        {
            refX = currentSlot.xDisplayPosition + 8;
            refY = currentSlot.yDisplayPosition + 8;
        }
        else
        {
            refX = scaledMouseX - guiLeft;
            refY = scaledMouseY - guiTop;
        }

        Slot? bestSlot = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < InventorySlots.Slots.Count; i++)
        {
            Slot slot = InventorySlots.Slots[i];
            if (slot == currentSlot) continue;

            float slotCenterX = slot.xDisplayPosition + 8;
            float slotCenterY = slot.yDisplayPosition + 8;

            float dx = slotCenterX - refX;
            float dy = slotCenterY - refY;

            if (dpadX > 0 && dx <= 0) continue;
            if (dpadX < 0 && dx >= 0) continue;
            if (dpadY > 0 && dy <= 0) continue;
            if (dpadY < 0 && dy >= 0) continue;

            float primaryDist = dpadX != 0 ? Math.Abs(dx) : Math.Abs(dy);
            float crossDist = dpadX != 0 ? Math.Abs(dy) : Math.Abs(dx);
            float score = primaryDist + crossDist * 3f;

            if (score < bestScore)
            {
                bestScore = score;
                bestSlot = slot;
            }
        }

        if (bestSlot != null)
        {
            float targetScaledX = guiLeft + bestSlot.xDisplayPosition + 8;
            float targetScaledY = guiTop + bestSlot.yDisplayPosition + 8;
            cursorX = targetScaledX * Game.displayWidth / sr.ScaledWidth;
            cursorY = targetScaledY * Game.displayHeight / sr.ScaledHeight;
            return true;
        }

        return base.HandleDPadNavigation(dpadX, dpadY, ref cursorX, ref cursorY);
    }

    public override void GetTooltips(List<ActionTip> tips)
    {
        if (_hoveredSlot != null)
        {
            ItemStack cursorStack = Game.player.inventory.getCursorStack();
            if (_hoveredSlot.hasStack() || cursorStack != null)
            {
                tips.Add(new ActionTip(ControlIcon.A, "Move"));
            }

            if (_hoveredSlot.hasStack())
            {
                tips.Add(new ActionTip(ControlIcon.Y, "Quick Move"));
                if (_hoveredSlot.getStack().count > 1)
                {
                    tips.Add(new ActionTip(ControlIcon.X, "Take Half"));
                }
            }
        }
    }
}
