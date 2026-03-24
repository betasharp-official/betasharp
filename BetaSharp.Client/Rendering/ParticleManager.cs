using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Particles;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Rendering;

public class ParticleManager
{
    protected World worldObj;
    private readonly ParticleBuffer[] _layers = new ParticleBuffer[3];
    private readonly List<ISpecialParticle> _specialParticles = new();
    private readonly TextureManager _textures;
    private readonly JavaRandom _rand = new();
    private readonly List<ParticleUpdater.DeferredSmoke> _deferredSmoke = new();

    public ParticleManager(World world, TextureManager textures)
    {
        if (world != null)
        {
            worldObj = world;
        }

        _textures = textures;

        for (int i = 0; i < 3; i++)
        {
            _layers[i] = new ParticleBuffer();
        }
    }

    public void updateEffects()
    {
        for (int i = 0; i < 3; i++)
        {
            _deferredSmoke.Clear();
            ParticleUpdater.Update(_layers[i], worldObj, _deferredSmoke);

            // Lava spawns smoke sub-particles
            for (int j = 0; j < _deferredSmoke.Count; j++)
            {
                ParticleUpdater.DeferredSmoke s = _deferredSmoke[j];
                AddSmoke(s.X, s.Y, s.Z, s.VelX, s.VelY, s.VelZ);
            }
        }

        // Tick special particles (layer 3)
        for (int i = _specialParticles.Count - 1; i >= 0; i--)
        {
            _specialParticles[i].Tick();
            if (_specialParticles[i].IsDead)
            {
                _specialParticles.RemoveAt(i);
            }
        }
    }

    public void renderParticles(Entity camera, float partialTick)
    {
        ParticleRenderer.Render(_layers,
            camera.yaw, camera.pitch,
            camera.x, camera.y, camera.z,
            camera.lastTickX, camera.lastTickY, camera.lastTickZ,
            partialTick, _textures, worldObj);
    }

    public void func_1187_b(Entity camera, float partialTick)
    {
        ParticleRenderer.RenderSpecial(_specialParticles,
            camera.x, camera.y, camera.z,
            camera.lastTickX, camera.lastTickY, camera.lastTickZ,
            partialTick);
    }

    public void clearEffects(World world)
    {
        worldObj = world;
        for (int i = 0; i < 3; i++)
        {
            _layers[i].Clear();
        }

        _specialParticles.Clear();
    }

    public string getStatistics()
    {
        return "" + (_layers[0].Count + _layers[1].Count + _layers[2].Count);
    }

    // --- Special particle support ---

    public void AddSpecialParticle(ISpecialParticle particle)
    {
        _specialParticles.Add(particle);
    }

    // --- Factory methods: each replicates the EntityFX subclass constructor logic ---

    public void AddSmoke(double x, double y, double z, double vx, double vy, double vz, float scaleMultiplier = 1.0f)
    {
        // EntitySmokeFX: base ctor with (0,0,0) velocity, then scale base vel * 0.1 + add provided vel
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);
        double velX = bvx * 0.1 + vx;
        double velY = bvy * 0.1 + vy;
        double velZ = bvz * 0.1 + vz;

        float color = (float)(Random.Shared.NextDouble() * 0.3);
        float baseScale = RandomBaseScale() * (12.0f / 16.0f) * scaleMultiplier;
        int maxAge = (int)(8.0 / (Random.Shared.NextDouble() * 0.8 + 0.2));
        maxAge = (int)(maxAge * scaleMultiplier);

        ParticleType type = scaleMultiplier > 1.5f ? ParticleType.LargeSmoke : ParticleType.Smoke;
        _layers[0].Add(type, x, y, z, velX, velY, velZ,
            color, color, color, baseScale, 0, 7,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
    }

    public void AddFlame(double x, double y, double z, double vx, double vy, double vz)
    {
        // EntityFlameFX: base vel * 0.01 + provided vel
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);
        double velX = bvx * 0.01 + vx;
        double velY = bvy * 0.01 + vy;
        double velZ = bvz * 0.01 + vz;

        float baseScale = RandomBaseScale();
        int maxAge = (int)(8.0 / (Random.Shared.NextDouble() * 0.8 + 0.2)) + 4;

