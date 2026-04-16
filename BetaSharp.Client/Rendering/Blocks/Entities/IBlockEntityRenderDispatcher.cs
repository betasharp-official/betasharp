using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering.Blocks.Entities;

/// <summary>
/// Backend-facing block-entity render dispatcher used by world and UI renderers.
/// </summary>
public interface IBlockEntityRenderDispatcher
{
    double StaticPlayerX { get; set; }
    double StaticPlayerY { get; set; }
    double StaticPlayerZ { get; set; }

    ITextureManager TextureManager { get; }
    IEntityRenderDispatcher EntityDispatcher { get; set; }

    void CacheActiveRenderInfo(World world, ITextureManager textureManager, ITextRenderer textRenderer,
        EntityLiving camera, float tickDelta);

    void RenderTileEntity(BlockEntity blockEntity, float tickDelta);
    void RenderTileEntityAt(BlockEntity blockEntity, double x, double y, double z, float tickDelta);
    ITextRenderer GetFontRenderer();
}
