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
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Rendering.Entities;

public class EntityRenderDispatcher
{
    private readonly Dictionary<Type, EntityRenderer> entityRenderMap = [];
    public static EntityRenderDispatcher instance = new();
    private TextRenderer fontRenderer;
    public static double offsetX;
    public static double offsetY;
    public static double offsetZ;
    public TextureManager textureManager;
    public SkinManager skinManager;
    public HeldItemRenderer heldItemRenderer;
    public World world;
    public EntityLiving cameraEntity;
    public float playerViewY;
    public float playerViewX;
    public GameOptions options;
    public double x;
    public double y;
    public double z;

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

        foreach (var render in entityRenderMap.Values)
        {
            render.Dispatcher = this;
        }
    }

    private void RegisterRenderer(Type type, EntityRenderer render)
    {
        entityRenderMap[type] = render;
    }

    public EntityRenderer GetEntityClassRenderObject(Type type)
    {
        if (!entityRenderMap.TryGetValue(type, out EntityRenderer? entityRenderer) && type != typeof(Entity))
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

    public void cacheActiveRenderInfo(World world, TextureManager textureManager, TextRenderer textRenderer, EntityLiving camera, GameOptions options, float tickDelta)
    {
        this.world = world;
        this.textureManager = textureManager;
        this.options = options;
        cameraEntity = camera;
        fontRenderer = textRenderer;
        if (camera.isSleeping())
        {
            int var7 = world.Reader.GetBlockId(MathHelper.Floor(camera.x), MathHelper.Floor(camera.y), MathHelper.Floor(camera.z));
            if (var7 == Block.Bed.id)
            {
                int var8 = world.Reader.GetBlockMeta(MathHelper.Floor(camera.x), MathHelper.Floor(camera.y), MathHelper.Floor(camera.z));
                int var9 = var8 & 3;
                playerViewY = var9 * 90 + 180;
                playerViewX = 0.0F;
            }
        }
        else
        {
            playerViewY = camera.prevYaw + (camera.yaw - camera.prevYaw) * tickDelta;
            playerViewX = camera.prevPitch + (camera.pitch - camera.prevPitch) * tickDelta;
        }

        x = camera.lastTickX + (camera.x - camera.lastTickX) * (double)tickDelta;
        y = camera.lastTickY + (camera.y - camera.lastTickY) * (double)tickDelta;
        z = camera.lastTickZ + (camera.z - camera.lastTickZ) * (double)tickDelta;
    }

    public void renderEntity(Entity target, float tickDelta)
    {
        double x = target.lastTickX + (target.x - target.lastTickX) * (double)tickDelta;
        double y = target.lastTickY + (target.y - target.lastTickY) * (double)tickDelta;
        double z = target.lastTickZ + (target.z - target.lastTickZ) * (double)tickDelta;
        float yaw = target.prevYaw + (target.yaw - target.prevYaw) * tickDelta;
        float brightness = target.getBrightnessAtEyes(tickDelta);
        GLManager.GL.Color3(brightness, brightness, brightness);
        renderEntityWithPosYaw(target, x - offsetX, y - offsetY, z - offsetZ, yaw, tickDelta);
    }

    public void renderEntityWithPosYaw(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        EntityRenderer entityRenderer = GetEntityRenderObject(target);
        if (entityRenderer == null) return;

        entityRenderer.render(target, x, y, z, yaw, tickDelta);
        entityRenderer.PostRender(target, new Vec3D(x, y, z), yaw, tickDelta);
        entityRenderer.RenderBoundingBox(target, new Vec3D(x, y, z), yaw, tickDelta);
    }

    public void SetWorld(World world)
    {
        this.world = world;
    }

    public double GetSquareDistanceTo(double x, double y, double z)
    {
        double xDelta = x - this.x;
        double yDelta = y - this.y;
        double zDelta = z - this.z;
        return xDelta * xDelta + yDelta * yDelta + zDelta * zDelta;
    }

    public TextRenderer getTextRenderer()
    {
        return fontRenderer;
    }
}
