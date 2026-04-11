using BetaSharp.Blocks;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Backends;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering.Entities;

public class EntityRenderDispatcher : IEntityRenderDispatcher
{
    private readonly Dictionary<Type, EntityRenderer> _entityRenderMap = [];
    public static readonly EntityRenderDispatcher Instance = new();
    private ITextRenderer _fontRenderer;
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double OffsetZ { get; set; }
    public ITextureManager TextureManager { get; private set; }
    public ISkinManager SkinManager { get; set; }
    public IHeldItemRenderer HeldItemRenderer { get; set; } = new NoOpHeldItemRenderer();
    public ISceneRenderBackend SceneRenderBackend { get; private set; } = new NoOpSceneRenderBackend();
    public World World { get; set; }
    public EntityLiving CameraEntity { get; private set; }
    public float PlayerViewY { get; set; }
    public float PlayerViewX { get; private set; }
    public GameOptions Options { get; private set; }
    private double _x;
    private double _y;
    private double _z;

    private EntityRenderDispatcher()
    {
        RegisterRenderer(typeof(EntitySpider), new SpiderEntityRenderer());
        RegisterRenderer(typeof(EntityPig), new PigEntityRenderer(new ModelPig(), new ModelPig(0.5F), 0.7F));
        RegisterRenderer(typeof(EntitySheep), new SheepEntityRenderer(new ModelSheep2(), new ModelSheep1(), 0.7F));
        RegisterRenderer(typeof(EntityCow), new CowEntityRenderer(new ModelCow(), 0.7F));
        RegisterRenderer(typeof(EntityWolf), new WolfEntityRenderer(new ModelWolf(), 0.5F));
        RegisterRenderer(typeof(EntityChicken), new ChickenEntityRenderer(new ModelChicken(), 0.3F));
        RegisterRenderer(typeof(EntityCreeper), new CreeperEntityRenderer());
        RegisterRenderer(typeof(EntitySkeleton), new UndeadEntityRenderer(new ModelSkeleton(), 0.5F));
        RegisterRenderer(typeof(EntityZombie), new UndeadEntityRenderer(new ModelZombie(), 0.5F));
        RegisterRenderer(typeof(EntitySlime), new SlimeEntityRenderer(new ModelSlime(16), new ModelSlime(0), 0.25F));
        RegisterRenderer(typeof(EntityPlayer), new PlayerEntityRenderer());
        RegisterRenderer(typeof(EntityGiantZombie), new GiantEntityRenderer(new ModelZombie(), 0.5F, 6.0F));
        RegisterRenderer(typeof(EntityGhast), new GhastEntityRenderer());
        RegisterRenderer(typeof(EntitySquid), new SquidEntityRenderer(new ModelSquid(), 0.7F));
        RegisterRenderer(typeof(EntityLiving), new LivingEntityRenderer(new ModelBiped(), 0.5F));
        RegisterRenderer(typeof(Entity), new BoxEntityRenderer());
        RegisterRenderer(typeof(EntityPainting), new PaintingEntityRenderer());
        RegisterRenderer(typeof(EntityArrow), new ArrowEntityRenderer());
        RegisterRenderer(typeof(EntitySnowball), new ProjectileEntityRenderer(Item.Snowball.getTextureId(0)));
        RegisterRenderer(typeof(EntityEgg), new ProjectileEntityRenderer(Item.Egg.getTextureId(0)));
        RegisterRenderer(typeof(EntityFireball), new FireballEntityRenderer());
        RegisterRenderer(typeof(EntityItem), new ItemRenderer());
        RegisterRenderer(typeof(EntityTNTPrimed), new TntEntityRenderer());
        RegisterRenderer(typeof(EntityFallingSand), new FallingBlockEntityRenderer());
        RegisterRenderer(typeof(EntityMinecart), new MinecartEntityRenderer());
        RegisterRenderer(typeof(EntityBoat), new BoatEntityRenderer());
        RegisterRenderer(typeof(EntityFish), new FishingBobberEntityRenderer());
        RegisterRenderer(typeof(EntityLightningBolt), new LightningEntityRenderer());

        foreach (EntityRenderer render in _entityRenderMap.Values)
        {
            render.Dispatcher = this;
        }
    }

