using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityChicken : EntityAnimal
{
    public override EntityType Type => EntityRegistry.Chicken;
    public bool field_753_a = false;
    public float field_752_b;
    public float destPos;
    public float field_757_d;
    public float field_756_e;
    public float field_755_h = 1.0F;
    public int timeUntilNextEgg;

    public EntityChicken(IWorldContext world) : base(world)
    {
        texture = "/mob/chicken.png";
        setBoundingBoxSpacing(0.3F, 0.4F);
        health = 4;
        timeUntilNextEgg = random.NextInt(6000) + 6000;
    }

    public override void tickMovement()
    {
        base.tickMovement();
        if (world.IsRemote)
        {
            onGround = System.Math.Abs(y - prevY) < 0.02D;
        }
        field_756_e = field_752_b;
        field_757_d = destPos;
        destPos = (float)((double)destPos + (double)(onGround ? -1 : 4) * 0.3D);
        if (destPos < 0.0F)
        {
            destPos = 0.0F;
        }

        if (destPos > 1.0F)
        {
            destPos = 1.0F;
        }

        if (!onGround && field_755_h < 1.0F)
        {
            field_755_h = 1.0F;
        }

        field_755_h = (float)((double)field_755_h * 0.9D);
        if (!onGround && velocityY < 0.0D)
        {
            velocityY *= 0.6D;
        }

        field_752_b += field_755_h * 2.0F;
        if (!world.IsRemote && --timeUntilNextEgg <= 0)
        {
            world.Broadcaster.PlaySoundAtEntity(this, "mob.chickenplop", 1.0F, (random.NextFloat() - random.NextFloat()) * 0.2F + 1.0F);
            dropItem(Item.Egg.id, 1);
            timeUntilNextEgg = random.NextInt(6000) + 6000;
        }

    }

    protected override void onLanding(float fallDistance)
    {
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
    }

    protected override string getLivingSound()
    {
        return "mob.chicken";
    }

    protected override string getHurtSound()
    {
        return "mob.chickenhurt";
    }

    protected override string getDeathSound()
    {
        return "mob.chickenhurt";
    }

    protected override int getDropItemId()
    {
        return Item.Feather.id;
    }
}
