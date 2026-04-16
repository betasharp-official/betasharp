using BetaSharp.Client.Rendering.Chunks;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering.Backends;

internal sealed class NoOpWorldRenderer : IWorldRenderer
{
    public int CountEntitiesTotal => 0;
    public int CountEntitiesRendered => 0;
    public int CountEntitiesHidden => 0;
    public float DamagePartialTime { get; set; }

    public void ChangeWorld(World world)
    {
    }

    public void Tick(Entity view, float partialTicks)
    {
    }

    public void UpdateClouds()
    {
    }

    public void RenderSky(float partialTicks)
    {
    }

    public int SortAndRender(EntityLiving entity, int pass, double partialTicks, ICuller culler)
    {
        return 0;
    }

    public void RenderEntities(Vec3D position, ICuller culler, float partialTicks)
    {
    }

    public void DrawBlockBreaking(EntityPlayer entityPlayer, HitResult hit, ItemStack? itemStack, float partialTicks)
    {
    }

    public void DrawSelectionBox(EntityPlayer entityPlayer, HitResult hit, int stage, ItemStack? itemStack,
        float partialTicks)
    {
    }

    public void RenderClouds(float partialTicks)
    {
    }

    public bool TryGetChunkStats(out ChunkRendererStats stats)
    {
        stats = default;
        return false;
    }

    public void MarkAllVisibleChunksDirty()
    {
    }

    public void SetChunkFogColor(float red, float green, float blue, float alpha)
    {
    }

    public void SetChunkFogMode(int mode)
    {
    }

    public void SetChunkFogDensity(float density)
    {
    }

    public void SetChunkFogStart(float start)
    {
    }

    public void SetChunkFogEnd(float end)
    {
    }
}
