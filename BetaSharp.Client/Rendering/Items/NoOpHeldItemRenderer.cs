using BetaSharp.Entities;
using BetaSharp.Items;

namespace BetaSharp.Client.Rendering.Items;

internal sealed class NoOpHeldItemRenderer : IHeldItemRenderer
{
    public void renderItem(EntityLiving entity, ItemStack item)
    {
    }

    public void renderItemInFirstPerson(float tickDelta)
    {
    }

    public void renderOverlays(float tickDelta)
    {
    }

    public void updateEquippedItem()
    {
    }

    public void ResetEquippedProgress()
    {
    }
}
