using BetaSharp.Items;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityCow : EntityAnimal
{
    public EntityCow(IWorldContext world) : base(world)
    {
        Texture = "/mob/cow.png";
        SetBoundingBoxSpacing(0.9F, 1.3F);
    }

    public override EntityType Type => EntityRegistry.Cow;

    protected override string? LivingSound => "mob.cow";

    protected override string? HurtSound => "mob.cowhurt";

    protected override string? DeathSound => "mob.cowhurt";

    protected override float SoundVolume => 0.4F;

    protected override int DropItemId => Item.Leather.id;

    public override bool Interact(EntityPlayer player)
    {
        ItemStack? heldBucket = player.Inventory.ItemInHand;
        if (heldBucket == null || heldBucket.ItemId != Item.Bucket.id) return false;
        player.Inventory.SetStack(player.Inventory.SelectedSlot, new ItemStack(Item.MilkBucket));
        return true;
    }
}
