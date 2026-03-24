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

    public void CacheActiveRenderInfo(World var1, TextureManager var2, TextRenderer var3, EntityLiving var4, float var5)
    {
        if (World != var1)
        {
            func_31072_a(var1);
        }

        TextureManager = var2;
        PlayerEntity = var4;
        _fontRenderer = var3;
        PlayerYaw = var4.prevYaw + (var4.yaw - var4.prevYaw) * var5;
        PlayerPitch = var4.prevPitch + (var4.pitch - var4.prevPitch) * var5;
        PlayerX = var4.lastTickX + (var4.x - var4.lastTickX) * (double)var5;
        PlayerY = var4.lastTickY + (var4.y - var4.lastTickY) * (double)var5;
        PlayerZ = var4.lastTickZ + (var4.z - var4.lastTickZ) * (double)var5;
    }

    public void RenderTileEntity(BlockEntity var1, float var2)
    {
        if (var1.distanceFrom(PlayerX, PlayerY, PlayerZ) < 4096.0D)
        {
            float var3 = World.GetLuminance(var1.X, var1.Y, var1.Z);
            GLManager.GL.Color3(var3, var3, var3);
            RenderTileEntityAt(var1, var1.X - StaticPlayerX, var1.Y - StaticPlayerY, var1.Z - StaticPlayerZ, var2);
        }

    }

    public void RenderTileEntityAt(BlockEntity var1, double var2, double var4, double var6, float var8)
    {
        BlockEntitySpecialRenderer? var9 = GetSpecialRendererForEntity(var1);
        var9?.renderTileEntityAt(var1, var2, var4, var6, var8);

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
