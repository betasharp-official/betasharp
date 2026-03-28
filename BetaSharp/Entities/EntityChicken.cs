using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityChicken : EntityAnimal
{
    public override EntityType Type => EntityRegistry.Chicken;
    public bool jockey = false;
    public float flapProgress;
    public float destPos;
    public float prevDestPos;
    public float prevFlapProgress;
    public float flapSpeed = 1.0F;
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
        prevFlapProgress = flapProgress;
        prevDestPos = destPos;
        destPos = (float)((double)destPos + (double)(onGround ? -1 : 4) * 0.3D);
        if (destPos < 0.0F)
        {
            destPos = 0.0F;
        }

        if (destPos > 1.0F)
        {
            destPos = 1.0F;
        }

        if (!onGround && flapSpeed < 1.0F)
        {
            flapSpeed = 1.0F;
        }

        flapSpeed = (float)((double)flapSpeed * 0.9D);
        if (!onGround && velocityY < 0.0D)
        {
            velocityY *= 0.6D;
        }

        flapProgress += flapSpeed * 2.0F;
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
