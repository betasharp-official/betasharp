using BetaSharp.Entities;
using BetaSharp.Items;

namespace BetaSharp.Client.Rendering.Items;

/// <summary>
/// Backend-facing contract for first-person held-item rendering and overlays.
/// OpenGL keeps the current implementation, while non-OpenGL backends can supply no-op or native implementations.
/// </summary>
public interface IHeldItemRenderer
{
    void renderItem(EntityLiving entity, ItemStack item);
    void renderItemInFirstPerson(float tickDelta);
    void renderOverlays(float tickDelta);
    void updateEquippedItem();
    void ResetEquippedProgress();
}
