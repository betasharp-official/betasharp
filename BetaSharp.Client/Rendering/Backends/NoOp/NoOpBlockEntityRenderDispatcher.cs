using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Rendering.Blocks.Entities;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering.Backends;

internal sealed class NoOpBlockEntityRenderDispatcher : IBlockEntityRenderDispatcher
{
    private static readonly NoOpTextRenderer s_textRenderer = new();

    public double StaticPlayerX { get; set; }
    public double StaticPlayerY { get; set; }
    public double StaticPlayerZ { get; set; }
    public ITextureManager TextureManager => null!;
    public IEntityRenderDispatcher EntityDispatcher { get; set; } = new NoOpEntityRenderDispatcher();

    public void CacheActiveRenderInfo(World world, ITextureManager textureManager, ITextRenderer textRenderer,
        EntityLiving camera, float tickDelta)
    {
    }

    public void RenderTileEntity(BlockEntity blockEntity, float tickDelta)
    {
    }

    public void RenderTileEntityAt(BlockEntity blockEntity, double x, double y, double z, float tickDelta)
    {
    }

    public ITextRenderer GetFontRenderer() => s_textRenderer;
}
