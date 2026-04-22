using BetaSharp.PathFinding;
using BetaSharp.Profiling;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public abstract class EntityCreature : EntityLiving
{
    private PathEntity pathToEntity;
    protected Entity? playerToAttack;
    protected bool hasAttacked;

    public EntityCreature(IWorldContext world) : base(world)
    {
    }

    protected virtual bool isMovementCeased()
    {
        return false;
    }

    public override void tickLiving()
    {
        hasAttacked = isMovementCeased();
        float range = 16.0F;
        if (playerToAttack == null)
        {
            playerToAttack = findPlayerToAttack();
            if (playerToAttack != null)
            {
                pathToEntity = World.Pathing.findPath(this, playerToAttack, range);
            }
        }
        else if (!playerToAttack.IsAlive())
        {
            playerToAttack = null;
        }
        else
        {
            float distance = playerToAttack.GetDistance(this);
            if (canSee(playerToAttack))
            {
                attackEntity(playerToAttack, distance);
            }
            else
            {
                attackBlockedEntity(playerToAttack, distance);
            }
        }

        if (hasAttacked || playerToAttack == null || pathToEntity != null && Random.NextInt(20) != 0)
        {
            if (!hasAttacked && (pathToEntity == null && Random.NextInt(80) == 0 || Random.NextInt(80) == 0))
            {
                findRandomWanderTarget();
            }
        }
        else
        {
            pathToEntity = World.Pathing.findPath(this, playerToAttack, range);
        }

        int floorY = MathHelper.Floor(BoundingBox.MinY + 0.5D);
        bool isInWater = base.IsInWater();
        bool isTouchingLava = base.IsTouchingLava();
        Pitch = 0.0F;
        if (pathToEntity != null && Random.NextInt(100) != 0)
        {
            Vec3D? pos = pathToEntity.GetPosition(this);
            double distance = (double)(Width * 2.0F);

            while (pos != null && pos.Value.squareDistanceTo(new Vec3D(X, pos.Value.y, Z)) < distance * distance)
            {
                pathToEntity.IncrementPathIndex();
                if (pathToEntity.IsFinished)
                {
                    pos = null;
                    pathToEntity = null;
                }
                else
                {
                    pos = pathToEntity.GetPosition(this);
                }
            }

            Jumping = false;
            if (pos != null)
            {
                double dx = pos.Value.x - X;
                double dz = pos.Value.z - Z;
                double verticalOffset = pos.Value.y - (double)floorY;
                float targetYaw = (float)(System.Math.Atan2(dz, dx) * 180.0D / (double)((float)System.Math.PI)) - 90.0F;
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
                if (hasAttacked && playerToAttack != null)
                {
                    double targetDeltaX = playerToAttack.X - X;
                    double targetDeltaZ = playerToAttack.Z - Z;
                    float previousYaw = Yaw;
                    Yaw = (float)(System.Math.Atan2(targetDeltaZ, targetDeltaX) * 180.0D / (double)((float)System.Math.PI)) - 90.0F;
                    yawDelta = (previousYaw - Yaw + 90.0F) * (float)System.Math.PI / 180.0F;
                    SidewaysSpeed = -MathHelper.Sin(yawDelta) * ForwardSpeed * 1.0F;
                    ForwardSpeed = MathHelper.Cos(yawDelta) * ForwardSpeed * 1.0F;
                }

                if (verticalOffset > 0.0D)
                {
                    Jumping = true;
                }
            }

            if (playerToAttack != null)
            {
                faceEntity(playerToAttack, 30.0F, 30.0F);
            }

            if (HorizontalCollison && !hasPath())
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
            base.tickLiving();
            pathToEntity = null;
        }
    }

    protected void findRandomWanderTarget()
    {
        bool foundWanderTarget = false;
        int bestX = -1;
        int bestY = -1;
        int bestZ = -1;
        float bestCost = -99999.0F;

        for (int _ = 0; _ < 10; ++_)
        {
            int floorX = MathHelper.Floor(X + (double)Random.NextInt(13) - 6.0D);
            int floorY = MathHelper.Floor(Y + (double)Random.NextInt(7) - 3.0D);
            int floorZ = MathHelper.Floor(Z + (double)Random.NextInt(13) - 6.0D);
            float cost = getBlockPathWeight(floorX, floorY, floorZ);
            if (cost > bestCost)
            {
                bestCost = cost;
                bestX = floorX;
                bestY = floorY;
                bestZ = floorZ;
                foundWanderTarget = true;
            }
        }

        if (foundWanderTarget)
        {
            pathToEntity = World.Pathing.findPath(this, bestX, bestY, bestZ, 10.0F);
        }
    }

    protected virtual void attackEntity(Entity entity, float distance)
    {
    }

    protected virtual void attackBlockedEntity(Entity entity, float distance)
    {
    }

    protected virtual float getBlockPathWeight(int x, int y, int z)
    {
        return 0.0F;
    }

    protected virtual Entity? findPlayerToAttack()
    {
        return null;
    }

    public override bool canSpawn()
    {
        int floorX = MathHelper.Floor(X);
        int floorY = MathHelper.Floor(BoundingBox.MinY);
        int floorZ = MathHelper.Floor(Z);
        return base.canSpawn() && getBlockPathWeight(floorX, floorY, floorZ) >= 0.0F;
    }

    public bool hasPath()
    {
        return pathToEntity != null;
    }

    internal void setPathToEntity(PathEntity pathToEntity)
    {
        this.pathToEntity = pathToEntity;
    }

    public Entity getTarget()
    {
        return playerToAttack;
    }

    public void setTarget(Entity playerToAttack)
    {
        this.playerToAttack = playerToAttack;
    }
}