    private void RegisterRenderer(Type type, EntityRenderer render)
    {
        _entityRenderMap[type] = render;
    }

    public EntityRenderer GetEntityClassRenderObject(Type type)
    {
        if (!_entityRenderMap.TryGetValue(type, out EntityRenderer? entityRenderer) && type != typeof(Entity))
        {
            entityRenderer = GetEntityClassRenderObject(type.BaseType);
            RegisterRenderer(type, entityRenderer);
        }

        return entityRenderer;
    }

    public EntityRenderer GetEntityRenderObject(Entity entity)
    {
        return GetEntityClassRenderObject(entity.GetType());
    }

    public void CacheRenderInfo(World world, ITextureManager textureManager, ITextRenderer textRenderer, EntityLiving camera, GameOptions options, ISceneRenderBackend sceneRenderBackend, float tickDelta)
    {
        World = world;
        TextureManager = textureManager;
        Options = options;
        SceneRenderBackend = sceneRenderBackend;
        CameraEntity = camera;
        _fontRenderer = textRenderer;
        if (camera.isSleeping())
        {
            int blockId = world.Reader.GetBlockId(MathHelper.Floor(camera.x), MathHelper.Floor(camera.y), MathHelper.Floor(camera.z));
            if (blockId == Block.Bed.id)
            {
                int bedMetadata = world.Reader.GetBlockMeta(MathHelper.Floor(camera.x), MathHelper.Floor(camera.y), MathHelper.Floor(camera.z));
                int bedFacing = bedMetadata & 3;
                PlayerViewY = bedFacing * 90 + 180;
                PlayerViewX = 0.0F;
            }
        }
        else
        {
            PlayerViewY = camera.prevYaw + (camera.yaw - camera.prevYaw) * tickDelta;
            PlayerViewX = camera.prevPitch + (camera.pitch - camera.prevPitch) * tickDelta;
        }

        _x = camera.lastTickX + (camera.x - camera.lastTickX) * (double)tickDelta;
        _y = camera.lastTickY + (camera.y - camera.lastTickY) * (double)tickDelta;
        _z = camera.lastTickZ + (camera.z - camera.lastTickZ) * (double)tickDelta;
    }

    public void RenderEntity(Entity target, float tickDelta)
    {
        double x = target.lastTickX + (target.x - target.lastTickX) * (double)tickDelta;
        double y = target.lastTickY + (target.y - target.lastTickY) * (double)tickDelta;
        double z = target.lastTickZ + (target.z - target.lastTickZ) * (double)tickDelta;
        float yaw = target.prevYaw + (target.yaw - target.prevYaw) * tickDelta;
        float brightness = target.getBrightnessAtEyes(tickDelta);
        SceneRenderBackend.SetColorRgb(brightness, brightness, brightness);
        RenderEntityWithPosYaw(target, x - OffsetX, y - OffsetY, z - OffsetZ, yaw, tickDelta);
    }

    public void RenderEntityWithPosYaw(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        EntityRenderer entityRenderer = GetEntityRenderObject(target);
        if (entityRenderer == null) return;

        entityRenderer.Render(target, x, y, z, yaw, tickDelta);
        entityRenderer.PostRender(target, new Vec3D(x, y, z), yaw, tickDelta);
        entityRenderer.RenderBoundingBox(target, new Vec3D(x, y, z), yaw, tickDelta);
    }

    public double GetSquareDistanceTo(double x, double y, double z)
    {
        double xDelta = x - _x;
        double yDelta = y - _y;
        double zDelta = z - _z;
        return xDelta * xDelta + yDelta * yDelta + zDelta * zDelta;
    }

    public ITextRenderer GetTextRenderer()
    {
        return _fontRenderer;
    }

    public ITextRenderer getTextRenderer() => GetTextRenderer();
}
