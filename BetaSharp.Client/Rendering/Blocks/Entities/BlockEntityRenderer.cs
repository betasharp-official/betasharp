using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Entities;
using BetaSharp.Worlds;

namespace BetaSharp.Client.Rendering.Blocks.Entities;

public class BlockEntityRenderer
{
    private readonly Dictionary<Type, BlockEntitySpecialRenderer?> _specialRendererMap = [];
    public static BlockEntityRenderer Instance { get; } = new();
    private TextRenderer _fontRenderer;
    public static double StaticPlayerX;
    public static double StaticPlayerY;
    public static double StaticPlayerZ;
    public TextureManager TextureManager { get; set; }
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

    public void CacheActiveRenderInfo(World world, TextureManager textureManager, TextRenderer fontRenderer, EntityLiving player, float partialTicks)
    {
        if (World != world)
        {
            func_31072_a(world);
        }

        TextureManager = textureManager;
        PlayerEntity = player;
        _fontRenderer = fontRenderer;
        PlayerYaw = player.prevYaw + (player.yaw - player.prevYaw) * partialTicks;
        PlayerPitch = player.prevPitch + (player.pitch - player.prevPitch) * partialTicks;
        PlayerX = player.lastTickX + (player.x - player.lastTickX) * (double)partialTicks;
        PlayerY = player.lastTickY + (player.y - player.lastTickY) * (double)partialTicks;
        PlayerZ = player.lastTickZ + (player.z - player.lastTickZ) * (double)partialTicks;
    }

    public void RenderTileEntity(BlockEntity blockEntity, float partialTicks)
    {
        if (blockEntity.distanceFrom(PlayerX, PlayerY, PlayerZ) < 4096.0D)
        {
            float luminance = World.getLuminance(blockEntity.X, blockEntity.Y, blockEntity.Z);
            GLManager.GL.Color3(luminance, luminance, luminance);
            RenderTileEntityAt(blockEntity, blockEntity.X - StaticPlayerX, blockEntity.Y - StaticPlayerY, blockEntity.Z - StaticPlayerZ, partialTicks);
        }

    }

    public void RenderTileEntityAt(BlockEntity blockEntity, double x, double y, double z, float partialTicks)
    {
        BlockEntitySpecialRenderer? renderer = GetSpecialRendererForEntity(blockEntity);
        renderer?.renderTileEntityAt(blockEntity, x, y, z, partialTicks);

    }

    public void func_31072_a(World world)
    {
        World = world;
        foreach (BlockEntitySpecialRenderer? renderer in _specialRendererMap.Values)
        {
            renderer?.func_31069_a(world);
        }
    }

    public TextRenderer GetFontRenderer()
    {
        return _fontRenderer;
    }
}
