using BetaSharp.Blocks;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Entities;

public class EntitySheep : EntityAnimal
{
    public static readonly float[][] fleeceColorTable = [[1.0F, 1.0F, 1.0F], [0.95F, 0.7F, 0.2F], [0.9F, 0.5F, 0.85F], [0.6F, 0.7F, 0.95F], [0.9F, 0.9F, 0.2F], [0.5F, 0.8F, 0.1F], [0.95F, 0.7F, 0.8F], [0.3F, 0.3F, 0.3F], [0.6F, 0.6F, 0.6F], [0.3F, 0.6F, 0.7F], [0.7F, 0.4F, 0.9F], [0.2F, 0.4F, 0.8F], [0.5F, 0.4F, 0.3F], [0.4F, 0.5F, 0.2F], [0.8F, 0.3F, 0.3F], [0.1F, 0.1F, 0.1F]];

    public EntitySheep(World world) : base(world)
    {
        texture = "/mob/sheep.png";
        setBoundingBoxSpacing(0.9F, 1.3F);
    }

    public override void PostSpawn()
    {
        setFleeceColor(getRandomFleeceColor(world.random));
    }

    protected override void initDataTracker()
    {
        base.initDataTracker();
        dataWatcher.AddObject(16, (byte)0);
    }

    protected override void dropFewItems()
    {
        if (!getSheared())
        {
            dropItem(new ItemStack(Block.Wool.id, 1, getFleeceColor()), 0.0F);
        }

    }

    protected override int getDropItemId()
    {
        return Block.Wool.id;
    }

    public override bool interact(EntityPlayer player)
    {
        ItemStack heldItem = player.inventory.getSelectedItem();
        if (heldItem != null && heldItem.itemId == Item.Shears.id && !getSheared())
        {
            if (!world.IsRemote)
            {
                setSheared(true);
                int woolCount = 2 + random.NextInt(3);

                for (int i = 0; i < woolCount; ++i)
                {
                    EntityItem woolItem = dropItem(new ItemStack(Block.Wool.id, 1, getFleeceColor()), 1.0F);
                    woolItem.velocityY += (double)(random.NextFloat() * 0.05F);
                    woolItem.velocityX += (double)((random.NextFloat() - random.NextFloat()) * 0.1F);
                    woolItem.velocityZ += (double)((random.NextFloat() - random.NextFloat()) * 0.1F);
                }
            }

            heldItem.damageItem(1, player);
        }

        return false;
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
        nbt.SetBoolean("Sheared", getSheared());
        nbt.SetByte("Color", (sbyte)getFleeceColor());
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
        setSheared(nbt.GetBoolean("Sheared"));
        setFleeceColor(nbt.GetByte("Color"));
    }

    protected override string getLivingSound()
    {
        return "mob.sheep";
    }

    protected override string getHurtSound()
    {
        return "mob.sheep";
    }

    protected override string getDeathSound()
    {
        return "mob.sheep";
    }

    public int getFleeceColor()
    {
        return dataWatcher.getWatchableObjectByte(16) & 15;
    }

    public void setFleeceColor(int color)
    {
        sbyte packedData = dataWatcher.getWatchableObjectByte(16);
        dataWatcher.UpdateObject(16, ((byte)(packedData & 240 | color & 15)));
    }

    public bool getSheared()
    {
        return (dataWatcher.getWatchableObjectByte(16) & 16) != 0;
    }

    public void setSheared(bool sheared)
    {
        sbyte packedData = dataWatcher.getWatchableObjectByte(16);
        if (sheared)
        {
            dataWatcher.UpdateObject(16,((byte)(packedData | 16)));
        }
        else
        {
            dataWatcher.UpdateObject(16,((byte)(packedData & -17)));
        }

    }

    public static int getRandomFleeceColor(JavaRandom random) // TODO: Use WeightedRandomSelector
    {
        int roll = random.NextInt(100);
        return roll < 5 ? 15 : (roll < 10 ? 7 : (roll < 15 ? 8 : (roll < 18 ? 12 : (random.NextInt(500) == 0 ? 6 : 0))));
    }
}