        _layers[0].Add(ParticleType.Flame, x, y, z, velX, velY, velZ,
            1.0f, 1.0f, 1.0f, baseScale, 0, 48,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
    }

    public void AddExplode(double x, double y, double z, double vx, double vy, double vz)
    {
        // EntityExplodeFX: provided vel + random jitter
        double velX = vx + (Random.Shared.NextDouble() * 2.0 - 1.0) * 0.05;
        double velY = vy + (Random.Shared.NextDouble() * 2.0 - 1.0) * 0.05;
        double velZ = vz + (Random.Shared.NextDouble() * 2.0 - 1.0) * 0.05;

        float jrnd = RandomJavaFloat();
        float color = jrnd * 0.3f + 0.7f;
        float scale = jrnd * RandomJavaFloat() * 6.0f + 1.0f;
        int maxAge = (int)(16.0 / (RandomJavaFloat() * 0.8 + 0.2)) + 2;

        _layers[0].Add(ParticleType.Explode, x, y, z, velX, velY, velZ,
            color, color, color, scale, 0, 7,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
    }

    public void AddReddust(double x, double y, double z, float red, float green, float blue)
    {
        // EntityReddustFX: base ctor with (0,0,0), vel *= 0.1, color with variation
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);
        double velX = bvx * 0.1;
        double velY = bvy * 0.1;
        double velZ = bvz * 0.1;

        if (red == 0.0f)
        {
            red = 1.0f;
        }
        float colorVariation = (float)Random.Shared.NextDouble() * 0.4f + 0.6f;
        float r = ((float)(Random.Shared.NextDouble() * 0.2) + 0.8f) * red * colorVariation;
        float g = ((float)(Random.Shared.NextDouble() * 0.2) + 0.8f) * green * colorVariation;
        float b = ((float)(Random.Shared.NextDouble() * 0.2) + 0.8f) * blue * colorVariation;

        float baseScale = RandomBaseScale() * (12.0f / 16.0f);
        int maxAge = (int)(8.0 / (Random.Shared.NextDouble() * 0.8 + 0.2));

        _layers[0].Add(ParticleType.Reddust, x, y, z, velX, velY, velZ,
            r, g, b, baseScale, 0, 7,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
    }

    public void AddSnowShovel(double x, double y, double z, double vx, double vy, double vz)
    {
        // EntitySnowShovelFX: base ctor with provided vel, then vel *= 0.1 + add provided again
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz, vx, vy, vz);
        double velX = bvx * 0.1 + vx;
        double velY = bvy * 0.1 + vy;
        double velZ = bvz * 0.1 + vz;

        float color = 1.0f - (float)(Random.Shared.NextDouble() * 0.3);
        float baseScale = RandomBaseScale() * (12.0f / 16.0f);
        int maxAge = (int)(8.0 / (Random.Shared.NextDouble() * 0.8 + 0.2));

        _layers[0].Add(ParticleType.SnowShovel, x, y, z, velX, velY, velZ,
            color, color, color, baseScale, 0, 7,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
    }

    public void AddHeart(double x, double y, double z, double vx, double vy, double vz)
    {
        // EntityHeartFX: base ctor with (0,0,0), vel *= 0.01, velocityY += 0.1
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);
        double velX = bvx * 0.01;
        double velY = bvy * 0.01 + 0.1;
        double velZ = bvz * 0.01;

        float baseScale = RandomBaseScale() * (12.0f / 16.0f) * 2.0f;

        _layers[0].Add(ParticleType.Heart, x, y, z, velX, velY, velZ,
            1.0f, 1.0f, 1.0f, baseScale, 0, 80,
            RandomJitterX(), RandomJitterY(), 16);
    }

    public void AddNote(double x, double y, double z, double notePitch, double _, double __)
    {
        // EntityNoteFX: base ctor with (0,0,0), vel *= 0.01, velocityY += 0.2, color from pitch
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);
        double velX = bvx * 0.01;
        double velY = bvy * 0.01 + 0.2;
        double velZ = bvz * 0.01;

