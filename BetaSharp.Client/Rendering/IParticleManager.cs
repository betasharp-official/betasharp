using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering;

/// <summary>
/// Backend-neutral particle simulation and draw interface used by gameplay and renderer orchestration.
/// </summary>
public interface IParticleManager
{
    int ActiveParticleCount { get; }

    void updateEffects();
    void renderParticles(Entity camera, float partialTick);
    void renderSpecialParticles(Entity camera, float partialTick);
    void clearEffects(World world);

    void AddPickupParticle(Entity target, Entity collector, float yOffset,
        IEntityRenderDispatcher entityRenderDispatcher);

    void AddFootstep(double x, double y, double z);
    void AddSmoke(double x, double y, double z, double vx, double vy, double vz, float scaleMultiplier = 1.0f);
    void AddFlame(double x, double y, double z, double vx, double vy, double vz);
    void AddExplode(double x, double y, double z, double vx, double vy, double vz);
    void AddReddust(double x, double y, double z, float red, float green, float blue);
    void AddSnowShovel(double x, double y, double z, double vx, double vy, double vz);
    void AddHeart(double x, double y, double z, double vx, double vy, double vz);
    void AddNote(double x, double y, double z, double notePitch, double unused1, double unused2);
    void AddPortal(double x, double y, double z, double vx, double vy, double vz);
    void AddLava(double x, double y, double z);
    void AddRain(double x, double y, double z);
    void AddSplash(double x, double y, double z, double vx, double vy, double vz);
    void AddBubble(double x, double y, double z, double vx, double vy, double vz);

    void AddDigging(
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
        int blockZ);

    void AddDiggingScaled(
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
        float sizeScale);

    void AddSlime(double x, double y, double z, Item item);
    void addBlockDestroyEffects(int x, int y, int z, int blockId, int meta);
    void addBlockHitEffects(int blockX, int blockY, int blockZ, int face);
}
