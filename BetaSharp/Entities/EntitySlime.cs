using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntitySlime : EntityLiving, Monster
{
    private readonly SyncedProperty<byte> _slimeSize;
    private int _slimeJumpDelay;
    public float PrevSquishAmount;
    public float SquishAmount;

    public EntitySlime(IWorldContext world) : base(world)
    {
        Texture = "/mob/slime.png";
        _slimeSize = DataSynchronizer.MakeProperty<byte>(16, 1);
        int size = 1 << Random.NextInt(3);
        StandingEyeHeight = 0.0F;
        _slimeJumpDelay = Random.NextInt(20) + 10;
        SlimeSize = size;
    }

    public override EntityType Type => EntityRegistry.Slime;

    protected override string? HurtSound => "mob.slime";

    protected override string? DeathSound => "mob.slime";

    protected override float SoundVolume => 0.6F;

    public int SlimeSize
    {
        get
        {
            return _slimeSize.Value;
           
        }
        set
        {
             _slimeSize.Value = (byte)value;
            SetBoundingBoxSpacing(0.6F * value, 0.6F * value);
            Health = value * value;
            SetPosition(X, Y, Z);
        }
    }

    protected override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        nbt.SetInteger("Size", SlimeSize - 1);
    }

    protected override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        SlimeSize = nbt.GetInteger("Size") + 1;
    }

    public override void Tick()
    {
        PrevSquishAmount = SquishAmount;
        bool wasOnGround = OnGround;
        base.Tick();
        if (OnGround && !wasOnGround)
        {
            int size = SlimeSize;

            for (int _ = 0; _ < size * 8; ++_)
            {
                float angle = Random.NextFloat() * (float)Math.PI * 2.0F;
                float spread = Random.NextFloat() * 0.5F + 0.5F;
                float offsetX = MathHelper.Sin(angle) * size * 0.5F * spread;
                float offsetY = MathHelper.Cos(angle) * size * 0.5F * spread;
                World.Broadcaster.AddParticle("slime", X + offsetX, BoundingBox.MinY, Z + offsetY, 0.0D, 0.0D, 0.0D);
            }

            if (size > 2)
            {
                World.Broadcaster.PlaySoundAtEntity(this, "mob.slime", SoundVolume, ((Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F) / 0.8F);
            }

            SquishAmount = -0.5F;
        }

        SquishAmount *= 0.6F;
    }

    protected override void TickLiving()
    {
        func_27021_X();
        EntityPlayer? player = World.Entities.GetClosestPlayerTarget(X, Y, Z, 16.0D);
        if (player != null)
        {
            faceEntity(player, 10.0F, 20.0F);
        }

        if (OnGround && _slimeJumpDelay-- <= 0)
        {
            _slimeJumpDelay = Random.NextInt(20) + 10;
            if (player != null)
            {
                _slimeJumpDelay /= 3;
            }

            Jumping = true;
            if (SlimeSize > 1)
            {
                World.Broadcaster.PlaySoundAtEntity(this, "mob.slime", SoundVolume, ((Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F) * 0.8F);
            }

            SquishAmount = 1.0F;
            SidewaysSpeed = 1.0F - Random.NextFloat() * 2.0F;
            ForwardSpeed = 1 * SlimeSize;
        }
        else
        {
            Jumping = false;
            if (OnGround)
            {
                SidewaysSpeed = ForwardSpeed = 0.0F;
            }
        }
    }

    public override void MarkDead()
    {
        int size = SlimeSize;
        if (!World.IsRemote && size > 1 && Health == 0)
        {
            for (int i = 0; i < 4; ++i)
            {
                float offsetX = (i % 2 - 0.5F) * size / 4.0F;
                float offsetY = (i * 0.5f - 0.5F) * size / 4.0F;
                EntitySlime slime = new(World);
                slime.SlimeSize = size / 2;
                slime.SetPositionAndAnglesKeepPrevAngles(X + offsetX, Y + 0.5D, Z + offsetY, Random.NextFloat() * 360.0F, 0.0F);
                World.SpawnEntity(slime);
            }
        }

        base.MarkDead();
    }

    public override void OnPlayerInteraction(EntityPlayer player)
    {
        int size = SlimeSize;
        if (size > 1 && CanSee(player) && GetDistance(player) < 0.6D * size && player.Damage(this, size))
        {
            World.Broadcaster.PlaySoundAtEntity(this, "mob.slimeattack", 1.0F, (Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F);
        }
    }

    protected override int DropItemId => SlimeSize == 1 ? Item.Slimeball.id : 0;

    public override bool CanSpawn()
    {
        Chunk chunk = World.ChunkHost.GetChunkFromPos(MathHelper.Floor(X), MathHelper.Floor(Z));
        return (SlimeSize == 1 || World.Difficulty > 0) && Random.NextInt(10) == 0 && chunk.GetSlimeRandom(987234911L).NextInt(10) == 0 && Y < 16.0D;
    }
}
