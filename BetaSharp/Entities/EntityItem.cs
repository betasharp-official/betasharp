using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityItem : Entity
{
    public readonly float BobPhase = System.Random.Shared.NextSingle() * (float)Math.PI * 2.0f;
    private int _health = 5;
    private int _itemAge;
    public int DelayBeforeCanPickup;
    public ItemStack Stack;

    public EntityItem(IWorldContext world, double x, double y, double z, ItemStack stack) : base(world)
    {
        SetBoundingBoxSpacing(0.25F, 0.25F);
        StandingEyeHeight = Height / 2.0F;
        SetPosition(x, y, z);
        Stack = stack;
        Yaw = System.Random.Shared.NextSingle() * 360.0f;
        VelocityX = System.Random.Shared.NextDouble() * 0.2f - 0.1f;
        VelocityY = 0.2F;
        VelocityZ = System.Random.Shared.NextDouble() * 0.2f - 0.1f;
    }


    public EntityItem(IWorldContext world) : base(world)
    {
        SetBoundingBoxSpacing(0.25F, 0.25F);
        StandingEyeHeight = Height / 2.0F;
    }

    public override EntityType Type => EntityRegistry.Item;

    protected sealed override void SetBoundingBoxSpacing(float widthOffset, float heightOffset) => base.SetBoundingBoxSpacing(widthOffset, heightOffset);

    protected override bool BypassesSteppingEffects() => false;


    public override void Tick()
    {
        base.Tick();
        if (DelayBeforeCanPickup > 0)
        {
            --DelayBeforeCanPickup;
        }

        PrevX = X;
        PrevY = Y;
        PrevZ = Z;
        VelocityY -= 0.04F;
        if (World.Reader.GetMaterial(MathHelper.Floor(X), MathHelper.Floor(Y), MathHelper.Floor(Z)) == Material.Lava)
        {
            VelocityY = 0.2F;
            VelocityX = (Random.NextFloat() - Random.NextFloat()) * 0.2F;
            VelocityZ = (Random.NextFloat() - Random.NextFloat()) * 0.2F;
            World.Broadcaster.PlaySoundAtEntity(this, "random.fizz", 0.4F, 2.0F + Random.NextFloat() * 0.4F);
        }

        PushOutOfBlocks(X, (BoundingBox.MinY + BoundingBox.MaxY) / 2.0D, Z);
        Move(VelocityX, VelocityY, VelocityZ);
        float friction = 0.98F;
        if (OnGround)
        {
            friction = 0.1F * 0.1F * 58.8F;
            int groundBlockId = World.Reader.GetBlockId(MathHelper.Floor(X), MathHelper.Floor(BoundingBox.MinY) - 1, MathHelper.Floor(Z));
            if (groundBlockId > 0)
            {
                friction = Block.Blocks[groundBlockId].Slipperiness * 0.98F;
            }
        }

        VelocityX *= friction;
        VelocityY *= 0.98F;
        VelocityZ *= friction;
        if (OnGround)
        {
            VelocityY *= -0.5D;
        }

        ++_itemAge;
        if (_itemAge >= 6000) MarkDead();
    }

    public override bool CheckWaterCollisions() => World.Reader.UpdateMovementInFluid(BoundingBox, Material.Water, this);

    protected override void Damage(int amount) => Damage(null, amount);

    public override bool Damage(Entity? entity, int amount)
    {
        ScheduleVelocityUpdate();
        _health -= amount;
        if (_health <= 0)
        {
            MarkDead();
        }

        return false;
    }

    protected override void WriteNbt(NBTTagCompound nbt)
    {
        nbt.SetShort("Health", (byte)_health);
        nbt.SetShort("Age", (short)_itemAge);
        nbt.SetCompoundTag("Item", Stack.writeToNBT(new NBTTagCompound()));
    }

    protected override void ReadNbt(NBTTagCompound nbt)
    {
        _health = nbt.GetShort("Health") & 255;
        _itemAge = nbt.GetShort("Age");
        NBTTagCompound itemTag = nbt.GetCompoundTag("Item");
        Stack = new ItemStack(itemTag);
    }

    public override void OnPlayerInteraction(EntityPlayer player)
    {
        if (World.IsRemote || !player.GameMode.CanPickup) return;
        if (DelayBeforeCanPickup != 0 || !player.Inventory.AddItemStackToInventory(Stack)) return;
        if (Stack.ItemId == Block.Log.id) player.IncrementStat(Achievements.MineWood);
        if (Stack.ItemId == Item.Leather.id) player.IncrementStat(Achievements.KillCow);

        World.Broadcaster.PlaySoundAtEntity(this, "random.pop", 0.2F, ((Random.NextFloat() - Random.NextFloat()) * 0.7F + 1.0F) * 2.0F);
        player.sendPickup(this, Stack.Count);
        if (Stack.Count <= 0)
        {
            MarkDead();
        }
    }
}
