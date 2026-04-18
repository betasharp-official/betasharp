using BetaSharp.Items;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityGhast : EntityFlying, Monster
{
    private const double AttackRange = 64.0D;
    private readonly SyncedProperty<bool> _charging;
    private int _aggroCooldown;
    private int _courseChangeCooldown;
    private Entity? _targetedEntity;
    private double _waypointX;
    private double _waypointY;
    private double _waypointZ;
    public int AttackCounter;
    public int PrevAttackCounter;

    public EntityGhast(IWorldContext world) : base(world)
    {
        Texture = "/mob/ghast.png";
        SetBoundingBoxSpacing(4.0F, 4.0F);
        IsImmuneToFire = true;
        _charging = DataSynchronizer.MakeProperty(16, false);
    }

    public override EntityType Type => EntityRegistry.Ghast;

    protected override string? LivingSound => "mob.ghast.moan";

    protected override string? HurtSound => "mob.ghast.scream";

    protected override string? DeathSound => "mob.ghast.death";

    protected override float SoundVolume => 10.0F;

    public override int MaxSpawnedInChunk => 1;

    protected sealed override void SetBoundingBoxSpacing(float widthOffset, float heightOffset) => base.SetBoundingBoxSpacing(widthOffset, heightOffset);

    public override void Tick()
    {
        base.Tick();
        Texture = _charging.Value ? "/mob/ghast_fire.png" : "/mob/ghast.png";
    }

    protected override void TickLiving()
    {
        if (World is { IsRemote: false, Difficulty: 0 }) MarkDead();

        func_27021_X();
        PrevAttackCounter = AttackCounter;
        double dx1 = _waypointX - X;
        double dy1 = _waypointY - Y;
        double dz1 = _waypointZ - Z;
        double distance = MathHelper.Sqrt(dx1 * dx1 + dy1 * dy1 + dz1 * dz1);
        if (distance is < 1.0D or > 60.0D)
        {
            _waypointX = X + (Random.NextFloat() * 2.0F - 1.0F) * 16.0F;
            _waypointY = Y + (Random.NextFloat() * 2.0F - 1.0F) * 16.0F;
            _waypointZ = Z + (Random.NextFloat() * 2.0F - 1.0F) * 16.0F;
        }

        if (_courseChangeCooldown-- <= 0)
        {
            _courseChangeCooldown += Random.NextInt(5) + 2;
            if (isCourseTraversable(_waypointX, _waypointY, _waypointZ, distance))
            {
                VelocityX += dx1 / distance * 0.1D;
                VelocityY += dy1 / distance * 0.1D;
                VelocityZ += dz1 / distance * 0.1D;
            }
            else
            {
                _waypointX = X;
                _waypointY = Y;
                _waypointZ = Z;
            }
        }

        if (_targetedEntity is { Dead: true })
        {
            _targetedEntity = null;
        }

        if (_targetedEntity == null || _aggroCooldown-- <= 0)
        {
            _targetedEntity = World.Entities.GetClosestPlayerTarget(X, Y, Z, 100.0D);
            if (_targetedEntity != null)
            {
                _aggroCooldown = 20;
            }
        }


        if (_targetedEntity != null && _targetedEntity.GetSquaredDistance(this) < AttackRange * AttackRange)
        {
            double dx2 = _targetedEntity.X - X;
            double dy2 = _targetedEntity.BoundingBox.MinY + _targetedEntity.Height / 2.0F - (Y + Height / 2.0F);
            double dz2 = _targetedEntity.Z - Z;
            BodyYaw = Yaw = -(float)Math.Atan2(dx2, dz2) * 180.0F / (float)Math.PI;
            if (CanSee(_targetedEntity))
            {
                if (AttackCounter == 10)
                {
                    World.Broadcaster.PlaySoundAtEntity(this, "mob.ghast.charge", SoundVolume, (Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F);
                }

                ++AttackCounter;
                if (AttackCounter == 20)
                {
                    World.Broadcaster.PlaySoundAtEntity(this, "mob.ghast.fireball", SoundVolume, (Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F);
                    EntityFireball fireball = new(World, this, dx2, dy2, dz2);
                    const double spawnOffset = 4.0D;
                    Vec3D lookDir = GetLook(1.0F);
                    fireball.X = X + lookDir.x * spawnOffset;
                    fireball.Y = Y + Height / 2.0F + 0.5D;
                    fireball.Z = Z + lookDir.z * spawnOffset;
                    World.SpawnEntity(fireball);
                    AttackCounter = -40;
                }
            }
            else if (AttackCounter > 0)
            {
                --AttackCounter;
            }
        }
        else
        {
            BodyYaw = Yaw = -(float)Math.Atan2(VelocityX, VelocityZ) * 180.0F / (float)Math.PI;
            if (AttackCounter > 0)
            {
                --AttackCounter;
            }
        }

        if (!World.IsRemote)
        {
            _charging.Value = AttackCounter > 10;
        }
    }

    private bool isCourseTraversable(double targetX, double targety, double targetZ, double distance)
    {
        double stepX = (_waypointX - X) / distance;
        double stepY = (_waypointY - Y) / distance;
        double stepZ = (_waypointZ - Z) / distance;
        Box box = BoundingBox;

        for (int i = 1; i < distance; ++i)
        {
            box.Translate(stepX, stepY, stepZ);
            if (World.Entities.GetEntityCollisionsScratch(this, box).Count > 0)
            {
                return false;
            }
        }

        return true;
    }

    protected override int DropItemId => Item.Gunpowder.id;

    public override bool CanSpawn() => Random.NextInt(20) == 0 && base.CanSpawn() && World.Difficulty > 0;
}
