using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Worlds;

namespace BetaSharp.Client.Rendering.Blocks.Entities;

public abstract class BlockEntitySpecialRenderer
{
    protected BlockEntityRenderer tileEntityRenderer;

    public abstract void renderTileEntityAt(BlockEntity blockEntity, double x, double y, double z, float tickDelta);

    protected void bindTextureByName(string var1)
    {
        TextureManager var2 = tileEntityRenderer.TextureManager;
        var2.BindTexture(var2.GetTextureId(var1));
    }

    public void setTileEntityRenderer(BlockEntityRenderer var1)
    {
        tileEntityRenderer = var1;
    }

    public virtual void func_31069_a(World var1)
    {
    }

    public TextRenderer getFontRenderer()
    {
        return tileEntityRenderer.GetFontRenderer();
    }
}
