using BetaSharp.Blocks;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntitySheep : EntityAnimal
{
    public static readonly float[][] FleeceColorTable =
    [
        [1.0F, 1.0F, 1.0F], [0.95F, 0.7F, 0.2F], [0.9F, 0.5F, 0.85F], [0.6F, 0.7F, 0.95F], [0.9F, 0.9F, 0.2F], [0.5F, 0.8F, 0.1F], [0.95F, 0.7F, 0.8F], [0.3F, 0.3F, 0.3F], [0.6F, 0.6F, 0.6F], [0.3F, 0.6F, 0.7F], [0.7F, 0.4F, 0.9F],
        [0.2F, 0.4F, 0.8F], [0.5F, 0.4F, 0.3F], [0.4F, 0.5F, 0.2F], [0.8F, 0.3F, 0.3F], [0.1F, 0.1F, 0.1F]
    ];

    private readonly SyncedProperty<byte> _sheepData;

    public EntitySheep(IWorldContext world) : base(world)
    {
        Texture = "/mob/sheep.png";
        SetBoundingBoxSpacing(0.9F, 1.3F);
        _sheepData = DataSynchronizer.MakeProperty<byte>(16, 0);
    }

    public override EntityType Type => EntityRegistry.Sheep;

    protected override string? LivingSound => "mob.sheep";

    protected override string? HurtSound => "mob.sheep";

    protected override string? DeathSound => "mob.sheep";

    public int FleeceColor
    {
        get => _sheepData.Value & 15;
        set => _sheepData.Value = (byte)((_sheepData.Value & 0xF0) | (value & 0x0F));
    }


    public bool IsSheared
    {
        get => (_sheepData.Value & 16) != 0;
        private set
        {
            if (value)
            {
                _sheepData.Value |= 16;
            }
            else
            {
                _sheepData.Value &= unchecked((byte)~16);
            }
        }
    }

    protected sealed override void SetBoundingBoxSpacing(float widthOffset, float heightOffset) => base.SetBoundingBoxSpacing(widthOffset, heightOffset);

    public override void PostSpawn() => FleeceColor = GetRandomFleeceColor(World.Random);

    protected override void DropFewItems()
    {
        if (!IsSheared)
        {
            DropItem(new ItemStack(Block.Wool.id, 1, FleeceColor), 0.0F);
        }
    }

    protected override int DropItemId => Block.Wool.id;

    public override bool Interact(EntityPlayer player)
    {
        ItemStack? heldItem = player.Inventory.ItemInHand;
        if (heldItem == null || heldItem.ItemId != Item.Shears.id || IsSheared) return false;

        if (!World.IsRemote)
        {
            IsSheared = true;
            int woolCount = 2 + Random.NextInt(3);

            for (int i = 0; i < woolCount; ++i)
            {
                EntityItem woolItem = DropItem(new ItemStack(Block.Wool.id, 1, FleeceColor), 1.0F);
                woolItem.VelocityY += Random.NextFloat() * 0.05F;
                woolItem.VelocityX += (Random.NextFloat() - Random.NextFloat()) * 0.1F;
                woolItem.VelocityZ += (Random.NextFloat() - Random.NextFloat()) * 0.1F;
            }
        }

        heldItem.DamageItem(1, player);

        return false;
    }

    protected override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        nbt.SetBoolean("Sheared", IsSheared);
        nbt.SetByte("Color", (sbyte)FleeceColor);
    }

    protected override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        IsSheared = nbt.GetBoolean("Sheared");
        FleeceColor = nbt.GetByte("Color");
    }


    private static int GetRandomFleeceColor(JavaRandom random)
    {
        int roll = random.NextInt(100);

        return roll switch
        {
            < 5 => 15, // White
            < 10 => 7, // Gray
            < 15 => 8, // Silver
            < 18 => 12, // Brown
            _ => random.NextInt(500) == 0 ? 6 : 0 // Pink (rare) or White
        };
    }
}
