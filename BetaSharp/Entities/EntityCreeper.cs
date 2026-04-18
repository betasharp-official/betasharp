using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityCreeper : EntityMonster
{
    private readonly SyncedProperty<byte> _creeperState;
    public readonly SyncedProperty<bool> Powered;
    private int _lastActiveTime;
    private int _timeSinceIgnited;

    public EntityCreeper(IWorldContext world) : base(world)
    {
        Texture = "/mob/creeper.png";
        _creeperState = DataSynchronizer.MakeProperty<byte>(16, 255); // -1
        Powered = DataSynchronizer.MakeProperty(17, false);
    }

    public override EntityType Type => EntityRegistry.Creeper;

    protected override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        if (Powered.Value)
        {
            nbt.SetBoolean("powered", true);
        }
    }

    protected override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        Powered.Value = nbt.GetBoolean("powered");
    }

    protected override void attackBlockedEntity(Entity entity, float distance)
    {
        if (World.IsRemote) return;
        if (_timeSinceIgnited <= 0) return;

        _creeperState.Value = 255;
        --_timeSinceIgnited;
        if (_timeSinceIgnited < 0)
        {
            _timeSinceIgnited = 0;
        }
    }

    public override void Tick()
    {
        _lastActiveTime = _timeSinceIgnited;
        if (World.IsRemote)
        {
            int state = (sbyte)_creeperState.Value;
            if (state > 0 && _timeSinceIgnited == 0)
            {
                World.Broadcaster.PlaySoundAtEntity(this, "random.fuse", 1.0F, 0.5F);
            }

            _timeSinceIgnited += state;
            if (_timeSinceIgnited < 0)
            {
                _timeSinceIgnited = 0;
            }

            if (_timeSinceIgnited >= 30)
            {
                _timeSinceIgnited = 30;
            }
        }

        base.Tick();
        if (World.IsRemote || Target != null || _timeSinceIgnited <= 0) return;

        _creeperState.Value = 255;
        --_timeSinceIgnited;
        if (_timeSinceIgnited < 0)
        {
            _timeSinceIgnited = 0;
        }
    }

    protected override string? HurtSound => "mob.creeper";

    protected override string? DeathSound => "mob.creeperdeath";

    protected override void OnKilledBy(Entity? entity)
    {
        base.OnKilledBy(entity);
        if (entity is EntitySkeleton)
        {
            DropItem(Item.RecordThirteen.id + Random.NextInt(2), 1);
        }
    }

    protected override void attackEntity(Entity entity, float distance)
    {
        if (World.IsRemote) return;

        int state = (sbyte)_creeperState.Value;
        if ((state <= 0 && distance < 3.0F) || (state > 0 && distance < 7.0F))
        {
            if (_timeSinceIgnited == 0)
            {
                World.Broadcaster.PlaySoundAtEntity(this, "random.fuse", 1.0F, 0.5F);
            }

            _creeperState.Value = 1;
            ++_timeSinceIgnited;
            if (_timeSinceIgnited >= 30)
            {
                World.CreateExplosion(this, X, Y, Z, Powered.Value ? 6.0F : 3.0F);
                MarkDead();
            }

            HasAttacked = true;
        }
        else
        {
            _creeperState.Value = 255;
            --_timeSinceIgnited;
            if (_timeSinceIgnited < 0)
            {
                _timeSinceIgnited = 0;
            }
        }
    }

    public float GetCreeperFlashTime(float partialTick) => (_lastActiveTime + (_timeSinceIgnited - _lastActiveTime) * partialTick) / 28.0F;

    protected override int DropItemId => Item.Gunpowder.id;

    public override void OnStruckByLightning(EntityLightningBolt bolt)
    {
        base.OnStruckByLightning(bolt);
        Powered.Value = true;
    }
}
