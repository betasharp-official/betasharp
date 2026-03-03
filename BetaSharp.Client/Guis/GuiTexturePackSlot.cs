using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Resource.Pack;
using java.util;

namespace BetaSharp.Client.Guis;

public class GuiTexturePackSlot : GuiSlot
{
    public readonly GuiTexturePacks _parentTexturePackGui;


    public GuiTexturePackSlot(GuiTexturePacks parent)
        : base(parent.Game, parent.Width, parent.Height, 32, parent.Height - 55 + 4, 36)
    {
        _parentTexturePackGui = parent;
    }

    public override int GetSize()
    {
        return _parentTexturePackGui.Game.texturePackList.AvailableTexturePacks.Count;
    }
    protected override void ElementClicked(int index, bool doubleClick)
    {
        var packs = _parentTexturePackGui.Game.texturePackList.AvailableTexturePacks;
        var selectedPack = packs[index];

        _parentTexturePackGui.Game.texturePackList.setTexturePack(selectedPack);
        _parentTexturePackGui.Game.textureManager.Reload();
    }

    protected override bool IsSelected(int index)
    {
        var packs = _parentTexturePackGui.Game.texturePackList.AvailableTexturePacks;
        return _parentTexturePackGui.Game.texturePackList.SelectedTexturePack == packs[index];
    }

    protected override int GetContentHeight()
    {
        return GetSize() * 36;
    }

    protected override void DrawBackground()
    {
        _parentTexturePackGui.DrawDefaultBackground();
    }

    protected override void DrawSlot(int index, int x, int y, int slotHeight, Tessellator tess)
    {
        var pack = _parentTexturePackGui.Game.texturePackList.AvailableTexturePacks[index];
        pack.BindThumbnailTexture(_parentTexturePackGui.Game);

        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);

        tess.startDrawingQuads();
        tess.setColorOpaque_I(0xFFFFFF);
        tess.addVertexWithUV(x, y + slotHeight, 0.0D, 0.0D, 1.0D);
        tess.addVertexWithUV(x + 32, y + slotHeight, 0.0D, 1.0D, 1.0D);
        tess.addVertexWithUV(x + 32, y, 0.0D, 1.0D, 0.0D);
        tess.addVertexWithUV(x, y, 0.0D, 0.0D, 0.0D);
        tess.draw();

        Gui.DrawString(_parentTexturePackGui.FontRenderer, pack.TexturePackFileName, x + 32 + 2, y + 1, Color.White);
        Gui.DrawString(_parentTexturePackGui.FontRenderer, pack.FirstDescriptionLine, x + 32 + 2, y + 12,  Color.Gray80);
        Gui.DrawString(_parentTexturePackGui.FontRenderer, pack.SecondDescriptionLine, x + 32 + 2, y + 12 + 10, Color.Gray80);
    }
}
