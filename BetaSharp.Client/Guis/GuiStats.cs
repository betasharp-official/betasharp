using BetaSharp.Client.Guis.Controls;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Items;
using BetaSharp.Stats;
using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Guis;

public class GuiStats : Screen
{
    private static readonly ItemRenderer itemRenderer = new();
    protected Screen parentScreen;
    private GuiListStatsGeneral _listGeneral;
    private GuiListStatsItem _listItem;
    private GuiListStatsBlock _listBlock;
    public StatFileWriter statFileWriter { get; }
    private GuiList _currentList;

    public GuiStats(Screen parent, StatFileWriter stats)
    {
        parentScreen = parent;
        statFileWriter = stats;

        Text = StatCollector.TranslateToLocal("gui.stats");
        DisplayTitle = true;

        _listGeneral = new GuiListStatsGeneral(this);
        _listItem = new GuiListStatsItem(this);
        _listBlock = new GuiListStatsBlock(this);
        _currentList = _listGeneral;

        TranslationStorage translations = TranslationStorage.Instance;
        Button doneButton = new(EffectiveWidth / 2 + 4, EffectiveHeight - 28, 150, translations.TranslateKey("gui.done"));
        Button generalButton = new(EffectiveWidth / 2 - 154, EffectiveHeight - 52, 100, translations.TranslateKey("stat.generalButton"));
        Button blocksButton = new(EffectiveWidth / 2 - 46, EffectiveHeight - 52, 100, translations.TranslateKey("stat.blocksButton")) { Enabled = _listBlock.GetSize() > 0 };
        Button itemsButton = new(EffectiveWidth / 2 + 62, EffectiveHeight - 52, 100, translations.TranslateKey("stat.itemsButton")) { Enabled = _listItem.GetSize() > 0 };

        doneButton.Clicked += (_, _) => MC.OpenScreen(parentScreen);
        generalButton.Clicked += (_, _) => _currentList = _listGeneral;
        blocksButton.Clicked += (_, _) => _currentList = _listBlock;
        itemsButton.Clicked += (_, _) => _currentList = _listItem;

        AddChildren(doneButton, generalButton, blocksButton, itemsButton);
    }

    public void drawItemSlot(int x, int y, int itemId)
    {
        drawSlotBackground(x + 1, y + 1);
        GLManager.GL.Enable(GLEnum.RescaleNormal);
        GLManager.GL.PushMatrix();
        GLManager.GL.Rotate(180.0F, 1.0F, 0.0F, 0.0F);
        Lighting.turnOn();
        GLManager.GL.PopMatrix();
        itemRenderer.drawItemIntoGui(FontRenderer, MC.textureManager, itemId, 0, Item.ITEMS[itemId].getTextureId(0), x + 2, y + 2);
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
        MC.textureManager.BindTexture(MC.textureManager.GetTextureId("/gui/slot.png"));
        Tessellator tessellator = Tessellator.instance;
        tessellator.startDrawingQuads();
        tessellator.addVertexWithUV(x + 0, y + 18, ZLevel, (double)((u + 0) * 0.0078125F), (double)((v + 18) * 0.0078125F));
        tessellator.addVertexWithUV(x + 18, y + 18, ZLevel, (double)((u + 18) * 0.0078125F), (double)((v + 18) * 0.0078125F));
        tessellator.addVertexWithUV(x + 18, y + 0, ZLevel, (double)((u + 18) * 0.0078125F), (double)((v + 0) * 0.0078125F));
        tessellator.addVertexWithUV(x + 0, y + 0, ZLevel, (double)((u + 0) * 0.0078125F), (double)((v + 0) * 0.0078125F));
        tessellator.draw();
    }

    public void drawTranslucentRect(int right, int bottom, int left, int top)
    {
        Gui.DrawGradientRect(right, bottom, left, top, 0xC0000000, 0xC0000000);
    }
}
