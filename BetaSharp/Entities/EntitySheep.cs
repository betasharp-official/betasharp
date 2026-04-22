using BetaSharp.Blocks;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntitySheep : EntityAnimal
{
    public override EntityType Type => EntityRegistry.Sheep;
    public static readonly float[][] fleeceColorTable = [[1.0F, 1.0F, 1.0F], [0.95F, 0.7F, 0.2F], [0.9F, 0.5F, 0.85F], [0.6F, 0.7F, 0.95F], [0.9F, 0.9F, 0.2F], [0.5F, 0.8F, 0.1F], [0.95F, 0.7F, 0.8F], [0.3F, 0.3F, 0.3F], [0.6F, 0.6F, 0.6F], [0.3F, 0.6F, 0.7F], [0.7F, 0.4F, 0.9F], [0.2F, 0.4F, 0.8F], [0.5F, 0.4F, 0.3F], [0.4F, 0.5F, 0.2F], [0.8F, 0.3F, 0.3F], [0.1F, 0.1F, 0.1F]];

    public readonly SyncedProperty<byte> SheepData;

    public EntitySheep(IWorldContext world) : base(world)
    {
        Texture = "/mob/sheep.png";
        SetBoundingBoxSpacing(0.9F, 1.3F);
        SheepData = DataSynchronizer.MakeProperty<byte>(16, 0);
    }

    public override void PostSpawn()
    {
        setFleeceColor(getRandomFleeceColor(World.Random));
    }

    protected override void dropFewItems()
    {
        if (!getSheared())
        {
            DropItem(new ItemStack(Block.Wool.id, 1, getFleeceColor()), 0.0F);
        }

    }

    protected override int getDropItemId()
    {
        return Block.Wool.id;
    }

    public override bool Interact(EntityPlayer player)
    {
        ItemStack heldItem = player.inventory.GetItemInHand();
        if (heldItem != null && heldItem.ItemId == Item.Shears.id && !getSheared())
        {
            if (!World.IsRemote)
            {
                setSheared(true);
                int woolCount = 2 + Random.NextInt(3);

                for (int i = 0; i < woolCount; ++i)
                {
                    EntityItem woolItem = DropItem(new ItemStack(Block.Wool.id, 1, getFleeceColor()), 1.0F);
                    woolItem.VelocityY += (double)(Random.NextFloat() * 0.05F);
                    woolItem.VelocityX += (double)((Random.NextFloat() - Random.NextFloat()) * 0.1F);
                    woolItem.VelocityZ += (double)((Random.NextFloat() - Random.NextFloat()) * 0.1F);
                }
            }

            heldItem.DamageItem(1, player);
        }

        return false;
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        nbt.SetBoolean("Sheared", getSheared());
        nbt.SetByte("Color", (sbyte)getFleeceColor());
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
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
        return SheepData.Value & 15;
    }

    public void setFleeceColor(int color)
    {
        byte packedData = SheepData.Value;
        SheepData.Value = (byte)(packedData & 240 | color & 15);
    }

    public bool getSheared()
    {
        return (SheepData.Value & 16) != 0;
    }

    public void setSheared(bool sheared)
    {
        byte packedData = SheepData.Value;
        if (sheared)
        {
            SheepData.Value = (byte)(packedData | 16);
        }
        else
        {
            SheepData.Value = (byte)(packedData & -17);
        }

    }

    public static int getRandomFleeceColor(JavaRandom random) // TODO: Use WeightedRandomSelector
    {
        int roll = random.NextInt(100);
        return roll < 5 ? 15 : (roll < 10 ? 7 : (roll < 15 ? 8 : (roll < 18 ? 12 : (random.NextInt(500) == 0 ? 6 : 0))));
    }
}
