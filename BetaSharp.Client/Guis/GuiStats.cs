using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Items;
using BetaSharp.Stats;

namespace BetaSharp.Client.Guis;

public class GuiStats : GuiScreen
{
    private static readonly ItemRenderer itemRenderer = new();
    protected GuiScreen parentScreen;
    protected string screenTitle = "Select world";
    private GuiSlotStatsGeneral slotGeneral;
    private GuiSlotStatsItem slotItem;
    private GuiSlotStatsBlock slotBlock;
    public StatFileWriter statFileWriter { get; }
    private GuiSlot currentSlot;

    public GuiStats(GuiScreen parent, StatFileWriter stats)
    {
        parentScreen = parent;
        statFileWriter = stats;
    }

    public override void InitGui()
    {
        screenTitle = StatCollector.TranslateToLocal("gui.stats");
        slotGeneral = new GuiSlotStatsGeneral(this);
        slotGeneral.RegisterScrollButtons(_controlList, 1, 1);
        slotItem = new GuiSlotStatsItem(this);
        slotItem.RegisterScrollButtons(_controlList, 1, 1);
        slotBlock = new GuiSlotStatsBlock(this);
        slotBlock.RegisterScrollButtons(_controlList, 1, 1);
        currentSlot = slotGeneral;
        initButtons();
    }
    public void initButtons()
    {
        const int BUTTON_DONE = 0;
        const int BUTTON_GENERAL = 1;
        const int BUTTON_BLOCKS = 2;
        const int BUTTON_ITEMS = 3;

        TranslationStorage translations = TranslationStorage.Instance;
        _controlList.Add(new GuiButton(BUTTON_DONE, Width / 2 + 4, Height - 28, 150, 20, translations.TranslateKey("gui.done")));
        _controlList.Add(new GuiButton(BUTTON_GENERAL, Width / 2 - 154, Height - 52, 100, 20, translations.TranslateKey("stat.generalButton")));
        GuiButton blocksButton = new(BUTTON_BLOCKS, Width / 2 - 46, Height - 52, 100, 20, translations.TranslateKey("stat.blocksButton"));
        _controlList.Add(blocksButton);
        GuiButton itemsButton = new(BUTTON_ITEMS, Width / 2 + 62, Height - 52, 100, 20, translations.TranslateKey("stat.itemsButton"));
        _controlList.Add(itemsButton);
        if (slotBlock.GetSize() == 0)
        {
            blocksButton.Enabled = false;
        }

        if (slotItem.GetSize() == 0)
        {
            itemsButton.Enabled = false;
        }
    }

    protected override void ActionPerformed(GuiButton button)
    {
        if (button.Enabled)
        {
            switch (button.Id)
            {
                case 0: // DONE
                    Game.displayGuiScreen(parentScreen);
                    break;
                case 1: // GENERAL
                    currentSlot = slotGeneral;
                    break;
                case 3: // ITEMS
                    currentSlot = slotItem;
                    break;
                case 2: // BLOCKS
                    currentSlot = slotBlock;
                    break;
                default:
                    currentSlot.ActionPerformed(button);
                    break;
            }

        }
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        currentSlot.DrawScreen(mouseX, mouseY, partialTicks);
        DrawCenteredString(FontRenderer, screenTitle, Width / 2, 20, Color.White);
        base.Render(mouseX, mouseY, partialTicks);
    }

    public void drawItemSlot(int x, int y, int itemId)
    {
        drawSlotBackground(x + 1, y + 1);
        GLManager.GL.Enable(GLEnum.RescaleNormal);
        GLManager.GL.PushMatrix();
        GLManager.GL.Rotate(180.0F, 1.0F, 0.0F, 0.0F);
        Lighting.turnOn();
        GLManager.GL.PopMatrix();
        itemRenderer.drawItemIntoGui(FontRenderer, Game.textureManager, itemId, 0, Item.ITEMS[itemId].getTextureId(0), x + 2, y + 2);
        Lighting.turnOff();
        GLManager.GL.Disable(GLEnum.RescaleNormal);
    }

    private void drawSlotBackground(int x, int y)
    {
        drawSlotTexture(x, y, 0, 0);
    }

    private void drawSlotTexture(int x, int y, int u, int v)
    {
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        Game.textureManager.BindTexture(Game.textureManager.GetTextureId("/gui/slot.png"));
        Tessellator tessellator = Tessellator.instance;
        tessellator.startDrawingQuads();
        tessellator.addVertexWithUV(x + 0, y + 18, _zLevel, (double)((u + 0) * 0.0078125F), (double)((v + 18) * 0.0078125F));
        tessellator.addVertexWithUV(x + 18, y + 18, _zLevel, (double)((u + 18) * 0.0078125F), (double)((v + 18) * 0.0078125F));
        tessellator.addVertexWithUV(x + 18, y + 0, _zLevel, (double)((u + 18) * 0.0078125F), (double)((v + 0) * 0.0078125F));
        tessellator.addVertexWithUV(x + 0, y + 0, _zLevel, (double)((u + 0) * 0.0078125F), (double)((v + 0) * 0.0078125F));
        tessellator.draw();
    }

    public void drawTranslucentRect(int right, int bottom, int left, int top)
    {
        DrawGradientRect(right, bottom, left, top, Color.BlackAlphaC0, Color.BlackAlphaC0);
    }
}
