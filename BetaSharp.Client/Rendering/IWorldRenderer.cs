using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Client.Rendering.Chunks;

namespace BetaSharp.Client.Rendering;

/// <summary>
/// Encapsulates world-space rendering operations used by the scene renderer.
/// </summary>
/// <remarks>
/// Scope:
/// - world draw entry points (sky, chunk passes, entities, particles overlays, clouds)
/// - world render-state inputs needed by scene orchestration (damage interpolation and fog forwarding)
/// - world/chunk visibility and metrics access used for diagnostics
/// </remarks>
public interface IWorldRenderer
{
    int CountEntitiesTotal { get; }
    int CountEntitiesRendered { get; }
    int CountEntitiesHidden { get; }
    float DamagePartialTime { get; set; }

    void ChangeWorld(World world);
    void Tick(Entity view, float partialTicks);
    void UpdateClouds();
    void RenderSky(float partialTicks);
    int SortAndRender(EntityLiving entity, int pass, double partialTicks, ICuller culler);
    void RenderEntities(Vec3D position, ICuller culler, float partialTicks);
    void DrawBlockBreaking(EntityPlayer entityPlayer, HitResult hit, ItemStack itemStack, float partialTicks);
    void DrawSelectionBox(EntityPlayer entityPlayer, HitResult hit, int stage, ItemStack itemStack, float partialTicks);
    void RenderClouds(float partialTicks);

    bool TryGetChunkStats(out ChunkRendererStats stats);
    void MarkAllVisibleChunksDirty();
    void SetChunkFogColor(float red, float green, float blue, float alpha);
    void SetChunkFogMode(int mode);
    void SetChunkFogDensity(float density);
    void SetChunkFogStart(float start);
    void SetChunkFogEnd(float end);
}
