using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Rendering.Blocks.Entities;

public abstract class BlockEntitySpecialRenderer
{
    protected BlockEntityRenderer tileEntityRenderer;

    public abstract void renderTileEntityAt(BlockEntity blockEntity, double x, double y, double z, float tickDelta);

    protected void bindTextureByName(string texturePath)
    {
        ITextureManager textureManager = tileEntityRenderer.TextureManager;
        textureManager.BindTexture(textureManager.GetTextureId(texturePath));
    }

    public void setTileEntityRenderer(BlockEntityRenderer renderer)
    {
        tileEntityRenderer = renderer;
    }

    public virtual void func_31069_a(World world)
    {
    }

    public ITextRenderer getFontRenderer()
    {
        return tileEntityRenderer.GetFontRenderer();
    }
}
