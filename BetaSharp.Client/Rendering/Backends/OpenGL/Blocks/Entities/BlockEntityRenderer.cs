using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering.Blocks.Entities;

public class BlockEntityRenderer : IBlockEntityRenderDispatcher
{
    private readonly Dictionary<Type, BlockEntitySpecialRenderer?> _specialRendererMap = [];
    public static BlockEntityRenderer Instance { get; } = new();
    private ITextRenderer _fontRenderer;
    public double StaticPlayerX { get; set; }
    public double StaticPlayerY { get; set; }
    public double StaticPlayerZ { get; set; }
    public ITextureManager TextureManager { get; set; }
    public IEntityRenderDispatcher EntityDispatcher { get; set; } = EntityRenderDispatcher.Instance;
    public World World { get; set; }
    public EntityLiving PlayerEntity { get; set; }
    public float PlayerYaw { get; set; }
    public float PlayerPitch { get; set; }
    public double PlayerX { get; set; }
    public double PlayerY { get; set; }
    public double PlayerZ { get; set; }

    private BlockEntityRenderer()
    {
        _specialRendererMap.Add(typeof(BlockEntitySign), new BlockEntitySignRenderer());
        _specialRendererMap.Add(typeof(BlockEntityMobSpawner), new BlockEntityMobSpawnerRenderer());
        _specialRendererMap.Add(typeof(BlockEntityPiston), new BlockEntityRendererPiston());

        foreach (BlockEntitySpecialRenderer? renderer in _specialRendererMap.Values)
        {
            renderer!.setTileEntityRenderer(this);
        }
    }

    public BlockEntitySpecialRenderer? GetSpecialRendererForClass(Type t)
    {
        _specialRendererMap.TryGetValue(t, out BlockEntitySpecialRenderer? renderer);
        if (renderer == null && t != typeof(BlockEntity))
        {
            renderer = GetSpecialRendererForClass(t.BaseType);
            _specialRendererMap[t] = renderer;
        }

        return renderer;
    }

    public BlockEntitySpecialRenderer? GetSpecialRendererForEntity(BlockEntity? be)
    {
        return be == null ? null : GetSpecialRendererForClass(be.GetType());
    }

    public void CacheActiveRenderInfo(World world, ITextureManager textureManager, ITextRenderer textRenderer,
        EntityLiving camera, float tickDelta)
    {
        if (World != world)
        {
            func_31072_a(world);
        }

        TextureManager = textureManager;
        PlayerEntity = camera;
        _fontRenderer = textRenderer;
        PlayerYaw = camera.prevYaw + (camera.yaw - camera.prevYaw) * tickDelta;
        PlayerPitch = camera.prevPitch + (camera.pitch - camera.prevPitch) * tickDelta;
        PlayerX = camera.lastTickX + (camera.x - camera.lastTickX) * (double)tickDelta;
        PlayerY = camera.lastTickY + (camera.y - camera.lastTickY) * (double)tickDelta;
        PlayerZ = camera.lastTickZ + (camera.z - camera.lastTickZ) * (double)tickDelta;
    }

    public void RenderTileEntity(BlockEntity blockEntity, float tickDelta)
    {
        if (blockEntity.distanceFrom(PlayerX, PlayerY, PlayerZ) < 4096.0D)
        {
            float luminance = World.GetLuminance(blockEntity.X, blockEntity.Y, blockEntity.Z);
            GLManager.GL.Color3(luminance, luminance, luminance);
            RenderTileEntityAt(
                blockEntity,
                blockEntity.X - this.StaticPlayerX,
                blockEntity.Y - this.StaticPlayerY,
                blockEntity.Z - this.StaticPlayerZ,
                tickDelta);
        }
    }

    public void RenderTileEntityAt(BlockEntity blockEntity, double x, double y, double z, float tickDelta)
    {
        BlockEntitySpecialRenderer? renderer = GetSpecialRendererForEntity(blockEntity);
        renderer?.renderTileEntityAt(blockEntity, x, y, z, tickDelta);
    }

    public void func_31072_a(World world)
    {
        World = world;
        foreach (BlockEntitySpecialRenderer? renderer in _specialRendererMap.Values)
        {
            renderer?.func_31069_a(world);
        }
    }

    public ITextRenderer GetFontRenderer()
    {
        return _fontRenderer;
    }
}
