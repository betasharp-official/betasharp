using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering.Backends;

internal sealed class NoOpParticleManager : IParticleManager
{
    public int ActiveParticleCount => 0;

    public void updateEffects()
    {
    }

    public void renderParticles(Entity camera, float partialTick)
    {
    }

    public void renderSpecialParticles(Entity camera, float partialTick)
    {
    }

    public void clearEffects(World world)
    {
    }

    public void AddPickupParticle(Entity target, Entity collector, float yOffset,
        IEntityRenderDispatcher entityRenderDispatcher)
    {
    }

    public void AddFootstep(double x, double y, double z)
    {
    }

    public void AddSmoke(double x, double y, double z, double vx, double vy, double vz, float scaleMultiplier = 1.0f)
    {
    }

    public void AddFlame(double x, double y, double z, double vx, double vy, double vz)
    {
    }

    public void AddExplode(double x, double y, double z, double vx, double vy, double vz)
    {
    }

    public void AddReddust(double x, double y, double z, float red, float green, float blue)
    {
    }

    public void AddSnowShovel(double x, double y, double z, double vx, double vy, double vz)
    {
    }

    public void AddHeart(double x, double y, double z, double vx, double vy, double vz)
    {
    }

    public void AddNote(double x, double y, double z, double notePitch, double unused1, double unused2)
    {
    }

    public void AddPortal(double x, double y, double z, double vx, double vy, double vz)
    {
    }

    public void AddLava(double x, double y, double z)
    {
    }

    public void AddRain(double x, double y, double z)
    {
    }

    public void AddSplash(double x, double y, double z, double vx, double vy, double vz)
    {
    }

    public void AddBubble(double x, double y, double z, double vx, double vy, double vz)
    {
    }

    public void AddDigging(
        double x,
        double y,
        double z,
        double vx,
        double vy,
        double vz,
        Block block,
        int hitFace,
        int meta,
        int blockX,
        int blockY,
        int blockZ)
    {
    }

    public void AddDiggingScaled(
        double x,
        double y,
        double z,
        Block block,
        int hitFace,
        int meta,
        int blockX,
        int blockY,
        int blockZ,
        float velScale,
        float sizeScale)
    {
    }

    public void AddSlime(double x, double y, double z, Item item)
    {
    }

    public void addBlockDestroyEffects(int x, int y, int z, int blockId, int meta)
    {
    }

    public void addBlockHitEffects(int blockX, int blockY, int blockZ, int face)
    {
    }
}