        float r = MathHelper.Sin(((float)notePitch + 0.0f) * (float)Math.PI * 2.0f) * 0.65f + 0.35f;
        float g = MathHelper.Sin(((float)notePitch + 1.0f / 3.0f) * (float)Math.PI * 2.0f) * 0.65f + 0.35f;
        float b = MathHelper.Sin(((float)notePitch + 2.0f / 3.0f) * (float)Math.PI * 2.0f) * 0.65f + 0.35f;

        float baseScale = RandomBaseScale() * (12.0f / 16.0f) * 2.0f;

        _layers[0].Add(ParticleType.Note, x, y, z, velX, velY, velZ,
            r, g, b, baseScale, 0, 64,
            RandomJitterX(), RandomJitterY(), 6);
    }

    public void AddPortal(double x, double y, double z, double vx, double vy, double vz)
    {
        // EntityPortalFX: velocity set directly (no base randomization)
        float brightnessVar = RandomJavaFloat() * 0.6f + 0.4f;
        float baseScale = RandomJavaFloat() * 0.2f + 0.5f;
        float r = 1.0f * brightnessVar * 0.9f;
        float g = 1.0f * brightnessVar * 0.3f;
        float b = 1.0f * brightnessVar;
        int maxAge = (int)(Random.Shared.NextDouble() * 10.0) + 40;
        int texIndex = (int)(Random.Shared.NextDouble() * 8.0);

        int idx = _layers[0].Add(ParticleType.Portal, x, y, z, vx, vy, vz,
            r, g, b, baseScale, 0, texIndex,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
        _layers[0].SpawnX[idx] = x;
        _layers[0].SpawnY[idx] = y;
        _layers[0].SpawnZ[idx] = z;
    }

    public void AddLava(double x, double y, double z)
    {
        // EntityLavaFX: base ctor with (0,0,0), vel*=0.8, velY = random*0.4+0.05
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);
        double velX = bvx * 0.8;
        double velY = RandomJavaFloat() * 0.4f + 0.05f;
        double velZ = bvz * 0.8;

        float baseScale = RandomBaseScale() * (RandomJavaFloat() * 2.0f + 0.2f);
        // Note: original also does particleScale *= random*2+0.2 but baseScale already has the random factor
        int maxAge = (int)(16.0 / (Random.Shared.NextDouble() * 0.8 + 0.2));

        _layers[0].Add(ParticleType.Lava, x, y, z, velX, velY, velZ,
            1.0f, 1.0f, 1.0f, baseScale, 0, 49,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
    }

    public void AddRain(double x, double y, double z)
    {
        // EntityRainFX: base ctor with (0,0,0), vel*=0.3, velY = random*0.2+0.1
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);
        double velX = bvx * 0.3;
        double velY = (float)Random.Shared.NextDouble() * 0.2f + 0.1f;
        double velZ = bvz * 0.3;

        int texIndex = 19 + _rand.NextInt(4);
        int maxAge = (int)(8.0 / (Random.Shared.NextDouble() * 0.8 + 0.2));

        _layers[0].Add(ParticleType.Rain, x, y, z, velX, velY, velZ,
            1.0f, 1.0f, 1.0f, RandomBaseScale(), 0.06f, texIndex,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
    }

    public void AddSplash(double x, double y, double z, double vx, double vy, double vz)
    {
        // EntitySplashFX extends EntityRainFX: same base, but gravity=0.04, texIndex+1
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);
        double velX = bvx * 0.3;
        double velY = (float)Random.Shared.NextDouble() * 0.2f + 0.1f;
        double velZ = bvz * 0.3;

        int texIndex = 19 + _rand.NextInt(4) + 1;
        int maxAge = (int)(8.0 / (Random.Shared.NextDouble() * 0.8 + 0.2));

        // Splash overrides: if vy==0 and (vx!=0 or vz!=0), use provided vel
        if (vy == 0.0 && (vx != 0.0 || vz != 0.0))
        {
            velX = vx;
            velY = vy + 0.1;
            velZ = vz;
        }

        _layers[0].Add(ParticleType.Splash, x, y, z, velX, velY, velZ,
            1.0f, 1.0f, 1.0f, RandomBaseScale(), 0.04f, texIndex,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
    }

    public void AddBubble(double x, double y, double z, double vx, double vy, double vz)
    {
        // EntityBubbleFX: vel = provided * 0.2 + random jitter
        double velX = vx * 0.2 + (Random.Shared.NextDouble() * 2.0 - 1.0) * 0.02;
        double velY = vy * 0.2 + (Random.Shared.NextDouble() * 2.0 - 1.0) * 0.02;
        double velZ = vz * 0.2 + (Random.Shared.NextDouble() * 2.0 - 1.0) * 0.02;

        float baseScale = RandomBaseScale() * (RandomJavaFloat() * 0.6f + 0.2f);
        int maxAge = (int)(8.0 / (Random.Shared.NextDouble() * 0.8 + 0.2));

        _layers[0].Add(ParticleType.Bubble, x, y, z, velX, velY, velZ,
            1.0f, 1.0f, 1.0f, baseScale, 0, 32,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
    }

    public void AddDigging(double x, double y, double z, double vx, double vy, double vz,
        Block block, int hitFace, int meta, int blockX, int blockY, int blockZ)
    {
        // EntityDiggingFX: standard base velocity randomization
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz, vx, vy, vz);

        int texIndex = block.getTexture(hitFace, meta);
        float gravity = block.particleFallSpeedModifier;
        float r = 0.6f, g = 0.6f, b = 0.6f;
        float baseScale = RandomBaseScale() / 2.0f;

        // Color multiplier (same as EntityDiggingFX.GetColorMultiplier)
        if (!(block == Block.GrassBlock && texIndex != 0))
        {
            int color = block.getColorMultiplier(worldObj.Reader, blockX, blockY, blockZ, meta);
            r *= (color >> 16 & 255) / 255.0f;
            g *= (color >> 8 & 255) / 255.0f;
            b *= (color & 255) / 255.0f;
        }

        _layers[1].Add(ParticleType.Digging, x, y, z, bvx, bvy, bvz,
            r, g, b, baseScale, gravity, texIndex,
            RandomJitterX(), RandomJitterY(), (short)RandomBaseMaxAge());
    }

    public void AddDiggingScaled(double x, double y, double z,
        Block block, int hitFace, int meta, int blockX, int blockY, int blockZ,
        float velScale, float sizeScale)
    {
        // Used by addBlockHitEffects: scaleVelocity(0.2) + scaleSize(0.6)
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);

        // scaleVelocity: velX *= scale, velY = (velY - 0.1) * scale + 0.1, velZ *= scale
        bvx *= velScale;
        bvy = (bvy - 0.1) * velScale + 0.1;
        bvz *= velScale;

        int texIndex = block.getTexture(hitFace, meta);
        float gravity = block.particleFallSpeedModifier;
        float r = 0.6f, g = 0.6f, b = 0.6f;
        float baseScale = RandomBaseScale() * sizeScale / 2.0f;

        if (!(block == Block.GrassBlock && texIndex != 0))
        {
            int color = block.getColorMultiplier(worldObj.Reader, blockX, blockY, blockZ, meta);
            r *= (color >> 16 & 255) / 255.0f;
            g *= (color >> 8 & 255) / 255.0f;
            b *= (color & 255) / 255.0f;
        }

        _layers[1].Add(ParticleType.Digging, x, y, z, bvx, bvy, bvz,
            r, g, b, baseScale, gravity, texIndex,
            RandomJitterX(), RandomJitterY(), (short)RandomBaseMaxAge());
    }

    public void AddSlime(double x, double y, double z, Item item)
    {
        // EntitySlimeFX: base ctor with (0,0,0)
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);

        int texIndex = item.getTextureId(0);
        float baseScale = RandomBaseScale() / 2.0f;
        float gravity = Block.SnowBlock.particleFallSpeedModifier;

        _layers[2].Add(ParticleType.Slime, x, y, z, bvx, bvy, bvz,
            1.0f, 1.0f, 1.0f, baseScale, gravity, texIndex,
            RandomJitterX(), RandomJitterY(), (short)RandomBaseMaxAge());
    }

    // --- Block destroy/hit effects (zero-alloc versions) ---

    public void addBlockDestroyEffects(int x, int y, int z, int blockId, int meta)
    {
        if (blockId == 0)
        {
            return;
        }

        Block block = Block.Blocks[blockId];
        byte particlesPerAxis = 4;

        for (int gridX = 0; gridX < particlesPerAxis; ++gridX)
        {
            for (int gridY = 0; gridY < particlesPerAxis; ++gridY)
            {
                for (int gridZ = 0; gridZ < particlesPerAxis; ++gridZ)
                {
                    double particleX = x + (gridX + 0.5) / particlesPerAxis;
                    double particleY = y + (gridY + 0.5) / particlesPerAxis;
                    double particleZ = z + (gridZ + 0.5) / particlesPerAxis;

                    int randomSide = _rand.NextInt(6);

                    double motionX = particleX - x - 0.5;
                    double motionY = particleY - y - 0.5;
                    double motionZ = particleZ - z - 0.5;

                    AddDigging(particleX, particleY, particleZ,
                        motionX, motionY, motionZ,
                        block, randomSide, meta, x, y, z);
                }
            }
        }
    }

    public void addBlockHitEffects(int blockX, int blockY, int blockZ, int face)
    {
        int blockId = worldObj.Reader.GetBlockId(blockX, blockY, blockZ);
        if (blockId != 0)
        {
            Block block = Block.Blocks[blockId];
            Box bb = block.BoundingBox;
            float margin = 0.1F;
            double px = blockX + _rand.NextDouble() * (bb.MaxX - bb.MinX - (margin * 2.0F)) + margin + bb.MinX;
            double py = blockY + _rand.NextDouble() * (bb.MaxY - bb.MinY - (margin * 2.0F)) + margin + bb.MinY;
            double pz = blockZ + _rand.NextDouble() * (bb.MaxZ - bb.MinZ - (margin * 2.0F)) + margin + bb.MinZ;
            if (face == 0)
            {
                py = blockY + bb.MinY - margin;
            }

            if (face == 1)
            {
                py = blockY + bb.MaxY + margin;
            }

            if (face == 2)
            {
                pz = blockZ + bb.MinZ - margin;
            }

            if (face == 3)
            {
                pz = blockZ + bb.MaxZ + margin;
            }

            if (face == 4)
            {
                px = blockX + bb.MinX - margin;
            }

            if (face == 5)
            {
                px = blockX + bb.MaxX + margin;
            }

            int meta = worldObj.Reader.GetBlockMeta(blockX, blockY, blockZ);
            AddDiggingScaled(px, py, pz, block, face, meta, blockX, blockY, blockZ, 0.2f, 0.6f);
        }
    }

    // --- Helpers to replicate EntityFX base constructor randomization ---

    private void ApplyBaseVelocity(double x, double y, double z,
        out double velX, out double velY, out double velZ,
        double inputVx = 0, double inputVy = 0, double inputVz = 0)
    {
        // Replicates EntityFX constructor: random velocity + normalize + scale
        velX = inputVx + (_rand.NextDouble() * 2.0 - 1.0) * 0.4;
        velY = inputVy + (_rand.NextDouble() * 2.0 - 1.0) * 0.4;
        velZ = inputVz + (_rand.NextDouble() * 2.0 - 1.0) * 0.4;
        float scale = (float)(_rand.NextDouble() + _rand.NextDouble() + 1.0) * 0.15f;
        float speed = MathHelper.Sqrt(velX * velX + velY * velY + velZ * velZ);
        velX = velX / speed * scale * 0.4f;
        velY = velY / speed * scale * 0.4f + 0.1;
        velZ = velZ / speed * scale * 0.4f;
    }

    private float RandomBaseScale()
    {
        // EntityFX: particleScale = (random.NextFloat() * 0.5F + 0.5F) * 2.0F
        return (RandomJavaFloat() * 0.5f + 0.5f) * 2.0f;
    }

    private int RandomBaseMaxAge()
    {
        // EntityFX: particleMaxAge = (int)(4.0F / (random.NextFloat() * 0.9F + 0.1F))
        return (int)(4.0f / (RandomJavaFloat() * 0.9f + 0.1f));
    }

    private float RandomJitterX() => _rand.NextFloat() * 3.0f;
    private float RandomJitterY() => _rand.NextFloat() * 3.0f;
    private float RandomJavaFloat() => _rand.NextFloat();
}
