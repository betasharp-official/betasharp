using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Particles;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering;

public class ParticleManager
{
    protected World worldObj;
    // Layer 0: Standard, Layer 1: Terrain/Digging (mipmapped), Layer 2: Overlays/Items
    private readonly ParticleBuffer[] _layers = new ParticleBuffer[3];
    private readonly List<ISpecialParticle> _specialParticles = [];
    private readonly TextureManager _textureManager;
    private readonly JavaRandom _rand = new();
    // Temp storage for sub-particles spawned during the update loop (avoids buffer mutation)
    private readonly List<ParticleUpdater.DeferredSmoke> _deferredSmoke = [];

    public ParticleManager(World world, TextureManager textureManager)
    {
        worldObj = world;
        _textureManager = textureManager;

        for (int i = 0; i < 3; i++)
        {
            _layers[i] = new ParticleBuffer();
        }
    }

    public void updateEffects()
    {
        foreach (ParticleBuffer layer in _layers)
        {
            _deferredSmoke.Clear();
            ParticleUpdater.Update(layer, worldObj, _deferredSmoke);

            // Lava particles spawn smoke sub-particles on certain ticks
            foreach (ParticleUpdater.DeferredSmoke s in _deferredSmoke)
            {
                AddSmoke(s.X, s.Y, s.Z, s.VelX, s.VelY, s.VelZ);
            }
        }

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
            camera.Yaw, camera.Pitch,
            camera.X, camera.Y, camera.Z,
            camera.LastTickX, camera.LastTickY, camera.LastTickZ,
            partialTick, _textureManager, worldObj);
    }

    public void renderSpecialParticles(Entity camera, float partialTick)
    {
        ParticleRenderer.RenderSpecial(_specialParticles,
            camera.X, camera.Y, camera.Z,
            camera.LastTickX, camera.LastTickY, camera.LastTickZ,
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

    public int ActiveParticleCount => _layers[0].Count + _layers[1].Count + _layers[2].Count;

    public void AddSpecialParticle(ISpecialParticle particle)
    {
        _specialParticles.Add(particle);
    }

    // --- Factory methods (replicates the legacy EntityFX constructor behaviors) ---

    public void AddSmoke(double x, double y, double z, double vx, double vy, double vz, float scaleMultiplier = 1.0f)
    {
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);
        double velX = bvx * 0.1 + vx;
        double velY = bvy * 0.1 + vy;
        double velZ = bvz * 0.1 + vz;

        float color = _rand.NextFloat() * 0.3f;
        float baseScale = RandomBaseScale() * (12.0f / 16.0f) * scaleMultiplier;
        int maxAge = (int)(8.0 / (_rand.NextFloat() * 0.8 + 0.2));
        maxAge = (int)(maxAge * scaleMultiplier);

        ParticleType type = scaleMultiplier > 1.5f ? ParticleType.LargeSmoke : ParticleType.Smoke;
        _layers[0].Add(type, x, y, z, velX, velY, velZ,
            color, color, color, baseScale, 0, 7,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
    }

    public void AddFlame(double x, double y, double z, double vx, double vy, double vz)
    {
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);
        double velX = bvx * 0.01 + vx;
        double velY = bvy * 0.01 + vy;
        double velZ = bvz * 0.01 + vz;

        float baseScale = RandomBaseScale();
        int maxAge = (int)(8.0 / (_rand.NextFloat() * 0.8 + 0.2)) + 4;

        _layers[0].Add(ParticleType.Flame, x, y, z, velX, velY, velZ,
            1.0f, 1.0f, 1.0f, baseScale, 0, 48,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
    }

    public void AddExplode(double x, double y, double z, double vx, double vy, double vz)
    {
        double velX = vx + (_rand.NextDouble() * 2.0 - 1.0) * 0.05;
        double velY = vy + (_rand.NextDouble() * 2.0 - 1.0) * 0.05;
        double velZ = vz + (_rand.NextDouble() * 2.0 - 1.0) * 0.05;

        float jrnd = _rand.NextFloat();
        float color = jrnd * 0.3f + 0.7f;
        float scale = jrnd * _rand.NextFloat() * 6.0f + 1.0f;
        int maxAge = (int)(16.0 / (_rand.NextFloat() * 0.8 + 0.2)) + 2;

        _layers[0].Add(ParticleType.Explode, x, y, z, velX, velY, velZ,
            color, color, color, scale, 0, 7,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
    }

    public void AddReddust(double x, double y, double z, float red, float green, float blue)
    {
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);
        double velX = bvx * 0.1;
        double velY = bvy * 0.1;
        double velZ = bvz * 0.1;

        if (red == 0.0f)
        {
            red = 1.0f;
        }

        float colorVariation = _rand.NextFloat() * 0.4f + 0.6f;
        float r = (_rand.NextFloat() * 0.2f + 0.8f) * red * colorVariation;
        float g = (_rand.NextFloat() * 0.2f + 0.8f) * green * colorVariation;
        float b = (_rand.NextFloat() * 0.2f + 0.8f) * blue * colorVariation;

        float baseScale = RandomBaseScale() * (12.0f / 16.0f);
        int maxAge = (int)(8.0 / (_rand.NextFloat() * 0.8 + 0.2));

        _layers[0].Add(ParticleType.Reddust, x, y, z, velX, velY, velZ,
            r, g, b, baseScale, 0, 7,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
    }

    public void AddSnowShovel(double x, double y, double z, double vx, double vy, double vz)
    {
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz, vx, vy, vz);
        double velX = bvx * 0.1 + vx;
        double velY = bvy * 0.1 + vy;
        double velZ = bvz * 0.1 + vz;

        float color = 1.0f - _rand.NextFloat() * 0.3f;
        float baseScale = RandomBaseScale() * (12.0f / 16.0f);
        int maxAge = (int)(8.0 / (_rand.NextFloat() * 0.8 + 0.2));

        _layers[0].Add(ParticleType.SnowShovel, x, y, z, velX, velY, velZ,
            color, color, color, baseScale, 0, 7,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
    }

    public void AddHeart(double x, double y, double z, double vx, double vy, double vz)
    {
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
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);
        double velX = bvx * 0.01;
        double velY = bvy * 0.01 + 0.2;
        double velZ = bvz * 0.01;

        float r = MathHelper.Sin(((float)notePitch + 0.0f) * MathF.PI * 2.0f) * 0.65f + 0.35f;
        float g = MathHelper.Sin(((float)notePitch + 1.0f / 3.0f) * MathF.PI * 2.0f) * 0.65f + 0.35f;
        float b = MathHelper.Sin(((float)notePitch + 2.0f / 3.0f) * MathF.PI * 2.0f) * 0.65f + 0.35f;

        float baseScale = RandomBaseScale() * (12.0f / 16.0f) * 2.0f;

        _layers[0].Add(ParticleType.Note, x, y, z, velX, velY, velZ,
            r, g, b, baseScale, 0, 64,
            RandomJitterX(), RandomJitterY(), 6);
    }

    public void AddPortal(double x, double y, double z, double vx, double vy, double vz)
    {
        float brightnessVar = _rand.NextFloat() * 0.6f + 0.4f;
        float baseScale = _rand.NextFloat() * 0.2f + 0.5f;
        float r = brightnessVar * 0.9f;
        float g = brightnessVar * 0.3f;
        float b = brightnessVar;
        int maxAge = (int)(_rand.NextDouble() * 10.0) + 40;
        int texIndex = (int)(_rand.NextDouble() * 8.0);

        int idx = _layers[0].Add(ParticleType.Portal, x, y, z, vx, vy, vz,
            r, g, b, baseScale, 0, texIndex,
            RandomJitterX(), RandomJitterY(), (short)maxAge);

        // Parametric physics calculate current position relative to this origin point
        _layers[0].SpawnX[idx] = x;
        _layers[0].SpawnY[idx] = y;
        _layers[0].SpawnZ[idx] = z;
    }

    public void AddLava(double x, double y, double z)
    {
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);
        double velX = bvx * 0.8;
        double velY = _rand.NextFloat() * 0.4f + 0.05f;
        double velZ = bvz * 0.8;

        float baseScale = RandomBaseScale() * (_rand.NextFloat() * 2.0f + 0.2f);
        int maxAge = (int)(16.0 / (_rand.NextDouble() * 0.8 + 0.2));

        _layers[0].Add(ParticleType.Lava, x, y, z, velX, velY, velZ,
            1.0f, 1.0f, 1.0f, baseScale, 0, 49,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
    }

    public void AddRain(double x, double y, double z)
    {
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);
        double velX = bvx * 0.3;
        double velY = _rand.NextFloat() * 0.2f + 0.1f;
        double velZ = bvz * 0.3;

        int texIndex = 19 + _rand.NextInt(4);
        int maxAge = (int)(8.0 / (_rand.NextDouble() * 0.8 + 0.2));

        _layers[0].Add(ParticleType.Rain, x, y, z, velX, velY, velZ,
            1.0f, 1.0f, 1.0f, RandomBaseScale(), 0.06f, texIndex,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
    }

    public void AddSplash(double x, double y, double z, double vx, double vy, double vz)
    {
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);
        double velX = bvx * 0.3;
        double velY = _rand.NextFloat() * 0.2f + 0.1f;
        double velZ = bvz * 0.3;

        int texIndex = 19 + _rand.NextInt(4) + 1;
        int maxAge = (int)(8.0 / (_rand.NextDouble() * 0.8 + 0.2));

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
        double velX = vx * 0.2 + (_rand.NextDouble() * 2.0 - 1.0) * 0.02;
        double velY = vy * 0.2 + (_rand.NextDouble() * 2.0 - 1.0) * 0.02;
        double velZ = vz * 0.2 + (_rand.NextDouble() * 2.0 - 1.0) * 0.02;

        float baseScale = RandomBaseScale() * (_rand.NextFloat() * 0.6f + 0.2f);
        int maxAge = (int)(8.0 / (_rand.NextDouble() * 0.8 + 0.2));

        _layers[0].Add(ParticleType.Bubble, x, y, z, velX, velY, velZ,
            1.0f, 1.0f, 1.0f, baseScale, 0, 32,
            RandomJitterX(), RandomJitterY(), (short)maxAge);
    }

    public void AddDigging(double x, double y, double z, double vx, double vy, double vz,
        Block block, int hitFace, int meta, int blockX, int blockY, int blockZ)
    {
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz, vx, vy, vz);

        int texIndex = block.GetTexture(hitFace.ToSide(), meta);
        float gravity = block.particleFallSpeedModifier;
        float r = 0.6f, g = 0.6f, b = 0.6f;
        float baseScale = RandomBaseScale() / 2.0f;

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
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);

        bvx *= velScale;
        bvy = (bvy - 0.1) * velScale + 0.1;
        bvz *= velScale;

        int texIndex = block.GetTexture(hitFace.ToSide(), meta);
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
        ApplyBaseVelocity(x, y, z, out double bvx, out double bvy, out double bvz);

        int texIndex = item.getTextureId(0);
        float baseScale = RandomBaseScale() / 2.0f;
        float gravity = Block.SnowBlock.particleFallSpeedModifier;

        _layers[2].Add(ParticleType.Slime, x, y, z, bvx, bvy, bvz,
            1.0f, 1.0f, 1.0f, baseScale, gravity, texIndex,
            RandomJitterX(), RandomJitterY(), (short)RandomBaseMaxAge());
    }

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
                    double px = x + (gridX + 0.5) / particlesPerAxis;
                    double py = y + (gridY + 0.5) / particlesPerAxis;
                    double pz = z + (gridZ + 0.5) / particlesPerAxis;

                    AddDigging(px, py, pz, px - x - 0.5, py - y - 0.5, pz - z - 0.5,
                        block, _rand.NextInt(6), meta, x, y, z);
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

            switch (face)
            {
                case 0: py = blockY + bb.MinY - margin; break;
                case 1: py = blockY + bb.MaxY + margin; break;
                case 2: pz = blockZ + bb.MinZ - margin; break;
                case 3: pz = blockZ + bb.MaxZ + margin; break;
                case 4: px = blockX + bb.MinX - margin; break;
                case 5: px = blockX + bb.MaxX + margin; break;
            }

            int meta = worldObj.Reader.GetBlockMeta(blockX, blockY, blockZ);
            AddDiggingScaled(px, py, pz, block, face, meta, blockX, blockY, blockZ, 0.2f, 0.6f);
        }
    }

    private void ApplyBaseVelocity(double x, double y, double z,
        out double velX, out double velY, out double velZ,
        double inputVx = 0, double inputVy = 0, double inputVz = 0)
    {
        // Replicates legacy EntityFX constructor logic: random noise + speed normalization
        velX = inputVx + (_rand.NextDouble() * 2.0 - 1.0) * 0.4;
        velY = inputVy + (_rand.NextDouble() * 2.0 - 1.0) * 0.4;
        velZ = inputVz + (_rand.NextDouble() * 2.0 - 1.0) * 0.4;

        float speed = MathHelper.Sqrt(velX * velX + velY * velY + velZ * velZ);
        float scale = (_rand.NextFloat() + _rand.NextFloat() + 1.0f) * 0.15f;

        velX = velX / speed * scale * 0.4f;
        velY = velY / speed * scale * 0.4f + 0.1; // Base upward drift
        velZ = velZ / speed * scale * 0.4f;
    }

    private float RandomBaseScale() => (_rand.NextFloat() * 0.5f + 0.5f) * 2.0f;
    private int RandomBaseMaxAge() => (int)(4.0f / (_rand.NextFloat() * 0.9f + 0.1f));
    private float RandomJitterX() => _rand.NextFloat() * 3.0f;
    private float RandomJitterY() => _rand.NextFloat() * 3.0f;
}
