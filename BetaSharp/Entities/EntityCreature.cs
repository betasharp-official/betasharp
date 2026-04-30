using BetaSharp.PathFinding;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public abstract class EntityCreature(IWorldContext world) : EntityLiving(world)
{
    private const float Range = 16.0F;
    private PathEntity? _pathToEntity;
    protected bool HasAttacked;
    public Entity? Target { get; set; }

    protected virtual bool IsMovementCeased => false;

    protected bool HasPath => _pathToEntity != null;

    protected override void TickLiving()
    {
        HasAttacked = IsMovementCeased;
        if (Target == null)
        {
            Target = FindPlayerToAttack();
            if (Target != null)
            {
                _pathToEntity = World.Pathing.findPath(this, Target, Range);
            }
        }
        else if (!Target.IsAlive)
        {
            Target = null;
        }
        else
        {
            float distance = Target.GetDistance(this);
            if (CanSee(Target))
            {
                attackEntity(Target, distance);
            }
            else
            {
                attackBlockedEntity(Target, distance);
            }
        }

        if (HasAttacked || Target == null || (_pathToEntity != null && Random.NextInt(20) != 0))
        {
            if (!HasAttacked && ((_pathToEntity == null && Random.NextInt(80) == 0) || Random.NextInt(80) == 0))
            {
                FindRandomWanderTarget();
            }
        }
        else
        {
            _pathToEntity = World.Pathing.findPath(this, Target, Range);
        }

        int floorY = MathHelper.Floor(BoundingBox.MinY + 0.5D);
        bool isInWater = base.IsInWater;
        bool isTouchingLava = IsTouchingLava;
        Pitch = 0.0F;
        if (_pathToEntity != null && Random.NextInt(100) != 0)
        {
            Vec3D? pos = _pathToEntity.GetPosition(this);
            double distance = Width * 2.0F;

            while (pos != null && pos.Value.squareDistanceTo(new Vec3D(X, pos.Value.y, Z)) < distance * distance)
            {
                _pathToEntity?.IncrementPathIndex();
                if (_pathToEntity is { IsFinished: true })
                {
                    pos = null;
                    _pathToEntity = null;
                }
                else
                {
                    pos = _pathToEntity?.GetPosition(this);
                }
            }

            Jumping = false;
            if (pos != null)
            {
                double dx = pos.Value.x - X;
                double dz = pos.Value.z - Z;
                double verticalOffset = pos.Value.y - floorY;
                float targetYaw = (float)(Math.Atan2(dz, dx) * 180.0D / (float)Math.PI) - 90.0F;
                float yawDelta = targetYaw - Yaw;

                for (ForwardSpeed = MovementSpeed; yawDelta < -180.0F; yawDelta += 360.0F)
                {
                }

                while (yawDelta >= 180.0F)
                {
                    yawDelta -= 360.0F;
                }

                if (yawDelta > 30.0F)
                {
                    yawDelta = 30.0F;
                }

                if (yawDelta < -30.0F)
                {
                    yawDelta = -30.0F;
                }

                Yaw += yawDelta;
                if (HasAttacked && Target != null)
                {
                    double targetDeltaX = Target.X - X;
                    double targetDeltaZ = Target.Z - Z;
                    float previousYaw = Yaw;
                    Yaw = (float)(Math.Atan2(targetDeltaZ, targetDeltaX) * 180.0D / (float)Math.PI) - 90.0F;
                    yawDelta = (previousYaw - Yaw + 90.0F) * (float)Math.PI / 180.0F;
                    SidewaysSpeed = -MathHelper.Sin(yawDelta) * ForwardSpeed * 1.0F;
                    ForwardSpeed = MathHelper.Cos(yawDelta) * ForwardSpeed * 1.0F;
                }

                if (verticalOffset > 0.0D)
                {
                    Jumping = true;
                }
            }

            if (Target != null)
            {
                faceEntity(Target, 30.0F, 30.0F);
            }

            if (HorizontalCollision && !HasPath)
            {
                Jumping = true;
            }

            if (Random.NextFloat() < 0.8F && (isInWater || isTouchingLava))
            {
                Jumping = true;
            }
        }
        else
        {
            base.TickLiving();
            _pathToEntity = null;
        }
    }

    private void FindRandomWanderTarget()
    {
        bool foundWanderTarget = false;
        BlockPos bestTile = new(-1, -1, -1);
        float bestCost = float.MinValue;

        for (int _ = 0; _ < 10; ++_)
        {
            BlockPos tile = new BlockPos(
                MathHelper.Floor(X + Random.NextInt(13) - 6.0D),
                MathHelper.Floor(Y + Random.NextInt(7) - 3.0D),
                MathHelper.Floor(Z + Random.NextInt(13) - 6.0D)
            );
            float cost = GetBlockPathWeight(tile.x, tile.y, tile.z);
            if (cost <= bestCost) continue;
            bestCost = cost;
            bestTile = tile;
            foundWanderTarget = true;
        }

        if (foundWanderTarget)
        {
            _pathToEntity = World.Pathing.findPath(this, bestTile.x, bestTile.y, bestTile.z, 10.0F);
        }
    }

    protected virtual void attackEntity(Entity entity, float distance)
    {
    }

    protected virtual void attackBlockedEntity(Entity entity, float distance)
    {
    }

    protected virtual float GetBlockPathWeight(int x, int y, int z) => 0.0F;

    protected virtual Entity? FindPlayerToAttack() => null;

    public override bool CanSpawn()
    {
        BlockPos tile = new BlockPos(MathHelper.Floor(X), MathHelper.Floor(BoundingBox.MinY), MathHelper.Floor(Z));
        return base.CanSpawn() && GetBlockPathWeight(tile.x, tile.y, tile.z) >= 0.0F;
    }

    internal void setPathToEntity(PathEntity? pathToEntity) => _pathToEntity = pathToEntity;
}
