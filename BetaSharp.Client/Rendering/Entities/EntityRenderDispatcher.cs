using BetaSharp.Blocks;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering.Entities;

public class EntityRenderDispatcher
{
    private readonly Dictionary<Type, EntityRenderer> _entityRenderMap = [];
    public static readonly EntityRenderDispatcher Instance = new();
    private TextRenderer _fontRenderer;
    public static double OffsetX { get; set; }
    public static double OffsetY { get; set; }
    public static double OffsetZ { get; set; }
    public TextureManager TextureManager { get; private set; }
    public SkinManager SkinManager { get; set; }
    public HeldItemRenderer HeldItemRenderer { get; set; }
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
        RegisterRenderer(typeof(EntitySheep), new SheepEntityRenderer(new SheepModel(), new SheepFurModel(), 0.7F));
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

    public void CacheRenderInfo(World world, TextureManager textureManager, TextRenderer textRenderer, EntityLiving camera, GameOptions options, float tickDelta)
    {
        World = world;
        TextureManager = textureManager;
        Options = options;
        CameraEntity = camera;
        _fontRenderer = textRenderer;
        if (camera.isSleeping())
        {
            int blockId = world.Reader.GetBlockId(MathHelper.Floor(camera.X), MathHelper.Floor(camera.Y), MathHelper.Floor(camera.Z));
            if (blockId == Block.Bed.id)
            {
                int bedMeta = world.Reader.GetBlockMeta(MathHelper.Floor(camera.X), MathHelper.Floor(camera.Y), MathHelper.Floor(camera.Z));
                int bedFacing = bedMeta & 3;
                PlayerViewY = bedFacing * 90 + 180;
                PlayerViewX = 0.0F;
            }
        }
        else
        {
            PlayerViewY = camera.PrevYaw + (camera.Yaw - camera.PrevYaw) * tickDelta;
            PlayerViewX = camera.PrevPitch + (camera.Pitch - camera.PrevPitch) * tickDelta;
        }

        _x = camera.LastTickX + (camera.X - camera.LastTickX) * (double)tickDelta;
        _y = camera.LastTickY + (camera.Y - camera.LastTickY) * (double)tickDelta;
        _z = camera.LastTickZ + (camera.Z - camera.LastTickZ) * (double)tickDelta;
    }

    public void RenderEntity(Entity target, float tickDelta)
    {
        double x = target.LastTickX + (target.X - target.LastTickX) * (double)tickDelta;
        double y = target.LastTickY + (target.Y - target.LastTickY) * (double)tickDelta;
        double z = target.LastTickZ + (target.Z - target.LastTickZ) * (double)tickDelta;
        float yaw = target.PrevYaw + (target.Yaw - target.PrevYaw) * tickDelta;
        float brightness = target.GetBrightnessAtEyes(tickDelta);
        GLManager.GL.Color3(brightness, brightness, brightness);
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

    public TextRenderer getTextRenderer()
    {
        return _fontRenderer;
    }
}
