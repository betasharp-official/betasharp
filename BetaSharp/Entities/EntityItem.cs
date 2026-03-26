using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityItem : Entity
{
    public override EntityType Type => EntityRegistry.Item;
    public ItemStack stack;
    public int itemAge;
    public int delayBeforeCanPickup;
    private int health = 5;
    public float bobPhase = Random.Shared.NextSingle() * ((float)Math.PI) * 2.0f;

    public EntityItem(IWorldContext world, double x, double y, double z, ItemStack stack) : base(world)
    {
        setBoundingBoxSpacing(0.25F, 0.25F);
        standingEyeHeight = height / 2.0F;
        setPosition(x, y, z);
        this.stack = stack;
        yaw = Random.Shared.NextSingle() * 360.0f;
        velocityX = Random.Shared.NextDouble() * 0.2f - 0.1f;
        velocityY = 0.2F;
        velocityZ = Random.Shared.NextDouble() * 0.2f - 0.1f;
    }

    protected override bool bypassesSteppingEffects()
    {
        return false;
    }

    public EntityItem(IWorldContext world) : base(world)
    {
        setBoundingBoxSpacing(0.25F, 0.25F);
        standingEyeHeight = height / 2.0F;
    }


    public override void tick()
    {
        base.tick();
        if (delayBeforeCanPickup > 0)
        {
            --delayBeforeCanPickup;
        }

        prevX = x;
        prevY = y;
        prevZ = z;
        velocityY -= (double)0.04F;
        if (world.Reader.GetMaterial(MathHelper.Floor(x), MathHelper.Floor(y), MathHelper.Floor(z)) == Material.Lava)
        {
            velocityY = (double)0.2F;
            velocityX = (double)((random.NextFloat() - random.NextFloat()) * 0.2F);
            velocityZ = (double)((random.NextFloat() - random.NextFloat()) * 0.2F);
            world.Broadcaster.PlaySoundAtEntity(this, "random.fizz", 0.4F, 2.0F + random.NextFloat() * 0.4F);
        }

        pushOutOfBlocks(x, (boundingBox.MinY + boundingBox.MaxY) / 2.0D, z);
        move(velocityX, velocityY, velocityZ);
        float friction = 0.98F;
        if (onGround)
        {
            friction = 0.1F * 0.1F * 58.8F;
            int groundBlockId = world.Reader.GetBlockId(MathHelper.Floor(x), MathHelper.Floor(boundingBox.MinY) - 1, MathHelper.Floor(z));
            if (groundBlockId > 0)
            {
                friction = Block.Blocks[groundBlockId].slipperiness * 0.98F;
            }
        }

        velocityX *= (double)friction;
        velocityY *= (double)0.98F;
        velocityZ *= (double)friction;
        if (onGround)
        {
            velocityY *= -0.5D;
        }

        ++itemAge;
        if (itemAge >= 6000)
        {
            markDead();
        }

    }

    public override bool checkWaterCollisions()
    {
        return world.Reader.UpdateMovementInFluid(boundingBox, Material.Water, this);
    }

    protected override void damage(int amount)
    {
        damage((Entity)null, amount);
    }

    public override bool damage(Entity entity, int amount)
    {
        scheduleVelocityUpdate();
        health -= amount;
        if (health <= 0)
        {
            markDead();
        }

        return false;
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        nbt.SetShort("Health", (short)((byte)health));
        nbt.SetShort("Age", (short)itemAge);
        nbt.SetCompoundTag("Item", stack.writeToNBT(new NBTTagCompound()));
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        health = nbt.GetShort("Health") & 255;
        itemAge = nbt.GetShort("Age");
        NBTTagCompound itemTag = nbt.GetCompoundTag("Item");
        stack = new ItemStack(itemTag);
    }

    public override void onPlayerInteraction(EntityPlayer player)
    {
        if (!world.IsRemote)
        {
            int pickedUpCount = stack.count;
            if (delayBeforeCanPickup == 0 && player.inventory.addItemStackToInventory(stack))
            {
                if (stack.itemId == Block.Log.id)
                {
                    player.incrementStat(Achievements.MineWood);
                }

                if (stack.itemId == Item.Leather.id)
                {
                    player.incrementStat(Achievements.KillCow);
                }

                world.Broadcaster.PlaySoundAtEntity(this, "random.pop", 0.2F, ((random.NextFloat() - random.NextFloat()) * 0.7F + 1.0F) * 2.0F);
                player.sendPickup(this, pickedUpCount);
                if (stack.count <= 0)
                {
                    markDead();
                }
            }

        }
    }
}
