using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;

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

    public void CacheActiveRenderInfo(World world, TextureManager textureManager, TextRenderer fontRenderer, EntityLiving player, float tickDelta)
    {
        if (World != world)
        {
            func_31072_a(world);
        }

        TextureManager = textureManager;
        PlayerEntity = player;
        _fontRenderer = fontRenderer;
        PlayerYaw = player.PrevYaw + (player.Yaw - player.PrevYaw) * tickDelta;
        PlayerPitch = player.PrevPitch + (player.Pitch - player.PrevPitch) * tickDelta;
        PlayerX = player.LastTickX + (player.X - player.LastTickX) * (double)tickDelta;
        PlayerY = player.LastTickY + (player.Y - player.LastTickY) * (double)tickDelta;
        PlayerZ = player.LastTickZ + (player.Z - player.LastTickZ) * (double)tickDelta;
    }

    public void RenderTileEntity(BlockEntity blockEntity, float tickDelta)
    {
        if (blockEntity.distanceFrom(PlayerX, PlayerY, PlayerZ) < 4096.0D)
        {
            float brightness = World.GetLuminance(blockEntity.X, blockEntity.Y, blockEntity.Z);
            GLManager.GL.Color3(brightness, brightness, brightness);
            RenderTileEntityAt(blockEntity, blockEntity.X - StaticPlayerX, blockEntity.Y - StaticPlayerY, blockEntity.Z - StaticPlayerZ, tickDelta);
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

    public TextRenderer GetFontRenderer()
    {
        return _fontRenderer;
    }
}
