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
        Texture = "/mob/chicken.png";
        SetBoundingBoxSpacing(0.3F, 0.4F);
        Health = 4;
        timeUntilNextEgg = Random.NextInt(6000) + 6000;
    }

    public override void tickMovement()
    {
        base.tickMovement();
        if (World.IsRemote)
        {
            OnGround = System.Math.Abs(Y - PrevY) < 0.02D;
        }
        prevFlapProgress = flapProgress;
        prevDestPos = destPos;
        destPos = (float)((double)destPos + (double)(OnGround ? -1 : 4) * 0.3D);
        if (destPos < 0.0F)
        {
            destPos = 0.0F;
        }

        if (destPos > 1.0F)
        {
            destPos = 1.0F;
        }

        if (!OnGround && flapSpeed < 1.0F)
        {
            flapSpeed = 1.0F;
        }

        flapSpeed = (float)((double)flapSpeed * 0.9D);
        if (!OnGround && VelocityY < 0.0D)
        {
            VelocityY *= 0.6D;
        }

        flapProgress += flapSpeed * 2.0F;
        if (!World.IsRemote && --timeUntilNextEgg <= 0)
        {
            World.Broadcaster.PlaySoundAtEntity(this, "mob.chickenplop", 1.0F, (Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F);
            DropItem(Item.Egg.id, 1);
            timeUntilNextEgg = Random.NextInt(6000) + 6000;
        }

    }

    protected override void OnLanding(float fallDistance)
    {
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
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
