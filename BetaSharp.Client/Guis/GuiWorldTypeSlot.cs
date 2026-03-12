using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Worlds;

namespace BetaSharp.Client.Guis;

public class GuiWorldTypeSlot(GuiSelectWorldType parent) : GuiSlot(parent.Game, parent.Width, parent.Height, 32, parent.Height - 64, 36)
{
    private readonly List<WorldType> _types = [.. WorldType.WorldTypes.Where(t => t != null && t.CanBeCreated)];

    public List<WorldType> GetTypes() => _types;

    public override int GetSize() => _types.Count;

    protected override void ElementClicked(int slotIndex, bool doubleClick)
    {
        parent.SelectedTypeIndex = slotIndex;
        if (doubleClick)
        {
            parent.DoneClicked();
        }
    }

    protected override bool IsSelected(int slotIndex)
    {
        return slotIndex == parent.SelectedTypeIndex;
    }

    protected override int GetContentHeight()
    {
        return GetSize() * 36;
    }

    protected override void DrawBackground()
    {
        parent.DrawDefaultBackground();
    }

    protected override void DrawSlot(int slotIndex, int x, int y, int slotHeight, Tessellator tessellator)
    {
        WorldType type = _types[slotIndex];

        const int iconSize = 32;
        int iconX = x;
        int iconY = y;

        Tessellator tess = Tessellator.instance;
        GLManager.GL.Disable(GLEnum.Texture2D);
        tess.startDrawingQuads();
        tess.setColorOpaque_I(0x202020);
        tess.addVertex(iconX, iconY + iconSize, 0);
        tess.addVertex(iconX + iconSize, iconY + iconSize, 0);
        tess.addVertex(iconX + iconSize, iconY, 0);
        tess.addVertex(iconX, iconY, 0);
        tess.draw();
        GLManager.GL.Enable(GLEnum.Texture2D);

        if (!string.IsNullOrEmpty(type.IconPath))
        {
            TextureHandle textureHandle = parent.Game.textureManager.GetTextureId(type.IconPath);
            if (textureHandle != null)
            {
                parent.Game.textureManager.BindTexture(textureHandle);
                textureHandle.Bind();
                GLManager.GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
                tess.startDrawingQuads();
                tess.addVertexWithUV(iconX, iconY + iconSize, 0, 0, 1);
                tess.addVertexWithUV(iconX + iconSize, iconY + iconSize, 0, 1, 1);
                tess.addVertexWithUV(iconX + iconSize, iconY, 0, 1, 0);
                tess.addVertexWithUV(iconX, iconY, 0, 0, 0);
                tess.draw();
            }
        }

        int textX = iconX + iconSize + 6;
        Gui.DrawString(parent.FontRenderer, type.DisplayName, textX, iconY + 2, Color.White);

        int maxX = parent.Width / 2 + 110;
        int textWidth = maxX - textX;

        parent.FontRenderer.DrawStringWrapped(type.Description, textX, iconY + 14, textWidth, Color.Gray80);
    }
}
